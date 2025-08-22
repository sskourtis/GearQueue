using GearQueue.Consumer;
using GearQueue.Consumer.Provider;
using Microsoft.Extensions.DependencyInjection;

namespace GearQueue.Extensions.Microsoft.DependencyInjection.Providers;

public class GearQueueMicrosoftScopedProvider(IServiceScopeFactory scopeFactory) : IGearQueueHandlerProvider
{
    private readonly IServiceScope _serviceScope = scopeFactory.CreateScope();
    
    public IHandler? Get<T>() where T : IHandler
    {
        return _serviceScope.ServiceProvider.GetService<T>();
    }

    public IHandler? Get(Type type)
    {
        return _serviceScope.ServiceProvider.GetService(type) as IHandler;
    }

    public void Dispose()
    {
        _serviceScope.Dispose();
    }
}