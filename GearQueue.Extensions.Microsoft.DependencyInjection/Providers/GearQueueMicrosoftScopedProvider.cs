using GearQueue.Consumer;
using GearQueue.Consumer.Provider;
using Microsoft.Extensions.DependencyInjection;

namespace GearQueue.Extensions.Microsoft.DependencyInjection.Providers;

public class GearQueueMicrosoftScopedProvider(IServiceScopeFactory scopeFactory) : IGearQueueHandlerProvider
{
    private readonly IServiceScope _serviceScope = scopeFactory.CreateScope();
    
    public IGearQueueHandler? Get<T>() where T : IGearQueueHandler
    {
        return _serviceScope.ServiceProvider.GetService<T>();
    }

    public IGearQueueHandler? Get(Type type)
    {
        return _serviceScope.ServiceProvider.GetService(type) as IGearQueueHandler;
    }

    public void Dispose()
    {
        _serviceScope.Dispose();
    }
}