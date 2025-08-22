using GearQueue.Extensions.Microsoft.DependencyInjection.Configurator;
using Microsoft.Extensions.DependencyInjection;

namespace GearQueue.Extensions.Microsoft.DependencyInjection;

public static class GearQueueExtensions
{
    public static IServiceCollection AddGearQueue(this IServiceCollection services, Action<Configurator.Configurator> configurator)
    {
        var config = new Configurator.Configurator(services);
        
        configurator(config);
        
        config.Setup();
        
        return services;
    }
}
