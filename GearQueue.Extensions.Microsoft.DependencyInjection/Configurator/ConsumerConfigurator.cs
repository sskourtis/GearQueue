using GearQueue.Consumer;
using GearQueue.Options;
using GearQueue.Serialization;
using Microsoft.Extensions.DependencyInjection;

namespace GearQueue.Extensions.Microsoft.DependencyInjection.Configurator;

public class ConsumerConfigurator
{
    private readonly ConsumerRegistration _consumerRegistration;
    
    internal ConsumerConfigurator(ConsumerRegistration consumerRegistration)
    {
        _consumerRegistration = consumerRegistration;
    }

    public ConsumerConfigurator SetHandler<T>(string functionName, IGearQueueJobSerializer jobSerializer, ServiceLifetime lifetime = ServiceLifetime.Transient) 
        where T : IHandler
    {
        _consumerRegistration.HandlerMapping[functionName] = (new HandlerOptions
        {
            Type = typeof(T),
            Serializer = jobSerializer
        }, lifetime);
        return this;
    }
    
    public ConsumerConfigurator SetHandler<T>(string functionName, ServiceLifetime lifetime = ServiceLifetime.Transient) 
        where T : IHandler
    {
        _consumerRegistration.HandlerMapping[functionName] = (new HandlerOptions
        {
            Type = typeof(T)
        }, lifetime);
        return this;
    }

    
    public ConsumerConfigurator SetBatchHandler<T>(string functionName, IGearQueueJobSerializer jobSerializer, BatchOptions batchOptions, ServiceLifetime lifetime = ServiceLifetime.Transient) 
        where T : IHandler
    {
        _consumerRegistration.HandlerMapping[functionName] = (new HandlerOptions
        {
            Type = typeof(T),
            Serializer = jobSerializer,
            Batch = batchOptions,
        }, lifetime);
        return this;
    }
    
    public ConsumerConfigurator SetBatchHandler<T>(string functionName, BatchOptions batchOptions, ServiceLifetime lifetime = ServiceLifetime.Transient) 
        where T : IHandler
    {
        _consumerRegistration.HandlerMapping[functionName] = (new HandlerOptions
        {
            Type = typeof(T),
            Batch = batchOptions,
        }, lifetime);
        return this;
    }
    
    public void SetPipeline(Action<ConsumerPipelineBuilder> builder)
    {
        builder(_consumerRegistration.PipelineBuilder);
    }
}