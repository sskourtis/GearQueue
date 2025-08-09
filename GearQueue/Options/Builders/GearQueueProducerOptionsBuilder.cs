using GearQueue.Network;
using GearQueue.Options.Validation;
using GearQueue.Producer;

namespace GearQueue.Options.Builders;

public sealed class GearQueueProducerOptionsBuilder
{
    private readonly GearQueueProducerOptions _options = new();

    public GearQueueProducerOptionsBuilder WithDistributionStrategy(DistributionStrategy strategy)
    {
        _options.DistributionStrategy = strategy;
        
        return this;
    }

    public GearQueueProducerOptionsBuilder AddServer(string hostname, int port)
    {
        _options.Servers.Add(new ConnectionPoolOptions
        {
            ServerInfo = new ServerInfo
            {
                Hostname = hostname,
                Port = port
            }
        });

        return this;
    }
    
    public GearQueueProducerOptionsBuilder AddServer(string hostname, int port, Action<ConnectionPoolOptions> configure)
    {
        var connectionPoolOptions = new ConnectionPoolOptions
        {
            ServerInfo = new ServerInfo
            {
                Hostname = hostname,
                Port = port
            }
        };
        
        configure(connectionPoolOptions);
        
        _options.Servers.Add(connectionPoolOptions);

        return this;
    }
    
    internal GearQueueProducerOptions Build()
    {
        var validator =  new GearQueueProducerOptionsValidator();
        var result =  validator.Validate(_options);
        
        result.ThrowIfInvalid();
        
        return _options;
    }
}