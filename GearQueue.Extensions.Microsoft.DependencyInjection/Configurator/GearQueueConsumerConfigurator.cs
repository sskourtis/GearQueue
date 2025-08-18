using Microsoft.Extensions.DependencyInjection;

namespace GearQueue.Extensions.Microsoft.DependencyInjection.Configurator;

public class GearQueueConsumerConfigurator
{
    private readonly ConsumerRegistration _consumerRegistration;
    
    internal GearQueueConsumerConfigurator(ConsumerRegistration consumerRegistration)
    {
        _consumerRegistration = consumerRegistration;
    }

    public GearQueueConsumerConfigurator SetHandler<T>(string functionName, ServiceLifetime lifetime = ServiceLifetime.Transient)
    {
        _consumerRegistration.HandlerMapping[functionName] = (typeof(T), lifetime);
        return this;
    }
}