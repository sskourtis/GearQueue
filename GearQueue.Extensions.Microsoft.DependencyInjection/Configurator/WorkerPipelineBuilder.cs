using GearQueue.Worker.Pipeline;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace GearQueue.Extensions.Microsoft.DependencyInjection.Configurator;

public class WorkerPipelineBuilder(IServiceCollection serviceCollection)
{
    private readonly List<Type> _middlewares = [];

    public WorkerPipelineBuilder Use(Type middleware)
    {
        if (middleware == null)
        {
            throw new ArgumentNullException(nameof(middleware));
        }
        
        _middlewares.Add(middleware);
        serviceCollection.TryAddTransient(middleware);
        return this;
    }

    public WorkerPipelineBuilder Use<T>() where T : class, IGearQueueMiddleware
    {
        return Use(typeof(T));
    }

    internal WorkerPipeline Build<TExecutionMiddleware>(IServiceProvider serviceProvider) where TExecutionMiddleware : class, IGearQueueMiddleware 
    {
        var middlewares = _middlewares
            .Select(m =>
            {
                if (serviceProvider.GetRequiredService(m) is not IGearQueueMiddleware middleware)
                {
                    throw new InvalidOperationException($"Middleware of type {m.FullName} is not registered");
                }

                return middleware;
            })
            .ToList();

        // Add the final middleware, this is the one that will execute the handler.
        middlewares.Add(serviceProvider.GetRequiredService<TExecutionMiddleware>());
        
        return new WorkerPipeline(middlewares.ToArray());
    }
}