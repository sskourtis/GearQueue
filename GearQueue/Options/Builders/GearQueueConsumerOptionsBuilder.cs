using GearQueue.Network;
using GearQueue.Options.Validation;

namespace GearQueue.Options.Builders;

public class GearQueueConsumerOptionsBuilder
{
    private readonly GearQueueConsumerOptions _options = new();
    
    public GearQueueConsumerOptionsBuilder WithCreateScope(bool createScope)
    {
        _options.CreateScope = createScope;
        
        return this;
    }

    public GearQueueConsumerOptionsBuilder AddServer(string hostname, int port)
    {
        _options.Servers.Add(new GearQueueConsumerServerOptions
        {
            ServerInfo = new ServerInfo
            {
                Hostname = hostname,
                Port = port
            }
        });

        return this;
    }

    public GearQueueConsumerOptionsBuilder AddServer(string hostname, int port, Action<GearQueueConsumerServiceOptionsBuilder> configure)
    {
        var serverOptions = new GearQueueConsumerServerOptions
        {
            ServerInfo = new ServerInfo
            {
                Hostname = hostname,
                Port = port
            }
        };
        
        var serverOptionsBuilder = new GearQueueConsumerServiceOptionsBuilder(serverOptions);
        
        configure(serverOptionsBuilder);
        
        _options.Servers.Add(serverOptions);

        return this;
    }
    
    public GearQueueConsumerOptionsBuilder WithBatching(int batchSize, TimeSpan timeLimit)
    {
        _options.Batch = new BatchOptions
        {
            TimeLimit = timeLimit,
            Size = batchSize
        };

        return this;
    }

    public GearQueueConsumerOptionsBuilder WithConcurrencyAcrossServers(int concurrency)
    {
        _options.MaxConcurrency = concurrency;
        _options.ConcurrencyStrategy = ConcurrencyStrategy.AcrossServers;

        return this;
    }
    
    public GearQueueConsumerOptionsBuilder WithConcurrencyPerConnection(int concurrency)
    {
        _options.MaxConcurrency = concurrency;
        _options.ConcurrencyStrategy = ConcurrencyStrategy.PerConnection;

        return this;
    }
    
    public GearQueueConsumerOptionsBuilder WithConcurrencyPerServer(int concurrency)
    {
        _options.MaxConcurrency = concurrency;
        _options.ConcurrencyStrategy = ConcurrencyStrategy.PerServer;

        return this;
    }
    
    internal GearQueueConsumerOptions Build()
    {
        var validator =  new GearQueueConsumerOptionsValidator();
        var result =  validator.Validate(_options);
        
        result.ThrowIfInvalid();
        
        return _options;
    }
}