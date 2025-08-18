using GearQueue.BatchConsumer;
using GearQueue.Consumer;
using Microsoft.Extensions.DependencyInjection;

namespace GearQueue.Extensions.Microsoft.DependencyInjection.HandlerExecutors;

public class GearQueueMicrosoftBatchScopedHandlerExecutor(
    IServiceScopeFactory serviceScopeFactory) : IGearQueueBatchHandlerExecutor
{
    public async Task<(bool Success, JobStatus? Status)> TryExecute(Type handlerType, BatchJobContext jobContext)
    {
        var scope = serviceScopeFactory.CreateScope();
        
        if (scope.ServiceProvider.GetService(handlerType) is not IGearQueueBatchHandler handler)
        {
            return (false, null);
        }

        return (true, await handler.Consume(jobContext));
    }
}