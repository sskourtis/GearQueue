using GearQueue.Consumer;
using Microsoft.Extensions.DependencyInjection;

namespace GearQueue.Extensions.Microsoft.DependencyInjection;

public class GearQueueMicrosoftScopedHandlerExecutor(
    IServiceScopeFactory serviceScopeFactory) : IGearQueueHandlerExecutor
{

    public async Task<(bool Success, JobStatus? Status)> TryExecute(Type handlerType, JobContext jobContext)
    {
        var scope = serviceScopeFactory.CreateScope();
        
        if (scope.ServiceProvider.GetService(handlerType) is not IGearQueueHandler handler)
        {
            return (false, null);
        }

        return (true, await handler.Consume(jobContext));
    }
}