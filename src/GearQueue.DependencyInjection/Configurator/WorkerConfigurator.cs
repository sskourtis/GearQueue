using GearQueue.Options;
using GearQueue.Serialization;
using GearQueue.Worker;
using Microsoft.Extensions.DependencyInjection;

namespace GearQueue.DependencyInjection.Configurator;

public class WorkerConfigurator
{
    private readonly WorkerRegistration _workerRegistration;
    
    internal WorkerConfigurator(WorkerRegistration workerRegistration)
    {
        _workerRegistration = workerRegistration;
    }

    public WorkerConfigurator SetHandler<T>(string functionName, IGearQueueJobSerializer jobSerializer, ServiceLifetime lifetime = ServiceLifetime.Transient) 
        where T : IHandler
    {
        _workerRegistration.HandlerMapping[functionName] = (new HandlerOptions
        {
            Type = typeof(T),
            Serializer = jobSerializer
        }, lifetime);
        return this;
    }
    
    public WorkerConfigurator SetHandler<T>(string functionName, ServiceLifetime lifetime = ServiceLifetime.Transient) 
        where T : IHandler
    {
        _workerRegistration.HandlerMapping[functionName] = (new HandlerOptions
        {
            Type = typeof(T)
        }, lifetime);
        return this;
    }

    
    public WorkerConfigurator SetBatchHandler<T>(string functionName, IGearQueueJobSerializer jobSerializer, BatchOptions batchOptions, ServiceLifetime lifetime = ServiceLifetime.Transient) 
        where T : IHandler
    {
        _workerRegistration.HandlerMapping[functionName] = (new HandlerOptions
        {
            Type = typeof(T),
            Serializer = jobSerializer,
            Batch = batchOptions,
        }, lifetime);
        return this;
    }
    
    public WorkerConfigurator SetBatchHandler<T>(string functionName, BatchOptions batchOptions, ServiceLifetime lifetime = ServiceLifetime.Transient) 
        where T : IHandler
    {
        _workerRegistration.HandlerMapping[functionName] = (new HandlerOptions
        {
            Type = typeof(T),
            Batch = batchOptions,
        }, lifetime);
        return this;
    }
    
    public void SetPipeline(Action<WorkerPipelineBuilder> builder)
    {
        builder(_workerRegistration.PipelineBuilder);
    }
}