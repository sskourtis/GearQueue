using GearQueue.Consumer;
using GearQueue.Consumer.Provider;
using Microsoft.Extensions.DependencyInjection;

namespace GearQueue.Extensions.Microsoft.DependencyInjection.Providers;

public class GearQueueMicrosoftProvider(IServiceProvider provider) : IGearQueueHandlerProvider
{
    public void Dispose()
    {
    }

    public IHandler? Get<T>() where T : IHandler
    {
        return provider.GetService<T>();
    }

    public IHandler? Get(Type type)
    {
        return provider.GetService(type) as IHandler;
    }
}