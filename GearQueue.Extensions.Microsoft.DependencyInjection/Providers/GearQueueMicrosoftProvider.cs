using GearQueue.Consumer;
using GearQueue.Consumer.Provider;
using Microsoft.Extensions.DependencyInjection;

namespace GearQueue.Extensions.Microsoft.DependencyInjection.Providers;

public class GearQueueMicrosoftProvider(IServiceProvider provider) : IGearQueueHandlerProvider
{
    public void Dispose()
    {
    }

    public IGearQueueHandler? Get<T>() where T : IGearQueueHandler
    {
        return provider.GetService<T>();
    }

    public IGearQueueHandler? Get(Type type)
    {
        return provider.GetService(type) as IGearQueueHandler;
    }
}