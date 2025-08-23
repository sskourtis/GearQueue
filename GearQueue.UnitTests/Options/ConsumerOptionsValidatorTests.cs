using GearQueue.Options;
using GearQueue.Options.Validation;

namespace GearQueue.UnitTests.Options;

public class ConsumerOptionsValidatorTests
{
    private readonly ConsumerOptionsValidator _sut = new();

    private ConsumerOptions CreateValidOptions()
    {
        return new ConsumerOptions
        {
            MaxConcurrency = 10,
            CreateScope = false,
            ConcurrencyStrategy = ConcurrencyStrategy.AcrossServers,
            Hosts =
            [
                new ConsumerHostsOptions
                {
                    Host = new HostOptions
                    {
                        Hostname = "localhost",
                        Port = 4730,
                        ConnectionTimeout = TimeSpan.FromSeconds(10),
                        ReceiveTimeout = TimeSpan.FromSeconds(10),
                        SendTimeout = TimeSpan.FromSeconds(10),
                    },
                    Connections = 1,
                    ReconnectTimeout = TimeSpan.FromSeconds(3),
                    UsePreSleep = true,
                    PollingDelay = TimeSpan.FromSeconds(2)
                }
            ]
        };
    }
    
    [Fact]
    public void Validate_ShouldSucceed_WithValidOptions()
    {
        // Arrange
        var options = CreateValidOptions();
        
        // Act
        var validationResult = _sut.Validate(options);
        
        // Assert
        Assert.True(validationResult.IsValid);
        Assert.Empty(validationResult.Errors);
    }
    
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-10)]
    [Theory]
    public void Validate_ShouldFail_WhenMaxConcurrencyIsZeroOrNegative(int maxConcurrency)
    {
        // Arrange
        var options = CreateValidOptions();
        options.MaxConcurrency = maxConcurrency;
        
        // Act
        var validationResult = _sut.Validate(options);
        
        // Assert
        Assert.False(validationResult.IsValid);
        Assert.NotEmpty(validationResult.Errors);
    }
    
    [InlineData(-1)]
    [InlineData(3)]
    [InlineData(4)]
    [Theory]
    public void Validate_ShouldFail_WhenConcurrencyStrategyIsInvalid(int strategy)
    {
        // Arrange
        var options = CreateValidOptions();
        options.ConcurrencyStrategy = (ConcurrencyStrategy) strategy;
        
        // Act
        var validationResult = _sut.Validate(options);
        
        // Assert
        Assert.False(validationResult.IsValid);
        Assert.NotEmpty(validationResult.Errors);
    }
    
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-10)]
    [Theory]
    public void Validate_ShouldFail_WhenConnectionsIsZeroOrNegative(int connection)
    {
        // Arrange
        var options = CreateValidOptions();
        options.Hosts[0].Connections = connection;
        
        // Act
        var validationResult = _sut.Validate(options);
        
        // Assert
        Assert.False(validationResult.IsValid);
        Assert.NotEmpty(validationResult.Errors);
    }
    
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-10)]
    [Theory]
    public void Validate_ShouldFail_WhenReconnectTimeoutIsZeroOrNegative(int seconds)
    {
        // Arrange
        var options = CreateValidOptions();
        options.Hosts[0].ReconnectTimeout = TimeSpan.FromSeconds(seconds);;
        
        // Act
        var validationResult = _sut.Validate(options);
        
        // Assert
        Assert.False(validationResult.IsValid);
        Assert.NotEmpty(validationResult.Errors);
    }
    
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-10)]
    [Theory]
    public void Validate_ShouldFail_WhenPollingDelayIsZeroOrNegative(int seconds)
    {
        // Arrange
        var options = CreateValidOptions();
        options.Hosts[0].PollingDelay = TimeSpan.FromSeconds(seconds);;
        options.Hosts[0].UsePreSleep = false;
        
        // Act
        var validationResult = _sut.Validate(options);
        
        // Assert
        Assert.False(validationResult.IsValid);
        Assert.NotEmpty(validationResult.Errors);
    }
}