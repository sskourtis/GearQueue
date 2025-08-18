using GearQueue.Consumer;

namespace GearQueue.Extensions.Microsoft.DependencyInjection.HandlerExecutors;

public class GearQueueMicrosoftHandlerExecutor(
    IServiceProvider serviceProvider) : IGearQueueHandlerExecutor
{
    public async Task<(bool Success, JobStatus? Status)> TryExecute(Type handlerType, JobContext jobContext)
    {
        if (serviceProvider.GetService(handlerType) is not IGearQueueHandler handler)
        {
            return (false, null);
        }
        
        return (true, await handler.Consume(jobContext));
    }
}