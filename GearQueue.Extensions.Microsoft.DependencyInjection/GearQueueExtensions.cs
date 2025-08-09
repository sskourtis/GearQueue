using GearQueue.Consumer;
using GearQueue.Producer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GearQueue.Extensions.Microsoft.DependencyInjection;

public static class GearQueueExtensions
{
    private static int _consumerCounter = 0;
    
    public static IServiceCollection AddGearQueueProducer(this IServiceCollection services, 
        IConfiguration configuration,
        string sectionName = "GearQueue:Producer")
    {
        var options = configuration.GetSection(sectionName).Get<GearQueueProducerOptions>();
        ArgumentNullException.ThrowIfNull(options);
        
        return services.AddGearQueueProducer(options);
    }

    public static IServiceCollection AddGearQueueProducer(this IServiceCollection services, GearQueueProducerOptions options)
    {
        services.AddSingleton<GearQueueProducer>(s => new GearQueueProducer(options, s.GetRequiredService<ILoggerFactory>()));;

        return services;
    }

    public static GearQueueHandlerMapping AddGearQueueConsumer(this IServiceCollection services,
        IConfiguration configuration,
        string sectionName = "GearQueue:Consumer")
    {
        var options = configuration.GetSection(sectionName).Get<GearQueueConsumerOptions>();
        ArgumentNullException.ThrowIfNull(options);
        
        return services.AddGearQueueConsumer(options);
    }
    

    public static GearQueueHandlerMapping AddGearQueueConsumer(this IServiceCollection services, GearQueueConsumerOptions options)
    {
        var consumerId = _consumerCounter++;

        if (options.CreateScope)
        {
            services.AddSingleton<IGearQueueHandlerExecutor, GearQueueMicrosoftScopedHandlerExecutor>();
        }
        else
        {
            services.AddSingleton<IGearQueueHandlerExecutor, GearQueueMicrosoftHandlerExecutor>();
        }
        
        services.AddHostedService(s =>
        {
            return new GearQueueHostedService(new GearQueueConsumer(
                options, 
                s.GetRequiredService<IGearQueueHandlerExecutor>(),
                s.GetServices<GearQueueHandlerRegistration>()
                    .Where(r => r.ConsumerId == consumerId)
                    .ToDictionary(r => r.Function, r => r.HandlerType),
                s.GetRequiredService<ILoggerFactory>()));
        });

        return new GearQueueHandlerMapping(services, consumerId);;
    }
    
    public static IServiceCollection AddGearQueueConsumer<T>(this IServiceCollection services,
        IConfiguration configuration,
        string sectionName,
        string function,
        ServiceLifetime handlerLifetime = ServiceLifetime.Scoped) where T : class, IGearQueueConsumerHandler
    {
        services.AddGearQueueConsumer(configuration, sectionName)
            .AddHandler<T>(function, handlerLifetime);

        return services;
    }
    
    public static IServiceCollection AddGearQueueConsumer<T>(this IServiceCollection services,
        GearQueueConsumerOptions options,
        string function,
        ServiceLifetime handlerLifetime = ServiceLifetime.Scoped) where T : class, IGearQueueConsumerHandler
    {
        services.AddGearQueueConsumer(options)
            .AddHandler<T>(function, handlerLifetime);

        return services;
    }

    public class GearQueueHandlerMapping(IServiceCollection services, int consumerId)
    {
        public GearQueueHandlerMapping AddHandler<T>(string function, ServiceLifetime lifetime = ServiceLifetime.Scoped) 
            where T : class, IGearQueueConsumerHandler
        {
            services.Add(new ServiceDescriptor(typeof(T), typeof(T), lifetime));
            services.AddSingleton(new GearQueueHandlerRegistration
            {
                Function = function,
                HandlerType = typeof(T),
                ConsumerId = consumerId
            });
            
            return this;
        }
    }

    private class GearQueueHandlerRegistration
    {
        public required string Function { get; init; }
        public required Type HandlerType { get; init; }
        public required int ConsumerId { get; init; }
    }
}