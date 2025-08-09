using System.Collections.Concurrent;
using GearQueue.Consumer;
using GearQueue.Logging;
using GearQueue.Options;
using GearQueue.Protocol.Response;
using Microsoft.Extensions.Logging;

namespace GearQueue.BatchConsumer;

public interface IBatchCoordinator
{
    void RegisterJobResultCallback(int connectionId, Func<string, JobStatus, Task> callback);
    Task<TimeSpan> Notify(int connectionId, JobAssign? job, CancellationToken cancellationToken);
    Task WaitAllHandlers();
}

public class BatchCoordinator(
    ILoggerFactory loggerFactory,
    IGearQueueBatchHandlerExecutor handlerExecutor,
    GearQueueConsumerOptions options,
    Type handlerType) : IBatchCoordinator
{
    private readonly ILogger<BatchCoordinator> _logger = loggerFactory.CreateLogger<BatchCoordinator>();
    private readonly List<(int ConnectionId, JobAssign Job)> _jobs = new(options.Batch!.Size);
    private readonly Dictionary<int, Func<string, JobStatus, Task>> _jobResultCallback = new();
    private readonly SemaphoreSlim _handlerSemaphore = new(options.MaxConcurrency, options.MaxConcurrency);
    private readonly ConcurrentDictionary<Guid, Task> _activeJobHandler = new();

    private DateTimeOffset _lastHandlerCall = new(DateTime.Now);

    public void RegisterJobResultCallback(int connectionId, Func<string, JobStatus, Task> callback)
    {
        _jobResultCallback[connectionId] = callback;
    }
    
    public async Task<TimeSpan> Notify(int connectionId, JobAssign? job, CancellationToken cancellationToken)
    {
        List<(int, JobAssign)> batch;
        
        lock (_jobs)
        {
            if (job is not null)
            {
                _jobs.Add((connectionId, job));    
            }

            var timeUntilNextTimeout = options.Batch!.TimeLimit - (DateTimeOffset.Now - _lastHandlerCall);
  
            if (_jobs.Count < options.Batch!.Size &&
                timeUntilNextTimeout > TimeSpan.Zero)
            {
                return timeUntilNextTimeout;
            }
            
            _lastHandlerCall = DateTimeOffset.UtcNow;

            if (_jobs.Count == 0)
            {
                return options.Batch!.TimeLimit;
            }
            
            batch = _jobs.ToList();

            _jobs.Clear();
        }
        
        await _handlerSemaphore.WaitAsync(cancellationToken);
        
        var batchId = Guid.NewGuid();

        var task = CallHandler(batchId, batch, cancellationToken);
        
        _activeJobHandler.TryAdd(batchId, task);
                
        return options.Batch!.TimeLimit;
    }

    public async Task WaitAllHandlers()
    {
        await Task.WhenAll(_activeJobHandler.Values);
    }

    private async Task CallHandler(
        Guid batchProcessingId,
        List<(int ConnectionId, JobAssign Job)> jobs, 
        CancellationToken cancellationToken)
    {
        try
        {
            var jobContext = new BatchJobContext(jobs.Select(j => j.Job), cancellationToken);

            var (success, jobStatus) = await handlerExecutor.TryExecute(handlerType, jobContext)
                .ConfigureAwait(false);

            if (!success)
            {
                _logger.LogHandlerTypeCreationFailure(handlerType, "-");
                return;
            }

            jobStatus ??= JobStatus.PermanentFailure;

            foreach (var job in jobs)
            {
                if (_jobResultCallback.TryGetValue(job.ConnectionId, out var callback))
                {
                    await callback.Invoke(job.Job.JobHandle, jobStatus.Value)
                        .ConfigureAwait(false);
                }
            }
        }
        finally
        {
            _activeJobHandler.Remove(batchProcessingId, out _);
            _handlerSemaphore.Release();
        }
    }
}