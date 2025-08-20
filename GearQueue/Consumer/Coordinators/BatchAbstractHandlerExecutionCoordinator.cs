using System.Collections.Concurrent;
using GearQueue.Consumer.Provider;
using GearQueue.Options;
using GearQueue.Protocol.Response;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;

namespace GearQueue.Consumer.Coordinators;

internal class BatchAbstractHandlerExecutionCoordinator(
    ILoggerFactory loggerFactory,
    IGearQueueHandlerProviderFactory handlerProviderFactory,
    GearQueueConsumerOptions options,
    Dictionary<string, Type> handlers) 
    : AbstractHandlerExecutionCoordinator( 
        loggerFactory,
        handlerProviderFactory, 
        handlers,
        new Dictionary<int, Func<string, JobResult, Task>>(), 
        new ConcurrentDictionary<Guid, Task>())
{
    private readonly ObjectPool<BatchData> _batchDataPool = new DefaultObjectPool<BatchData>(new DefaultPooledObjectPolicy<BatchData>());
    private readonly List<BatchData> _pendingBatches = [];
    private readonly SemaphoreSlim _handlerSemaphore = new(options.MaxConcurrency, options.MaxConcurrency);

    internal override async Task<ExecutionResult> ArrangeExecution(int connectionId, JobAssign? job, CancellationToken cancellationToken)
    {
        var (nextTimeout, completedBatches) = GetCompletedBatches(connectionId, job);

        if (completedBatches is not null)
        {
            foreach (var batch in completedBatches)
            {
                await _handlerSemaphore.WaitAsync(cancellationToken);
        
                var batchId = Guid.NewGuid();

                var task = CallHandler(batchId, batch, cancellationToken);
                ActiveJobs!.TryAdd(batchId, task);
            }
        }

        return nextTimeout ?? new ExecutionResult();
    }

    private (TimeSpan?, List<BatchData>?) GetCompletedBatches(int connectionId, JobAssign? job)
    {
        List<BatchData>? completedBatches = null;
        TimeSpan? minimumNextTimeout = null; 
        
        lock (_pendingBatches)
        {
            for (var i = _pendingBatches.Count - 1; i >= 0; i--)
            {
                var batch = _pendingBatches[i];
                
                if (job is not null && batch.Function == job.FunctionName)
                {
                    batch.Jobs.Add((connectionId, job));
                    job = null;
                }
                
                var batchNextTimeout = options.Batch!.TimeLimit - (DateTimeOffset.UtcNow - batch.Created);
                
                if (batch.Jobs.Count < options.Batch!.Size &&
                    batchNextTimeout > TimeSpan.Zero)
                {
                    minimumNextTimeout = minimumNextTimeout < batchNextTimeout
                        ? minimumNextTimeout
                        : batchNextTimeout;
                    continue;
                }

                completedBatches ??= [];
                completedBatches.Add(batch);
                _pendingBatches.RemoveAt(i);
            }

            if (job is not null)
            {
                var batch = _batchDataPool.Get();

                if (batch.Jobs.Count > 0)
                {
                    batch.Jobs.Clear();
                }
                
                batch.Jobs.Add((connectionId, job));
                
                batch.Created = DateTimeOffset.UtcNow;
                batch.Key = null;
                batch.Function = job.FunctionName;
                
                minimumNextTimeout ??= options.Batch!.TimeLimit;
                
                _pendingBatches.Add(batch);   
            }
        }

        if (minimumNextTimeout is null)
        {
            // There are no pending batches, don't return custom timeout
            return (null, completedBatches);
        } 

        return minimumNextTimeout >= options.Batch!.TimeLimit
            ? (options.Batch!.TimeLimit, completedBatches)
            : (minimumNextTimeout.Value, completedBatches);
    }

    private async Task CallHandler(
        Guid batchProcessingId,
        BatchData batchData, 
        CancellationToken cancellationToken)
    {
        try
        {
            var jobContext = new JobContext(batchData.Jobs.Select(j => j.Job), cancellationToken);
            
            var result = await InvokeHandler(batchData.Function, jobContext, cancellationToken);
            
            foreach (var job in batchData.Jobs)
            {
                if (JobResultCallback!.TryGetValue(job.ConnectionId, out var callback))
                {
                    await callback.Invoke(job.Job.JobHandle, result)
                        .ConfigureAwait(false);
                }
            }
        }
        finally
        {
            _batchDataPool.Return(batchData);
            _handlerSemaphore.Release();
            ActiveJobs!.Remove(batchProcessingId, out _);
        }
    }

    private class BatchData
    {
        public string Function { get; set; } = string.Empty;
        public string? Key { get; set; }

        public List<(int ConnectionId, JobAssign Job)> Jobs { get; set; } = [];
        
        public DateTimeOffset Created { get; set; }
    }
}