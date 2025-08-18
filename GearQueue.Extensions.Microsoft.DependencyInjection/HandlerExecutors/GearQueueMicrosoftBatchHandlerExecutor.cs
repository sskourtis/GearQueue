using GearQueue.BatchConsumer;
using GearQueue.Consumer;

namespace GearQueue.Extensions.Microsoft.DependencyInjection.HandlerExecutors;

public class GearQueueMicrosoftBatchHandlerExecutor(
    IServiceProvider serviceProvider) : IGearQueueBatchHandlerExecutor
{
    public async Task<(bool Success, JobStatus? Status)> TryExecute(Type handlerType, BatchJobContext jobContext)
    {
        if (serviceProvider.GetService(handlerType) is not IGearQueueBatchHandler handler)
        {
            return (false, null);
        }
        
        return (true, await handler.Consume(jobContext));
    }
}