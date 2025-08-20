using GearQueue.Logging;
using GearQueue.Protocol.Response;
using Microsoft.Extensions.Logging;

namespace GearQueue.Consumer.Coordinators;

public class SynchronousHandlerExecutionCoordinator(
    IGearQueueHandlerExecutor handlerExecutor,
    Dictionary<string, Type> handlers,
    ILoggerFactory loggerFactory) : IHandlerExecutionCoordinator
{
    private readonly ILogger<SynchronousHandlerExecutionCoordinator> _logger = loggerFactory.CreateLogger<SynchronousHandlerExecutionCoordinator>();
    
    public void RegisterAsyncResultCallback(int connectionId, Func<string, JobResult, Task> callback)
    {
    }

    public async Task<ExecutionResult> ArrangeExecution(int connectionId, JobAssign? job, CancellationToken cancellationToken)
    {
        if (job is null)
        {
            return new ExecutionResult();
        }
        
        try
        {
            var jobContext = new JobContext(job, cancellationToken);
            
            if (!handlers.TryGetValue(job.FunctionName, out var handlerType))
            {
                _logger.LogMissingHandlerType(job.FunctionName);
               
                return JobResult.PermanentFailure;
            }
            
            var (success, jobStatus) = await handlerExecutor.TryExecute(handlerType, jobContext).ConfigureAwait(false);

            if (!success)
            {
                _logger.LogHandlerTypeCreationFailure(handlerType, job.FunctionName);
            }

            return jobStatus ?? JobResult.PermanentFailure;
        }
        catch (Exception e)
        {
            _logger.LogConsumerException(e);
            return JobResult.PermanentFailure;
        }
    }

    public Task WaitAllExecutions()
    {
        return Task.CompletedTask;
    }
}