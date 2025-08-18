using GearQueue.Network;
using GearQueue.Options;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace GearQueue.UnitTests.Network;

public class ServerHealthTrackerTests
{
    private readonly GearQueueHostOptions _gearQueueHostOptions;
    private readonly ILoggerFactory _loggerFactory;

    public ServerHealthTrackerTests()
    {
        _gearQueueHostOptions = new GearQueueHostOptions
        {
            Hostname = "test-server",
            Port = 8080
        };
        
        var logger = Substitute.For<ILogger<ServerHealthTracker>>();
        _loggerFactory = Substitute.For<ILoggerFactory>();
        _loggerFactory.CreateLogger<ServerHealthTracker>().Returns(logger);
    }
    
    [Fact]
    public void Constructor_InitializesWithHealthyState()
    {
        // Arrange & Act
        var tracker = CreateTracker();
        
        // Assert
        Assert.True(tracker.IsHealthy);
        Assert.Equal(DateTimeOffset.MinValue, tracker.LastFailureTime);
    }
    
    [Fact]
    public void ReportSuccess_ResetsFailureCount()
    {
        // Arrange
        var tracker = CreateTracker();
        
        // Act - report some failures but below threshold
        tracker.ReportFailure();
        tracker.ReportFailure();
        
        // Still healthy
        Assert.True(tracker.IsHealthy);
        
        // Report success
        tracker.ReportSuccess();
        
        // Act - report one more failure
        tracker.ReportFailure();
        
        // Assert - should still be healthy since failure count was reset
        Assert.True(tracker.IsHealthy);
    }
    
    [Fact]
    public void ReportSuccess_TransitionsToHealthy_WhenUnhealthy()
    {
        // Arrange
        var tracker = CreateTracker();
        
        // Make tracker unhealthy
        for (int i = 0; i < 3; i++)
        {
            tracker.ReportFailure();
        }
        
        Assert.False(tracker.IsHealthy);
        
        // Act
        tracker.ReportSuccess();
        
        // Assert
        Assert.True(tracker.IsHealthy);
    }
    
    [Fact]
    public void ReportFailure_UpdatesLastFailureTime()
    {
        // Arrange
        var tracker = CreateTracker();
        var beforeFailure = DateTimeOffset.UtcNow;
        
        // Act
        tracker.ReportFailure();
        
        // Assert
        Assert.True(tracker.LastFailureTime > beforeFailure);
    }
    
    [Fact]
    public void ReportFailure_BecomesUnhealthy_AfterThresholdReached()
    {
        // Arrange
        var tracker = CreateTracker();
        
        // Act - report failures up to threshold
        for (int i = 0; i < 2; i++)
        {
            tracker.ReportFailure();
            Assert.True(tracker.IsHealthy);
        }
        
        // Report one more failure to exceed threshold
        tracker.ReportFailure();
        
        // Assert
        Assert.False(tracker.IsHealthy);
    }
    
    [Fact]
    public void ShouldTryConnection_ReturnsTrue_WhenHealthy()
    {
        // Arrange
        var tracker = CreateTracker();
        
        // Act & Assert
        Assert.True(tracker.ShouldTryConnection());
    }
    
    [Fact]
    public void ShouldTryConnection_ReturnsFalse_WhenUnhealthyAndIntervalNotElapsed()
    {
        // Arrange
        var tracker = CreateTracker();
        
        // Make tracker unhealthy
        for (int i = 0; i < 3; i++)
        {
            tracker.ReportFailure();
        }
        
        // Act & Assert
        Assert.False(tracker.ShouldTryConnection());
    }
    
    [Fact]
    public async Task ShouldTryConnection_ReturnsTrue_WhenUnhealthyAndIntervalElapsed()
    {
        // Arrange
        var tracker = CreateTracker(healthCheckInterval: TimeSpan.FromMilliseconds(50));
        
        // Make tracker unhealthy
        for (int i = 0; i < 3; i++)
        {
            tracker.ReportFailure();
        }
        
        // Wait for the health check interval to elapse
        await Task.Delay(100, TestContext.Current.CancellationToken);
        
        // Act
        var shouldTryConnection = tracker.ShouldTryConnection();
        
        // Assert
        Assert.True(shouldTryConnection);
    }

    [Fact]
    public void MultithreadedScenario_HandlesLockingCorrectly()
    {
        // Arrange
        var tracker = CreateTracker();
        var random = new Random();
        bool exceptionOccurred = false;
        
        // Act - simulate multiple threads reporting success/failure
        var actions = new Action[]
        {
            () => tracker.ReportSuccess(),
            () => tracker.ReportFailure(),
            () => _ = tracker.ShouldTryConnection()
        };
        
        // Run multiple operations in parallel
        Parallel.For(0, 1000, i =>
        {
            try
            {
                actions[random.Next(actions.Length)]();
            }
            catch
            {
                exceptionOccurred = true;
            }
        });
        
        // Assert - no exceptions should have occurred
        Assert.False(exceptionOccurred);
    }
    
    private ServerHealthTracker CreateTracker(
        int failureThreshold = 3, 
        TimeSpan? healthCheckInterval = null)
    {
        return new ServerHealthTracker(
            _gearQueueHostOptions,
            failureThreshold,
            healthCheckInterval ?? TimeSpan.FromMinutes(1),
            _loggerFactory);
    }
}