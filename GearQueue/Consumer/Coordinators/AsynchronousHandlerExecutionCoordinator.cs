using System.Collections.Concurrent;
using GearQueue.Logging;
using GearQueue.Options;
using GearQueue.Protocol.Response;
using Microsoft.Extensions.Logging;

namespace GearQueue.Consumer.Coordinators;

public class AsynchronousHandlerExecutionCoordinator(
    IGearQueueHandlerExecutor handlerExecutor,
    Type handlerType,
    GearQueueConsumerOptions options,
    ILoggerFactory loggerFactory) : IHandlerExecutionCoordinator
{
    private readonly ILogger<AsynchronousHandlerExecutionCoordinator> _logger = loggerFactory.CreateLogger<AsynchronousHandlerExecutionCoordinator>();
    private readonly ConcurrentDictionary<int, Func<string, JobStatus, Task>> _jobResultCallback = new();
    private readonly ConcurrentDictionary<Guid, Task> _activeJobs = new();
    private readonly SemaphoreSlim _handlerSemaphore = new(options.MaxConcurrency, options.MaxConcurrency);
    
    public void RegisterAsyncResultCallback(int connectionId, Func<string, JobStatus, Task> callback)
    {
        _jobResultCallback[connectionId] = callback;
    }

    public async Task<JobStatus?> ArrangeExecution(int connectionId, JobAssign job, CancellationToken cancellationToken)
    {
        await _handlerSemaphore.WaitAsync(cancellationToken);
        
        var batchId = Guid.NewGuid();

        var task = CallHandler(batchId, connectionId, job, cancellationToken);
        
        _activeJobs.TryAdd(batchId, task);

        return null;
    }

    public async Task WaitAllExecutions()
    {
        await Task.WhenAll(_activeJobs.Values);
    }
    
    private async Task CallHandler(
        Guid executionId,
        int connectionId,
        JobAssign job, 
        CancellationToken cancellationToken)
    {
        try
        {
            var jobContext = new JobContext(job, cancellationToken);

            var (success, jobStatus) = await handlerExecutor.TryExecute(handlerType, jobContext).ConfigureAwait(false);

            if (!success)
            {
                _logger.LogHandlerTypeCreationFailure(handlerType, job.FunctionName);
            }

            jobStatus ??= JobStatus.PermanentFailure;

            if (_jobResultCallback.TryGetValue(connectionId, out var callback))
            {
                await callback.Invoke(job.JobHandle, jobStatus.Value)
                    .ConfigureAwait(false);
            }
        }
        catch (Exception e)
        {
            _logger.LogConsumerException(e);
            
            if (_jobResultCallback.TryGetValue(connectionId, out var callback))
            {
                await callback.Invoke(job.JobHandle, JobStatus.PermanentFailure)
                    .ConfigureAwait(false);
            }
        }
        finally
        {
            _activeJobs.Remove(executionId, out _);
            _handlerSemaphore.Release();
        }
    }
    
}