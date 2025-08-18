using GearQueue.Options;
using GearQueue.Options.Parser;

namespace GearQueue.UnitTests.Options;

public class ConnectionStringParserTests
{
    [Fact]
    public void ParseToProducerOptions_ShouldSucceed_WithSingleHost()
    {
        // Arrange
        var connectionString =
            "Hosts=localhost:4740;DistributionStrategy=Random;ConnectionMaxAge=00:15:00;MaxConnections=100";
        
        // Act
        var options = ConnectionStringParser.ParseToProducerOptions(connectionString);
        
        // Assert
        Assert.NotNull(options);
        Assert.Single(options.ConnectionPools);
        Assert.Equal("localhost", options.ConnectionPools[0].Host.Hostname);
        Assert.Equal(4740, options.ConnectionPools[0].Host.Port);
        Assert.Equal(15, options.ConnectionPools[0].ConnectionMaxAge.TotalMinutes);
        Assert.Equal(100, options.ConnectionPools[0].MaxConnections);
        Assert.Equal(DistributionStrategy.Random, options.DistributionStrategy);
    }
    
    [Fact]
    public void ParseToProducerOptions_ShouldSucceed_WithDefaultPort()
    {
        // Arrange
        var connectionString =
            "Hosts=gearman-host;DistributionStrategy=2;ConnectionMaxAge=00:15:00;MaxConnections=100";
        
        // Act
        var options = ConnectionStringParser.ParseToProducerOptions(connectionString);
        
        // Assert
        Assert.NotNull(options);
        Assert.Single(options.ConnectionPools);
        Assert.Equal("gearman-host", options.ConnectionPools[0].Host.Hostname);
        Assert.Equal(4730, options.ConnectionPools[0].Host.Port);
        Assert.Equal(15, options.ConnectionPools[0].ConnectionMaxAge.TotalMinutes);
        Assert.Equal(100, options.ConnectionPools[0].MaxConnections);
        Assert.Equal(DistributionStrategy.Random, options.DistributionStrategy);
    }
    
    [Fact]
    public void ParseToProducerOptions_ShouldSucceed_WithMultipleHosts()
    {
        // Arrange
        var connectionString =
            "Hosts=gearman-host:4731,gearman-host2:4732;DistributionStrategy=PrimaryWithFailover;ConnectionMaxAge=00:15:00;MaxConnections=100";
        
        // Act
        var options = ConnectionStringParser.ParseToProducerOptions(connectionString);
        
        // Assert
        Assert.NotNull(options);
        Assert.Equal(2, options.ConnectionPools.Count);
        Assert.Equal(DistributionStrategy.PrimaryWithFailover, options.DistributionStrategy);
        
        Assert.Equal("gearman-host", options.ConnectionPools[0].Host.Hostname);
        Assert.Equal(4731, options.ConnectionPools[0].Host.Port);
        
        Assert.Equal("gearman-host2", options.ConnectionPools[1].Host.Hostname);
        Assert.Equal(4732, options.ConnectionPools[1].Host.Port);
        
        Assert.Equal(15, options.ConnectionPools[0].ConnectionMaxAge.TotalMinutes);
        Assert.Equal(100, options.ConnectionPools[0].MaxConnections);
        
        Assert.Equal(15, options.ConnectionPools[1].ConnectionMaxAge.TotalMinutes);
        Assert.Equal(100, options.ConnectionPools[1].MaxConnections);
    }
    
    [Fact]
    public void ParseToConsumerOptions_ShouldSucceed_WithMultipleHosts()
    {
        // Arrange
        var connectionString =
            "Hosts=gearman-host:4731,gearman-host2:4732;MaxConcurrency=10;ConcurrencyStrategy=PerConnection;Connections=10;ReconnectTimeout=00:00:02;ReceiveTimeout=00:20:00";
        
        // Act
        var options = ConnectionStringParser.ParseToConsumerOptions(connectionString);
        
        // Assert
        Assert.NotNull(options);
        Assert.Equal(2, options.Hosts.Count);
        Assert.Equal(10, options.MaxConcurrency);
        Assert.Equal(ConcurrencyStrategy.PerConnection, options.ConcurrencyStrategy);
        
        Assert.Equal("gearman-host", options.Hosts[0].Host.Hostname);
        Assert.Equal(4731, options.Hosts[0].Host.Port);
        
        Assert.Equal("gearman-host2", options.Hosts[1].Host.Hostname);
        Assert.Equal(4732, options.Hosts[1].Host.Port);
        
        Assert.Equal(10, options.Hosts[0].Connections);
        Assert.Equal(2, options.Hosts[0].ReconnectTimeout.TotalSeconds);
        Assert.Equal(20, options.Hosts[0].Host.ReceiveTimeout.TotalMinutes);
        
        Assert.Equal(10, options.Hosts[1].Connections);
        Assert.Equal(2, options.Hosts[1].ReconnectTimeout.TotalSeconds);
        Assert.Equal(20, options.Hosts[1].Host.ReceiveTimeout.TotalMinutes);
    }
    
    [Fact]
    public void ParseToConsumerOptions_ShouldSucceed_WithMultipleHostsAndExtraSpaces()
    {
        // Arrange
        var connectionString =
            "Hosts = gearman-host:4731  ,  gearman-host2:4732 ;  MaxConcurrency=10  ;  ConcurrencyStrategy = PerConnection ;  Connections  = 10 ;  ReconnectTimeout=  00:00:02 ";
        
        // Act
        var options = ConnectionStringParser.ParseToConsumerOptions(connectionString);
        
        // Assert
        Assert.NotNull(options);
        Assert.Equal(2, options.Hosts.Count);
        Assert.Equal(10, options.MaxConcurrency);
        Assert.Equal(ConcurrencyStrategy.PerConnection, options.ConcurrencyStrategy);
        
        Assert.Equal("gearman-host", options.Hosts[0].Host.Hostname);
        Assert.Equal(4731, options.Hosts[0].Host.Port);
        
        Assert.Equal("gearman-host2", options.Hosts[1].Host.Hostname);
        Assert.Equal(4732, options.Hosts[1].Host.Port);
        
        Assert.Equal(10, options.Hosts[0].Connections);
        Assert.Equal(2, options.Hosts[0].ReconnectTimeout.TotalSeconds);
        
        Assert.Equal(10, options.Hosts[1].Connections);
        Assert.Equal(2, options.Hosts[1].ReconnectTimeout.TotalSeconds);
    }
}