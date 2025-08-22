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

    public GearQueueConsumerConfigurator SetHandler<T, TY>(string functionName, IGearQueueSerializer serializer, ServiceLifetime lifetime = ServiceLifetime.Transient) 
        where T : GearQueueTypedHandler<TY>
    {
        _consumerRegistration.HandlerMapping[functionName] = (new HandlerOptions
        {
            Type = typeof(T),
            JobContextFactory = new JobContextFactory<T>(serializer)
        }, lifetime);
        return this;
    }
    
    public GearQueueConsumerConfigurator SetHandler<T>(string functionName, ServiceLifetime lifetime = ServiceLifetime.Transient) 
        where T : IGearQueueHandler
    {
        _consumerRegistration.HandlerMapping[functionName] = (new HandlerOptions
        {
            Type = typeof(T),
            JobContextFactory = new JobContextFactory(),
        }, lifetime);
        return this;
    }

    public void SetPipeline(Action<ConsumerPipelineBuilder> builder)
    {
        builder(_consumerRegistration.PipelineBuilder);
    }
}