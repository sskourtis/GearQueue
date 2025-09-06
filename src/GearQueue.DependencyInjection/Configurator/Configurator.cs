using System.Reflection;
using GearQueue.DependencyInjection.Middlewares;
using GearQueue.Metrics;
using GearQueue.Options;
using GearQueue.Options.Parser;
using GearQueue.Options.Validation;
using GearQueue.Producer;
using GearQueue.Serialization;
using GearQueue.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProducerOptions = GearQueue.Options.ProducerOptions;

namespace GearQueue.DependencyInjection.Configurator;

public class Configurator
{
    private IGearQueueJobSerializer? _defaultSerializer;
    private readonly IServiceCollection _services;

    private Action<ProducerOptions>? _defaultProducerOptions;
    private Action<WorkerOptions>? _defaultWorkerOptions;
    
    private readonly List<WorkerRegistration> _workerRegistrations = new();
    
    internal Configurator(IServiceCollection services)
    {
        _services = services;
    }

    internal void Setup()
    {
        foreach (var registration in _workerRegistrations)
        {
            AddWorker(registration);
        }

        if (_workerRegistrations.Count > 0)
        {
            _services.AddHostedService<GearQueueHostedService>();
            _services.TryAddSingleton<ScopedHandlerExecutionMiddleware>();
            _services.TryAddSingleton<UnscopedHandlerExecutionMiddleware>();
        }
    }
    
    public void SetMetricsCollector<T>(MetricsOptions? metricsOptions = null) where T : class, IMetricsCollector
    {
        _services.TryAddSingleton(metricsOptions ?? new MetricsOptions());
        _services.TryAddSingleton<IMetricsCollector, T>();
    }
    
    public void SetMetricsCollector(IMetricsCollector metricsCollector)
    {
        _services.TryAddSingleton(metricsCollector);
    }

    public void SetDefaultSerializer(IGearQueueJobSerializer jobSerializer)
    {
        _defaultSerializer = jobSerializer; 
    }

    public void SetDefaultProducerOptions(Action<ProducerOptions> options)
    {
        _defaultProducerOptions = options;
    }

    public void SetDefaultWorkerOptions(Action<WorkerOptions> options)
    {
        _defaultWorkerOptions = options;
    }

    public void AddProducer(string connectionString, Action<ProducerOptions>? configure = null, IGearQueueJobSerializer? serializer = null)
    {
        AddNamedProducer("default", connectionString, configure, serializer);
    }
    
    public void AddNamedProducer(string name, string connectionString, Action<ProducerOptions>? configure = null, IGearQueueJobSerializer? serializer = null)
    {
        AddProducer(new ProducerRegistration
        {
            Name = name,
            ConnectionString = connectionString,
            ConfigureOptions = configure,
            Serializer = serializer,
        });
    }
    
    public void AddProducer(IConfigurationSection section, Action<ProducerOptions>? configure = null, IGearQueueJobSerializer? serializer = null)
    {
        AddNamedProducer("default", section, configure, serializer);
    }
    
    public void AddNamedProducer(string name, IConfigurationSection section, Action<ProducerOptions>? configure = null, IGearQueueJobSerializer? serializer = null)
    {
        AddProducer(new ProducerRegistration
        {
            Name = name,
            Section = section,
            ConfigureOptions = configure,
            Serializer = serializer,
        });
    }
    
    public void AddProducer(Action<ProducerOptions> configure, IGearQueueJobSerializer? serializer = null)
    {
        AddNameProducer("default", configure, serializer);
    }

    public void AddNameProducer(string name, Action<ProducerOptions> configure, IGearQueueJobSerializer? serializer = null)
    {
        AddProducer(new ProducerRegistration
        {
            Name = name,
            ConfigureOptions = configure,
            Serializer = serializer,
        });
    }

    private void AddProducer(ProducerRegistration registration)
    {
        if (registration.Section is not null)
        {
            _services.Configure<ProducerOptions>(registration.Name, registration.Section);
        }

        var creator = (IServiceProvider s) =>
        {
            ProducerOptions options;

            if (registration.Section is not null)
            {
                var optionsMonitor = s.GetRequiredService<IOptionsMonitor<ProducerOptions>>();

                options = optionsMonitor.Get(registration.Name);
            }
            else if (registration.ConnectionString is not null)
            {
                options = ConnectionStringParser.ParseToProducerOptions(registration.ConnectionString);
            }
            else
            {
                options = new ProducerOptions();
            }
            
            _defaultProducerOptions?.Invoke(options);
            registration.ConfigureOptions?.Invoke(options);
            
            new ProducerOptionsValidator()
                .Validate(options)
                .ThrowIfInvalid();
            
            var instance = new Producer.Producer(
                options, 
                registration.Serializer ?? _defaultSerializer,
                s.GetRequiredService<ILoggerFactory>(),
                s.GetService<IMetricsCollector>())
            {
                Name = registration.Name,
            };

            return instance;
        };

        if (registration.Name == "default")
        {
            _services.AddSingleton<IProducer>(creator);    
        }
        else
        {
            _services.TryAddSingleton<IProducerFactory, ProducerFactory>();
            _services.AddSingleton<INamedProducer>(creator);
        }
    }
    
    public void RegisterTypedProducerFromAssembly(Assembly assembly, IGearQueueJobSerializer? serializer = null)
    {
        var foundTypes = assembly.GetTypes()
            .Select(t => new
            {
                Type = t,
                Function = t.GetCustomAttribute<GearQueueJobAttribute>()?.FunctionName
            })
            .Where(t => t.Function is not null);

        foreach (var typeInfo in foundTypes)
        {
            var serviceType = typeof(IProducer<>).MakeGenericType(typeInfo.Type);
        
            var implementationType = typeof(Producer<>).MakeGenericType(typeInfo.Type);
        
            _services.TryAddSingleton(serviceType, s =>
            {
                var baseProducer = s.GetRequiredService<IProducer>();
                return Activator.CreateInstance(implementationType, typeInfo.Function, serializer ?? _defaultSerializer, baseProducer)!;
            });
        }
    }

    public void RegisterTypedProducer<TJob>(string functionName, IGearQueueJobSerializer? serializer = null)
    {
        _services.TryAddSingleton<IProducer<TJob>>(
            s => new Producer<TJob>(functionName,
                (serializer ?? _defaultSerializer)!,
                s.GetRequiredService<IProducer>()));
    }
    
    public void RegisterTypedProducer<TJob>(string functionName, string producerName, IGearQueueJobSerializer? serializer = null)
    {
        _services.TryAddSingleton<IProducer<TJob>>(
            s =>
            {
                var producerFactory = s.GetRequiredService<IProducerFactory>();
                
                return new Producer<TJob>(functionName,
                    (serializer ?? _defaultSerializer)!,
                    producerFactory.GetRequired(producerName));
            });
    }
    
    public void RegisterTypedProducerFromAssembly(Assembly assembly, string producerName, IGearQueueJobSerializer? serializer = null)
    {
        var foundTypes = assembly.GetTypes()
            .Select(t => new
            {
                Type = t,
                Function = t.GetCustomAttribute<GearQueueJobAttribute>()?.FunctionName
            })
            .Where(t => t.Function is not null);

        foreach (var typeInfo in foundTypes)
        {
            var serviceType = typeof(IProducer<>).MakeGenericType(typeInfo.Type);
        
            var implementationType = typeof(Producer<>).MakeGenericType(typeInfo.Type);
        
            _services.TryAddSingleton(serviceType, s =>
            {
                var producerFactory = s.GetRequiredService<IProducerFactory>();
                
                return Activator.CreateInstance(implementationType, typeInfo.Function, serializer ?? _defaultSerializer, producerFactory.GetRequired(producerName))!;
            });
        }
    }

    public WorkerConfigurator AddWorker(Action<WorkerOptions> configure)
    {
        var registration = new WorkerRegistration
        {
            ConfigureOptions = configure,
            PipelineBuilder = new WorkerPipelineBuilder(_services)
        };
        
        _workerRegistrations.Add(registration);
        
        return new WorkerConfigurator(registration);
    }

    public WorkerConfigurator AddWorker(IConfigurationSection section, Action<WorkerOptions>? configure = null)
    {
        var registration = new WorkerRegistration
        {
            ConfigureOptions = configure,
            Section = section,
            PipelineBuilder = new WorkerPipelineBuilder(_services)
        };
        
        _workerRegistrations.Add(registration);
        
        return new WorkerConfigurator(registration);
    }
    
    public WorkerConfigurator AddWorker(string? connectionString, Action<WorkerOptions>? configure = null)
    {
        var registration = new WorkerRegistration
        {
            ConfigureOptions = configure,
            ConnectionString = connectionString,
            PipelineBuilder = new WorkerPipelineBuilder(_services)
        };
        
        _workerRegistrations.Add(registration);
        
        return new WorkerConfigurator(registration);
    }
    
    private void AddWorker(WorkerRegistration registration)
    {
        var uniqueName = Guid.NewGuid().ToString();
        
        foreach (var (_, (handlerOptions, lifetime)) in registration.HandlerMapping)
        {
            _services.Add(new ServiceDescriptor(handlerOptions.Type, handlerOptions.Type, lifetime));    
        }

        if (registration.Section is not null)
        {
            _services.Configure<WorkerOptions>(uniqueName, registration.Section);    
        }
        
        _services.AddSingleton<IWorker>(s =>
        {
            WorkerOptions options;

            if (registration.Section is not null)
            {
                var optionsMonitor = s.GetRequiredService<IOptionsMonitor<WorkerOptions>>();

                options = optionsMonitor.Get(uniqueName);    
            }
            else if (registration.ConnectionString is not null)
            {
                options = ConnectionStringParser.ParseToWorkerOptions(registration.ConnectionString);
            }
            else
            {
                options = new WorkerOptions();
            }
            
            _defaultWorkerOptions?.Invoke(options);
            registration.ConfigureOptions?.Invoke(options);
            
            new WorkerOptionsValidator()
                .Validate(options)
                .ThrowIfInvalid();
            
            if (registration.HandlerMapping.Count == 0)
            {
                throw new ArgumentException("There must be at least one handler mapping");
            }
           
            return new Worker.Worker(
                options,
                options.CreateScope 
                    ? registration.PipelineBuilder.Build<ScopedHandlerExecutionMiddleware>(s)
                    : registration.PipelineBuilder.Build<UnscopedHandlerExecutionMiddleware>(s),
                registration.HandlerMapping.ToDictionary(x => x.Key, x =>
                {
                    x.Value.Item1.Serializer ??= _defaultSerializer;

                    return x.Value.Item1;
                }),
                s.GetRequiredService<ILoggerFactory>(),
                s.GetService<IMetricsCollector>());
        });
    }
}