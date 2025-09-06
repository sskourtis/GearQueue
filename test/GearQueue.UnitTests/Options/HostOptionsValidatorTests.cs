using GearQueue.Options;
using GearQueue.Options.Validation;

namespace GearQueue.UnitTests.Options;

public class HostOptionsValidatorTests
{
    private readonly HostOptionsValidator _sut = new();
    
    private HostOptions CreateValidOptions()
    {
        return new HostOptions
        {
            Hostname = "localhost",
            Port = 4730,
            ConnectionTimeout = TimeSpan.FromSeconds(10),
            ReceiveTimeout = TimeSpan.FromSeconds(10),
            SendTimeout = TimeSpan.FromSeconds(10),
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
    
    [InlineData("localhost")]
    [InlineData("gearman-server")]
    [InlineData("gearman.server.corp")]
    [InlineData("192.168.1.2")]
    [InlineData("1.1.1.1")]
    [Theory]
    public void Validate_ShouldSucceed_WithValidHostname(string hostname)
    {
        // Arrange
        var options = CreateValidOptions();
        options.Hostname = hostname;
        
        // Act
        var validationResult = _sut.Validate(options);
        
        // Assert
        Assert.True(validationResult.IsValid);
        Assert.Empty(validationResult.Errors);
    }
    
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(100)]
    [InlineData(1000)]
    [InlineData(4730)]
    [InlineData(10000)]
    [InlineData(65535)]
    [Theory]
    public void Validate_ShouldSucceed_WithValidPort(int port)
    {
        // Arrange
        var options = CreateValidOptions();
        options.Port = port;
        
        // Act
        var validationResult = _sut.Validate(options);
        
        // Assert
        Assert.True(validationResult.IsValid);
        Assert.Empty(validationResult.Errors);
    }
        
    [InlineData("not a valid domain")]
    [InlineData("")]
    [InlineData("   ")]
    [Theory]
    public void Validate_ShouldFail_WhenHostnameIsInvalid(string hostname)
    {
        // Arrange
        var options = CreateValidOptions();
        options.Hostname = hostname;
        
        // Act
        var validationResult = _sut.Validate(options);
        
        // Assert
        Assert.False(validationResult.IsValid);
        Assert.NotEmpty(validationResult.Errors);
    }
    
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-10)]
    [InlineData(65536)]
    [InlineData(100000)]
    [Theory]
    public void Validate_ShouldFail_WhenPortIsInvalid(int port)
    {
        // Arrange
        var options = CreateValidOptions();
        options.Port = port;
        
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
    public void Validate_ShouldFail_WhenConnectionTimeoutIsZeroOrNegative(int seconds)
    {
        // Arrange
        var options = CreateValidOptions();
        options.ConnectionTimeout = TimeSpan.FromSeconds(seconds);;
        
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
    public void Validate_ShouldFail_WhenReceiveTimeoutIsZeroOrNegative(int seconds)
    {
        // Arrange
        var options = CreateValidOptions();
        options.ReceiveTimeout = TimeSpan.FromSeconds(seconds);;
        
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
    public void Validate_ShouldFail_WhenSendTimeoutIsZeroOrNegative(int seconds)
    {
        // Arrange
        var options = CreateValidOptions();
        options.SendTimeout = TimeSpan.FromSeconds(seconds);;
        
        // Act
        var validationResult = _sut.Validate(options);
        
        // Assert
        Assert.False(validationResult.IsValid);
        Assert.NotEmpty(validationResult.Errors);
    }
}