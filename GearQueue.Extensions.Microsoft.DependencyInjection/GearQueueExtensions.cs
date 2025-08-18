using GearQueue.Extensions.Microsoft.DependencyInjection.Configurator;
using Microsoft.Extensions.DependencyInjection;

namespace GearQueue.Extensions.Microsoft.DependencyInjection;

public static class GearQueueExtensions
{
    public static IServiceCollection AddGearQueue(this IServiceCollection services, Action<GearQueueConfigurator> configurator)
    {
        var config = new GearQueueConfigurator(services);
        
        configurator(config);
        
        config.Setup();
        
        return services;
    }
}
