using GearQueue.Consumer.Provider;
using GearQueue.Protocol.Response;
using Microsoft.Extensions.Logging;

namespace GearQueue.Consumer.Coordinators;

internal class SynchronousAbstractHandlerExecutionCoordinator(
    IGearQueueHandlerProviderFactory handlerProviderFactory,
    Dictionary<string, Type> handlers,
    ILoggerFactory loggerFactory) : AbstractHandlerExecutionCoordinator(loggerFactory, handlerProviderFactory, handlers, null, null)
{
    internal override async Task<ExecutionResult> ArrangeExecution(int connectionId, JobAssign? job, CancellationToken cancellationToken)
    {
        if (job is null)
        {
            return new ExecutionResult();
        }
        
        var jobContext = new JobContext(job, cancellationToken);

        return await InvokeHandler(job.FunctionName, jobContext, cancellationToken);
    }
}