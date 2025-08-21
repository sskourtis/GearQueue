using GearQueue.Consumer.Pipeline;
using GearQueue.Protocol.Response;
using GearQueue.Serialization;
using Microsoft.Extensions.Logging;

namespace GearQueue.Consumer.Coordinators;

internal class SynchronousAbstractHandlerExecutionCoordinator(
    IGearQueueSerializer? serializer,
    ConsumerPipeline consumerPipeline,
    Dictionary<string, Type> handlers,
    ILoggerFactory loggerFactory) : AbstractHandlerExecutionCoordinator(loggerFactory, consumerPipeline, handlers, null, null)
{
    internal override async Task<ExecutionResult> ArrangeExecution(int connectionId, JobAssign? job, CancellationToken cancellationToken)
    {
        if (job is null)
        {
            return new ExecutionResult();
        }
        
        var jobContext = new JobContext(serializer, job, cancellationToken);

        return await InvokeHandler(jobContext);
    }
}