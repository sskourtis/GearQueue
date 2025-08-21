using System.Collections.Concurrent;
using GearQueue.Consumer.Provider;
using GearQueue.Logging;
using GearQueue.Protocol.Response;
using Microsoft.Extensions.Logging;

namespace GearQueue.Consumer.Coordinators;

internal abstract class AbstractHandlerExecutionCoordinator(
    ILoggerFactory loggerFactory,
    IGearQueueHandlerProviderFactory handlerProviderFactory,
    Dictionary<string, Type> handlers,
    Dictionary<int, Func<string, JobResult, Task>>? jobResultCallback,
    ConcurrentDictionary<Guid, TaskCompletionSource<bool>>? activeJobs)
{
    private readonly ILogger _logger = loggerFactory.CreateLogger<GearQueueConsumer>();
    
    protected readonly Dictionary<int, Func<string, JobResult, Task>>? JobResultCallback = jobResultCallback;
    protected readonly ConcurrentDictionary<Guid, TaskCompletionSource<bool>>? ActiveJobs = activeJobs;
    
    /// <summary>
    /// Accepts a job and arranges the execution of the job. Depending on the implementation, the job may be
    /// executed synchronously or asynchronously.
    /// </summary>
    /// <param name="connectionId"></param>
    /// <param name="job"></param>
    /// <param name="cancellationToken"></param>
    /// <returns>Returns the job status when it is executed synchronously, otherwise it returns null</returns>
    internal abstract Task<ExecutionResult> ArrangeExecution(int connectionId, JobAssign? job, CancellationToken cancellationToken);

    
    /// <summary>
    /// Register a result callback for async job execution
    /// </summary>
    /// <param name="connectionId"></param>
    /// <param name="callback"></param>
    internal void RegisterAsyncResultCallback(int connectionId, Func<string, JobResult, Task> callback)
    {
        if (JobResultCallback is not null)
        {
            JobResultCallback[connectionId] = callback;
        }
    }

    internal async Task WaitAllExecutions()
    {
        if (ActiveJobs is not null)
        {
            await Task.WhenAll(ActiveJobs.Values.Select(t => t.Task)).ConfigureAwait(false);;
        }
    }

    protected async Task<JobResult> InvokeHandler(string function, JobContext context, CancellationToken cancellationToken)
    {
        try
        {
            if (!handlers.TryGetValue(function, out var handlerType))
            {
                _logger.LogMissingHandlerType(function);
               
                return JobResult.PermanentFailure;
            }

            using var provider = handlerProviderFactory.Create();

            var handler = provider.Get(handlerType);
            
            if (handler is null)
            {
                _logger.LogHandlerTypeCreationFailure(handlerType, function);
                return JobResult.PermanentFailure;
            }

            return await handler.Consume(context);
        }
        catch (Exception e)
        {
            _logger.LogConsumerException(e);
            return JobResult.PermanentFailure;
        }
    }
}