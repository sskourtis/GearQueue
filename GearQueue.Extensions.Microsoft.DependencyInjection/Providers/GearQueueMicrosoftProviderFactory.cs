using GearQueue.Consumer.Provider;
using Microsoft.Extensions.DependencyInjection;

namespace GearQueue.Extensions.Microsoft.DependencyInjection.Providers;

public class GearQueueMicrosoftProviderFactory(bool createScope, IServiceProvider serviceProvider) : IGearQueueHandlerProviderFactory
{
    public IGearQueueHandlerProvider Create()
    {
        return createScope
            ? serviceProvider.GetRequiredService<GearQueueMicrosoftScopedProvider>()
            : serviceProvider.GetRequiredService<GearQueueMicrosoftProvider>();
    }
}