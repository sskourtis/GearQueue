using GearQueue.Options;
using GearQueue.Options.Builders;
using GearQueue.Options.Validation;
using GearQueue.Producer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GearQueue.Extensions.Microsoft.DependencyInjection;

public static class GearQueueProducerExtensions
{
    public static IServiceCollection AddGearQueueProducer(this IServiceCollection services, 
        IConfiguration configuration,
        string name = "default")
    {
        return services.AddGearQueueProducer(configuration.GetSection("GearQueue:Producer"), name);
    }

    public static IServiceCollection AddGearQueueProducer(
        this IServiceCollection services, 
        IConfigurationSection configurationSection,
        string name = "default")
    {
        services.Configure<GearQueueProducerOptions>(name, configurationSection);
        services.AddSingleton<IValidateOptions<GearQueueProducerOptions>, OptionsValidator>();

        if (name == "default")
        {
            services.AddSingleton<IGearQueueProducer>(s =>
            {
                var options = s.GetRequiredService<IOptionsMonitor<GearQueueProducerOptions>>();
            
                var instance = new GearQueueProducer(options.Get(name), s.GetRequiredService<ILoggerFactory>())
                {
                    Name = name,
                };

                return instance;
            });    
        }
        else
        {
            services.TryAddSingleton<IGearQueueProducerFactory, GearQueueProducerFactory>();
            
            services.AddSingleton<INamedGearQueueProducer>(s =>
            {
                var options = s.GetRequiredService<IOptionsMonitor<GearQueueProducerOptions>>();
            
                var instance = new GearQueueProducer(options.Get(name), s.GetRequiredService<ILoggerFactory>())
                {
                    Name = name,
                };

                return instance;
            });
        }

        return services;
    }
    
    public static IServiceCollection AddGearQueueProducer(
        this IServiceCollection services, 
        Action<GearQueueProducerOptionsBuilder> configure,
        string name = "default")
    {
        var optionsBuilder = new GearQueueProducerOptionsBuilder();
        configure(optionsBuilder);
        
        var options = optionsBuilder.Build();

        if (name == "default")
        {
            services.AddSingleton<IGearQueueProducer>(s => 
                new GearQueueProducer(options, s.GetRequiredService<ILoggerFactory>())
                {
                    Name = name
                });            
        }
        else
        {
            services.TryAddSingleton<IGearQueueProducerFactory, GearQueueProducerFactory>();
            
            services.AddSingleton<INamedGearQueueProducer>(s => 
                new GearQueueProducer(options, s.GetRequiredService<ILoggerFactory>())
                {
                    Name = name
                });
        }

        return services;
    }
    
    private class OptionsValidator : IValidateOptions<GearQueueProducerOptions>
    {
        private readonly GearQueueProducerOptionsValidator _validator = new();

        public ValidateOptionsResult Validate(string? name, GearQueueProducerOptions options)
        {
            var result = _validator.Validate(options);
        
            return result.IsValid 
                ? ValidateOptionsResult.Success 
                : ValidateOptionsResult.Fail(result.Errors);
        }
    }
}

