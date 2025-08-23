using GearQueue.Options;
using GearQueue.Options.Validation;

namespace GearQueue.UnitTests.Options;

public class ProducerOptionsValidatorTests
{
    private readonly ProducerOptionsValidator _sut = new();

    private ProducerOptions CreateValidOptions()
    {
        return new ProducerOptions()
        {
            DistributionStrategy = DistributionStrategy.RoundRobin,
            ConnectionPools =
            [
                new ConnectionPoolOptions
                {
                    Host = new HostOptions
                    {
                        Hostname = "localhost",
                        Port = 4730,
                        ConnectionTimeout = TimeSpan.FromSeconds(10),
                        ReceiveTimeout = TimeSpan.FromSeconds(10),
                        SendTimeout = TimeSpan.FromSeconds(10),
                    },
                    ConnectionMaxAge = TimeSpan.FromMinutes(5),
                    HealthCheckInterval = TimeSpan.FromSeconds(10),
                    HealthErrorThreshold = 3,
                    MaxConnections = 50,
                    NewConnectionTimeout = TimeSpan.FromSeconds(10),
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

    [InlineData(-1)]
    [InlineData(3)]
    [InlineData(4)]
    [Theory]
    public void Validate_ShouldFail_WhenDistributionStrategyIsInvalid(int strategy)
    {
        // Arrange
        var options = CreateValidOptions();
        options.DistributionStrategy = (DistributionStrategy) strategy;
        
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
    public void Validate_ShouldFail_WhenConnectionMaxAgeIsZeroOrNegative(int seconds)
    {
        // Arrange
        var options = CreateValidOptions();
        options.ConnectionPools[0].ConnectionMaxAge = TimeSpan.FromSeconds(seconds);;
        
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
    public void Validate_ShouldFail_WhenNewConnectionTimeoutIsZeroOrNegative(int seconds)
    {
        // Arrange
        var options = CreateValidOptions();
        options.ConnectionPools[0].NewConnectionTimeout = TimeSpan.FromSeconds(seconds);;
        
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
    public void Validate_ShouldFail_WhenHealthCheckIntervalIsZeroOrNegative(int seconds)
    {
        // Arrange
        var options = CreateValidOptions();
        options.ConnectionPools[0].HealthCheckInterval = TimeSpan.FromSeconds(seconds);;
        
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
    public void Validate_ShouldFail_WhenMaxConnectionsIsZeroOrNegative(int connection)
    {
        // Arrange
        var options = CreateValidOptions();
        options.ConnectionPools[0].MaxConnections = connection;
        
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
    public void Validate_ShouldFail_WhenHealthErrorThresholdIsZeroOrNegative(int threshold)
    {
        // Arrange
        var options = CreateValidOptions();
        options.ConnectionPools[0].HealthErrorThreshold = threshold;
        
        // Act
        var validationResult = _sut.Validate(options);
        
        // Assert
        Assert.False(validationResult.IsValid);
        Assert.NotEmpty(validationResult.Errors);
    }
}