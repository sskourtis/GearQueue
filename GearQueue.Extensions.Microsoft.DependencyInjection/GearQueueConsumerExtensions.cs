using GearQueue.BatchConsumer;
using GearQueue.Consumer;
using GearQueue.Options;
using GearQueue.Options.Builders;
using GearQueue.Options.Validation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GearQueue.Extensions.Microsoft.DependencyInjection;

public static class GearQueueConsumerExtensions
{
    public static IServiceCollection AddGearQueueConsumer<T>(this IServiceCollection services,
        IConfigurationSection section,
        ServiceLifetime handlerLifetime = ServiceLifetime.Transient) where T : IGearQueueBaseHandler
    {
        var uniqueName = typeof(T).Name;
        
        services.Add(new ServiceDescriptor(typeof(T), typeof(T), handlerLifetime));
        services.Configure<GearQueueConsumerOptions>(uniqueName, section);
        services.TryAddSingleton<IValidateOptions<GearQueueConsumerOptions>, OptionsValidator>();
        
        services.AddHostedService<GearQueueHostedService<T>>(s =>
        {
            var optionsMonitor = s.GetRequiredService<IOptionsMonitor<GearQueueConsumerOptions>>();

            var options = optionsMonitor.Get(uniqueName);
            
            if (options.Batch is not null)
            {
                return new GearQueueHostedService<T>(new GearQueueBatchConsumer(
                    options,
                    options.CreateScope
                        ? new GearQueueMicrosoftBatchScopedHandlerExecutor(s.GetRequiredService<IServiceScopeFactory>())
                        : new GearQueueMicrosoftBatchHandlerExecutor(s.GetRequiredService<IServiceProvider>()),
                    typeof(T),
                    s.GetRequiredService<ILoggerFactory>()));
            }

            return new GearQueueHostedService<T>(new GearQueueConsumer(
                options,
                options.CreateScope
                    ? new GearQueueMicrosoftScopedHandlerExecutor(s.GetRequiredService<IServiceScopeFactory>())
                    : new GearQueueMicrosoftHandlerExecutor(s.GetRequiredService<IServiceProvider>()),
                typeof(T),
                s.GetRequiredService<ILoggerFactory>()));
        });

        return services;
    }

    public static IServiceCollection AddGearQueueConsumer<T>(this IServiceCollection services, 
        Action<GearQueueConsumerOptionsBuilder> configure,
        ServiceLifetime handlerLifetime = ServiceLifetime.Transient) 
        where T : IGearQueueBaseHandler
    {
        services.Add(new ServiceDescriptor(typeof(T), typeof(T), handlerLifetime));
        
        var builder =  new GearQueueConsumerOptionsBuilder();
        
        configure(builder);
        
        var options = builder.Build();

        if (options.Batch is not null)
        {
            services.AddHostedService<GearQueueHostedService<T>>(s => new GearQueueHostedService<T>(new GearQueueBatchConsumer(
                options,
                options.CreateScope
                    ? new GearQueueMicrosoftBatchScopedHandlerExecutor(s.GetRequiredService<IServiceScopeFactory>())
                    : new GearQueueMicrosoftBatchHandlerExecutor(s.GetRequiredService<IServiceProvider>()),
                typeof(T),
                s.GetRequiredService<ILoggerFactory>())));

            return services;
        }
        
        services.AddHostedService<GearQueueHostedService<T>>(s => new GearQueueHostedService<T>(new GearQueueConsumer(
            options,
            options.CreateScope
                ? new GearQueueMicrosoftScopedHandlerExecutor(s.GetRequiredService<IServiceScopeFactory>())
                : new GearQueueMicrosoftHandlerExecutor(s.GetRequiredService<IServiceProvider>()),
            typeof(T),
            s.GetRequiredService<ILoggerFactory>())));

        return services;
    }
    
    private class OptionsValidator : IValidateOptions<GearQueueConsumerOptions>
    {
        private readonly GearQueueConsumerOptionsValidator _validator = new();

        public ValidateOptionsResult Validate(string? name, GearQueueConsumerOptions options)
        {
            var result = _validator.Validate(options);
        
            return result.IsValid 
                ? ValidateOptionsResult.Success 
                : ValidateOptionsResult.Fail(result.Errors);
        }
    }
}