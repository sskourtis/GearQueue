using GearQueue.Consumer;
using GearQueue.Extensions.Microsoft.DependencyInjection.HandlerExecutors;
using GearQueue.Options;
using GearQueue.Options.Parser;
using GearQueue.Options.Validation;
using GearQueue.Producer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GearQueue.Extensions.Microsoft.DependencyInjection.Configurator;

public class GearQueueConfigurator
{
    private readonly IServiceCollection _services;

    private Action<GearQueueProducerOptions>? _defaultProducerOptions;
    private Action<GearQueueConsumerOptions>? _defaultConsumerOptions;
    
    private readonly List<ConsumerRegistration> _consumerRegistrations = new();
    
    internal GearQueueConfigurator(IServiceCollection services)
    {
        _services = services;
    }

    internal void Setup()
    {
        foreach (var consumerRegistration in _consumerRegistrations)
        {
            AddConsumer(consumerRegistration);
        }

        if (_consumerRegistrations.Count > 0)
        {
            _services.AddHostedService<GearQueueHostedService>();
        }
    }

    public void SetDefaultProducerOptions(Action<GearQueueProducerOptions> options)
    {
        _defaultProducerOptions = options;
    }

    public void SetDefaultConsumerOptions(Action<GearQueueConsumerOptions> options)
    {
        _defaultConsumerOptions = options;
    }

    public void AddProducer(string connectionString, Action<GearQueueProducerOptions>? configure = null)
    {
        AddNamedProducer("default", connectionString, configure);
    }
    
    public void AddNamedProducer(string name, string connectionString, Action<GearQueueProducerOptions>? configure = null)
    {
        AddProducer(new ProducerRegistration
        {
            Name = name,
            ConnectionString = connectionString,
            ConfigureOptions = configure
        });
    }
    
    public void AddProducer(IConfigurationSection section, Action<GearQueueProducerOptions>? configure = null)
    {
        AddNamedProducer("default", section, configure);
    }
    
    public void AddNamedProducer(string name, IConfigurationSection section, Action<GearQueueProducerOptions>? configure = null)
    {
        AddProducer(new ProducerRegistration
        {
            Name = name,
            Section = section,
            ConfigureOptions = configure
        });
    }
    
    public void AddProducer(Action<GearQueueProducerOptions> configure)
    {
        AddNameProducer("default", configure);
    }

    public void AddNameProducer(string name, Action<GearQueueProducerOptions> configure)
    {
        AddProducer(new ProducerRegistration
        {
            Name = name,
            ConfigureOptions = configure
        });
    }

    private void AddProducer(ProducerRegistration registration)
    {
        if (registration.Section is not null)
        {
            _services.Configure<GearQueueProducerOptions>(registration.Name, registration.Section);
        }

        var creator = (IServiceProvider s) =>
        {
            GearQueueProducerOptions options;

            if (registration.Section is not null)
            {
                var optionsMonitor = s.GetRequiredService<IOptionsMonitor<GearQueueProducerOptions>>();

                options = optionsMonitor.Get(registration.Name);
            }
            else if (registration.ConnectionString is not null)
            {
                options = ConnectionStringParser.ParseToProducerOptions(registration.ConnectionString);
            }
            else
            {
                options = new GearQueueProducerOptions();
            }
            
            _defaultProducerOptions?.Invoke(options);
            registration.ConfigureOptions?.Invoke(options);
            
            new GearQueueProducerOptionsValidator()
                .Validate(options)
                .ThrowIfInvalid();
            
            var instance = new GearQueueProducer(options, s.GetRequiredService<ILoggerFactory>())
            {
                Name = registration.Name,
            };

            return instance;
        };

        if (registration.Name == "default")
        {
            _services.AddSingleton<IGearQueueProducer>(creator);    
        }
        else
        {
            _services.TryAddSingleton<IGearQueueProducerFactory, GearQueueProducerFactory>();
            _services.AddSingleton<INamedGearQueueProducer>(creator);
        }
    }

    public GearQueueConsumerConfigurator AddConsumer(Action<GearQueueConsumerOptions> configure)
    {
        var registration = new ConsumerRegistration
        {
            ConfigureOptions = configure,
        };
        
        _consumerRegistrations.Add(registration);
        
        return new GearQueueConsumerConfigurator(registration);
    }

    public GearQueueConsumerConfigurator AddConsumer(IConfigurationSection section, Action<GearQueueConsumerOptions>? configure = null)
    {
        var registration = new ConsumerRegistration
        {
            ConfigureOptions = configure,
            Section = section,
        };
        
        _consumerRegistrations.Add(registration);
        
        return new GearQueueConsumerConfigurator(registration);
    }
    
    public GearQueueConsumerConfigurator AddConsumer(string? connectionString, Action<GearQueueConsumerOptions>? configure = null)
    {
        var registration = new ConsumerRegistration
        {
            ConfigureOptions = configure,
            ConnectionString = connectionString,
        };
        
        _consumerRegistrations.Add(registration);
        
        return new GearQueueConsumerConfigurator(registration);
    }
    
    private void AddConsumer(ConsumerRegistration registration)
    {
        var uniqueName = Guid.NewGuid().ToString();
        
        foreach (var (_, (type, lifetime)) in registration.HandlerMapping)
        {
            _services.Add(new ServiceDescriptor(type, type, lifetime));    
        }

        if (registration.Section is not null)
        {
            _services.Configure<GearQueueConsumerOptions>(uniqueName, registration.Section);    
        }
        
        _services.AddSingleton<IGearQueueConsumer>(s =>
        {
            GearQueueConsumerOptions options;

            if (registration.Section is not null)
            {
                var optionsMonitor = s.GetRequiredService<IOptionsMonitor<GearQueueConsumerOptions>>();

                options = optionsMonitor.Get(uniqueName);    
            }
            else if (registration.ConnectionString is not null)
            {
                options = ConnectionStringParser.ParseToConsumerOptions(registration.ConnectionString);
            }
            else
            {
                options = new GearQueueConsumerOptions();
            }
            
            _defaultConsumerOptions?.Invoke(options);
            registration.ConfigureOptions?.Invoke(options);
            
            new GearQueueConsumerOptionsValidator()
                .Validate(options)
                .ThrowIfInvalid();
            
            if (registration.HandlerMapping.Count == 0)
            {
                throw new ArgumentException("There must be at least one handler mapping");
            }
            
            IGearQueueHandlerExecutor handlerExecutor = options.CreateScope
                ? new GearQueueMicrosoftScopedHandlerExecutor(s.GetRequiredService<IServiceScopeFactory>())
                : new GearQueueMicrosoftHandlerExecutor(s.GetRequiredService<IServiceProvider>());

            if (options.Batch is not null && registration.HandlerMapping.Count != 1)
            {
                throw new ArgumentException("Batch consumer must have exactly one handler");
            }

            return new GearQueueConsumer(
                options,
                handlerExecutor,
                registration.HandlerMapping.ToDictionary(x => x.Key, x => x.Value.Item1),
                s.GetRequiredService<ILoggerFactory>());
        });
    }
}