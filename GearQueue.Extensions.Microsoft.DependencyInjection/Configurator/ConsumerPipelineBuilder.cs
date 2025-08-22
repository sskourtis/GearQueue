using GearQueue.Consumer.Pipeline;
using GearQueue.Extensions.Microsoft.DependencyInjection.Providers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

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

    internal ConsumerPipeline Build(bool createScope, IServiceProvider serviceProvider)
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

        middlewares.Add(
            new HandlerExecutionMiddleware(serviceProvider.GetRequiredService<ILoggerFactory>(),
                new MicrosoftProviderFactory(createScope,
                    serviceProvider.GetRequiredService<IServiceProvider>())
            ));
        
        return new ConsumerPipeline(middlewares.ToArray());
    }
}