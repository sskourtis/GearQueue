using GearQueue.Consumer;
using GearQueue.Serialization;
using Microsoft.Extensions.DependencyInjection;

namespace GearQueue.Extensions.Microsoft.DependencyInjection.Configurator;

public class GearQueueConsumerConfigurator
{
    private readonly ConsumerRegistration _consumerRegistration;
    
    internal GearQueueConsumerConfigurator(ConsumerRegistration consumerRegistration)
    {
        _consumerRegistration = consumerRegistration;
    }

    public GearQueueConsumerConfigurator SetHandler<T>(string functionName, IGearQueueSerializer serializer, ServiceLifetime lifetime = ServiceLifetime.Transient) 
        where T : IHandler
    {
        _consumerRegistration.HandlerMapping[functionName] = (new HandlerOptions
        {
            Type = typeof(T),
            Serializer = serializer
        }, lifetime);
        return this;
    }
    
    public GearQueueConsumerConfigurator SetHandler<T>(string functionName, ServiceLifetime lifetime = ServiceLifetime.Transient) 
        where T : IHandler
    {
        _consumerRegistration.HandlerMapping[functionName] = (new HandlerOptions
        {
            Type = typeof(T)
        }, lifetime);
        return this;
    }

    public void SetPipeline(Action<ConsumerPipelineBuilder> builder)
    {
        builder(_consumerRegistration.PipelineBuilder);
    }
}