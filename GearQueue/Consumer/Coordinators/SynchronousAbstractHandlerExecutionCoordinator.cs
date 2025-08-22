using GearQueue.Consumer.Pipeline;
using GearQueue.Protocol.Response;
using Microsoft.Extensions.Logging;

namespace GearQueue.Consumer.Coordinators;

internal class SynchronousAbstractHandlerExecutionCoordinator(
    ConsumerPipeline consumerPipeline,
    Dictionary<string, HandlerOptions> handlers,
    ILoggerFactory loggerFactory) : AbstractHandlerExecutionCoordinator(loggerFactory, consumerPipeline, handlers, null, null)
{
    internal override async Task<ExecutionResult> ArrangeExecution(int connectionId, JobAssign? job, CancellationToken cancellationToken)
    {
        if (job is null)
        {
            return new ExecutionResult();
        }
        
        return await InvokeHandler(job, cancellationToken);
    }
}