using GearQueue.Logging;
using GearQueue.Protocol.Response;
using Microsoft.Extensions.Logging;

namespace GearQueue.Consumer.Coordinators;

public class SynchronousHandlerExecutionCoordinator(
    IGearQueueHandlerExecutor handlerExecutor,
    Type handlerType,
    ILoggerFactory loggerFactory) : IHandlerExecutionCoordinator
{
    private readonly ILogger<SynchronousHandlerExecutionCoordinator> _logger = loggerFactory.CreateLogger<SynchronousHandlerExecutionCoordinator>();
    
    public void RegisterAsyncResultCallback(int connectionId, Func<string, JobStatus, Task> callback)
    {
    }

    public async Task<JobStatus?> ArrangeExecution(int connectionId, JobAssign job, CancellationToken cancellationToken)
    {
        try
        {
            var jobContext = new JobContext(job, cancellationToken);
            
            var (success, jobStatus) = await handlerExecutor.TryExecute(handlerType, jobContext).ConfigureAwait(false);

            if (!success)
            {
                _logger.LogHandlerTypeCreationFailure(handlerType, job.FunctionName);
            }

            return jobStatus ?? JobStatus.PermanentFailure;
        }
        catch (Exception e)
        {
            _logger.LogConsumerException(e);
            return JobStatus.PermanentFailure;
        }
    }

    public Task WaitAllExecutions()
    {
        return Task.CompletedTask;
    }
}