using GearQueue.Consumer.Pipeline;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace GearQueue.Extensions.Microsoft.DependencyInjection.Configurator;

public class ConsumerPipelineBuilder(IServiceCollection serviceCollection)
{
    private readonly List<Type> _middlewares = [];

    public ConsumerPipelineBuilder Use(Type middleware)
    {
        if (middleware == null)
        {
            throw new ArgumentNullException(nameof(middleware));
        }
        
        _middlewares.Add(middleware);
        serviceCollection.TryAddTransient(middleware);
        return this;
    }

    public ConsumerPipelineBuilder Use<T>() where T : class, IGearQueueMiddleware
    {
        return Use(typeof(T));
    }

    internal ConsumerPipeline Build<TExecutionMiddleware>(IServiceProvider serviceProvider) where TExecutionMiddleware : class, IGearQueueMiddleware 
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
        
        return new ConsumerPipeline(middlewares.ToArray());
    }
}