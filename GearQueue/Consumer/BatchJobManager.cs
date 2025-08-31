using GearQueue.Metrics;
using GearQueue.Protocol.Response;
using Microsoft.Extensions.ObjectPool;

namespace GearQueue.Consumer;

internal interface IBatchJobManager
{
    (TimeSpan?, IEnumerable<JobContext>?) TryGetJobs(int connectionId, JobAssign? job);
    
    string FunctionName { get; }
}

internal class BatchJobManager(string functionName, HandlerOptions options, IMetricsCollector? metricsCollector = null) : IBatchJobManager
{
    private static readonly ObjectPool<BatchData> BatchDataPool = new DefaultObjectPool<BatchData>(new DefaultPooledObjectPolicy<BatchData>());
    
    // When ByKey=false, there can only ever be one pending batch
    private readonly List<BatchData> _pendingBatches = [];
    private readonly TimeProvider _timeProvider = TimeProvider.System;

    // This constructor exists only for unit testing purposes
    internal BatchJobManager(string functionName, HandlerOptions options, TimeProvider timeProvider, List<BatchData>? pendingBatches = null)
        : this(functionName, options)
    {
        _timeProvider = timeProvider;
        _pendingBatches = pendingBatches ?? _pendingBatches;
    }

    public string FunctionName => functionName;
    
    public (TimeSpan?, IEnumerable<JobContext>?) TryGetJobs(int connectionId, JobAssign? job)
    {
        List<BatchData>? readyToRunBatches;
        TimeSpan? nextTimeout; 
        
        lock (_pendingBatches)
        {
            (nextTimeout, readyToRunBatches) = FindReadToRunBatches(connectionId, job);
        }

        var jobContexts = readyToRunBatches?.Select(
            b =>
            {
                var context = new JobContext(b.Jobs,
                    b.Key,
                    options.Serializer,
                    CancellationToken.None)
                {
                    HandlerType = options.Type
                };
                
                // Very important to return the batch data back to the pool
                BatchDataPool.Return(b);

                return context;
            });

        if (nextTimeout is null)
        {
            // There are no pending batches, don't return custom timeout
            return (null, jobContexts);
        } 

        return nextTimeout >= options.Batch!.TimeLimit
            ? (options.Batch!.TimeLimit, jobContexts)
            : (nextTimeout.Value, jobContexts);
    }

    private (TimeSpan?, List<BatchData>?) FindReadToRunBatches(int connectionId, JobAssign? job)
    {
        List<BatchData>? completedBatches = null;
        TimeSpan? minimumNextTimeout = null; 
        
        for (var i = _pendingBatches.Count - 1; i >= 0; i--)
        {
            var batch = _pendingBatches[i];
                
            if (job is not null &&
                job.FunctionName == FunctionName && 
                (!options.Batch!.ByKey || batch.Key == job.BatchKey))
            {
                batch.Jobs.Add((connectionId, job));
                job = null;
            }
                
            var batchNextTimeout = options.Batch!.TimeLimit - (_timeProvider.GetUtcNow() - batch.Created);
                
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
            metricsCollector?.BatchedJobPreparedWithSize(functionName, batch.Key, batch.Jobs.Count);
            _pendingBatches.RemoveAt(i);
        }

        if (job is not null && job.FunctionName == FunctionName)
        {
            minimumNextTimeout ??= options.Batch!.TimeLimit;
            
            _pendingBatches.Add(CreateNewBatch(connectionId, job));
        }
        
        return (minimumNextTimeout, completedBatches);
    }

    private BatchData CreateNewBatch(int connectionId, JobAssign job)
    {
        var batch = BatchDataPool.Get();

        if (batch.Jobs.Count > 0)
        {
            batch.Jobs.Clear();
        }
            
        batch.Jobs.Add((connectionId, job));
            
        batch.Created = _timeProvider.GetUtcNow();

        if (options.Batch!.ByKey)
        {
            batch.Key = job.BatchKey;   
        }

        return batch;
    }
    
    internal class BatchData
    {
        public string? Key { get; set; }

        public List<(int ConnectionId, JobAssign Job)> Jobs { get; set; } = [];
        
        public DateTimeOffset Created { get; set; }
    }
}