using System.Collections.Concurrent;
using GearQueue.Consumer.Pipeline;
using GearQueue.Logging;
using GearQueue.Protocol.Request;
using GearQueue.Protocol.Response;
using Microsoft.Extensions.Logging;

namespace GearQueue.Consumer.Coordinators;

internal abstract class AbstractHandlerExecutionCoordinator(
    ILoggerFactory loggerFactory,
    ConsumerPipeline consumerPipeline,
    Dictionary<string, HandlerOptions> handlers,
    Dictionary<int, Func<string, JobResult, Task>>? jobResultCallback,
    ConcurrentDictionary<Guid, TaskCompletionSource<bool>>? activeJobs)
{
    private readonly ILogger _logger = loggerFactory.CreateLogger<Consumer>();
    
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
    
    internal virtual RequestPacket GrabJobPacket => RequestFactory.GrabJob();
    
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

    protected async Task<JobResult> InvokeHandler(JobAssign job, CancellationToken cancellationToken)
    {
        try
        {
            if (!handlers.TryGetValue(job.FunctionName, out var handlerOptions))
            {
                _logger.LogMissingHandlerType(job.FunctionName);
               
                return JobResult.PermanentFailure;
            }
            
            var context = new JobContext(job, handlerOptions.Serializer,  0, cancellationToken)
            {
                HandlerType = handlerOptions.Type
            };

            await consumerPipeline.InvokeAsync(context);

            return context.Result ?? JobResult.PermanentFailure;
        }
        catch (Exception e)
        {
            _logger.LogConsumerException(e);
            return JobResult.PermanentFailure;
        }
    }
}