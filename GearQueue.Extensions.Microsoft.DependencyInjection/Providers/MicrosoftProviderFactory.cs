using GearQueue.Consumer.Provider;
using Microsoft.Extensions.DependencyInjection;

namespace GearQueue.Extensions.Microsoft.DependencyInjection.Providers;

public class MicrosoftProviderFactory(bool createScope, IServiceProvider serviceProvider) : IHandlerProviderFactory
{
    public IHandlerProvider Create()
    {
        return createScope
            ? serviceProvider.GetRequiredService<MicrosoftScopedProvider>()
            : serviceProvider.GetRequiredService<MicrosoftProvider>();
    }
}