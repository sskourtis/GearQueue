using GearQueue.Consumer;
using NSubstitute;

namespace GearQueue.UnitTests.Consumer.Batch;

using static JobContextUtils;

public partial class BatchJobManagerTests
{
    [Fact]
    public void TryGetJobs_ShouldWork_WhenConfiguredByKeyFalse()
    {
        // Arrange
        const string functionName = "test-batch";
        const int batchSize = 5;
        var timeLimit = TimeSpan.FromSeconds(5);

        var startingTime = new DateTimeOffset(
            2025, 1, 1, 0, 0, 4, 0, TimeSpan.Zero);
        
        _timeProvider.GetUtcNow().Returns(startingTime);
        var sut = SetupSut(functionName, CreateOptions(batchSize, timeLimit));
        
        TimeSpan? timeout;
        IEnumerable<JobContext>? completedBatches;
        List<JobContext>? completedJobs;
        
        // Case 1. Completed before timeout
        // --------------------------------
        for (var i = 0; i < batchSize - 1; i++)
        {
            // Act
            (timeout, completedBatches) = sut.TryGetJobs(_connectionId, CreateJobAssign(functionName));
            
            // Assert
            Assert.Equal(TimeSpan.FromSeconds(5), timeout);
            Assert.Null(completedBatches);
        }
        
        // Act
        (timeout, completedBatches) = sut.TryGetJobs(_connectionId, CreateJobAssign(functionName));
            
        // Assert
        completedJobs = completedBatches?.ToList();
        Assert.Null(timeout);
        Assert.NotNull(completedJobs);
        Assert.Single(completedJobs);
        Assert.Equal(batchSize, completedJobs.Single().Batches.Count());;
        
        // Case 1. Completed with timeout
        // --------------------------------
        for (var i = 0; i < batchSize - 2; i++)
        {
            // Act
            (timeout, completedBatches) = sut.TryGetJobs(_connectionId, CreateJobAssign(functionName));
            
            // Assert
            Assert.Equal(TimeSpan.FromSeconds(5), timeout);
            Assert.Null(completedBatches);
        }
        
        _timeProvider.GetUtcNow().Returns(startingTime.AddSeconds(5));
        // Act
        (timeout, completedBatches) = sut.TryGetJobs(_connectionId, CreateJobAssign(functionName));
            
        // Assert
        completedJobs = completedBatches?.ToList();
        Assert.Null(timeout);
        Assert.NotNull(completedJobs);
        Assert.Single(completedJobs);
        Assert.Equal(batchSize - 1, completedJobs.Single().Batches.Count());;
    }
    
        [Fact]
    public void TryGetJobs_ShouldWork_WhenConfiguredByKeyTrue()
    {
        // Arrange
        const string functionName = "test-batch";
        const int batchSize = 5;
        var timeLimit = TimeSpan.FromSeconds(5);

        var startingTime = new DateTimeOffset(
            2025, 1, 1, 0, 0, 0, 0, TimeSpan.Zero);
        
        _timeProvider.GetUtcNow().Returns(startingTime);
        var sut = SetupSut(functionName, CreateOptionsByKey(batchSize, timeLimit));
        
        TimeSpan? timeout;
        IEnumerable<JobContext>? completedBatches;
        List<JobContext>? completedJobs;
        
        // Case 1. Completed before timeout
        // --------------------------------
        for (var i = 0; i < batchSize - 1; i++)
        {
            // Act
            (timeout, completedBatches) = sut.TryGetJobs(_connectionId, CreateJobAssignWithKey(functionName, "key1"));
            
            // Assert
            Assert.Equal(TimeSpan.FromSeconds(5), timeout);
            Assert.Null(completedBatches);
        }
        
        for (var i = 0; i < batchSize - 1; i++)
        {
            // Act
            (timeout, completedBatches) = sut.TryGetJobs(_connectionId, CreateJobAssignWithKey(functionName, "key2"));
            
            // Assert
            Assert.Equal(TimeSpan.FromSeconds(5), timeout);
            Assert.Null(completedBatches);
        }
        
        // Act
        (timeout, completedBatches) = sut.TryGetJobs(_connectionId, CreateJobAssignWithKey(functionName, "key1"));
            
        // Assert
        completedJobs = completedBatches?.ToList();
        Assert.Equal(TimeSpan.FromSeconds(5), timeout);
        Assert.NotNull(completedJobs);
        Assert.Single(completedJobs);
        Assert.Equal(batchSize, completedJobs.Single().Batches.Count());;
        Assert.Equal("key1", completedJobs.Single().BatchKey);
        
        // Act
        (timeout, completedBatches) = sut.TryGetJobs(_connectionId, CreateJobAssignWithKey(functionName, "key2"));
            
        // Assert
        completedJobs = completedBatches?.ToList();
        Assert.Null(timeout);
        Assert.NotNull(completedJobs);
        Assert.Single(completedJobs);
        Assert.Equal(batchSize, completedJobs.Single().Batches.Count());;
        Assert.Equal("key2", completedJobs.Single().BatchKey);
        
        // Case 1. Completed with timeout
        // --------------------------------
        for (var i = 0; i < batchSize - 1; i++)
        {
            // Act
            (timeout, completedBatches) = sut.TryGetJobs(_connectionId, CreateJobAssignWithKey(functionName, "key3"));
            
            // Assert
            Assert.Equal(TimeSpan.FromSeconds(5), timeout);
            Assert.Null(completedBatches);
        }
        
        for (var i = 0; i < batchSize - 1; i++)
        {
            // Act
            (timeout, completedBatches) = sut.TryGetJobs(_connectionId, CreateJobAssignWithKey(functionName, "key1"));
            
            // Assert
            Assert.Equal(TimeSpan.FromSeconds(5), timeout);
            Assert.Null(completedBatches);
        }
        
        _timeProvider.GetUtcNow().Returns(startingTime.AddSeconds(5));
        // Act
        (timeout, completedBatches) = sut.TryGetJobs(_connectionId, CreateJobAssignWithKey(functionName, "key4"));
            
        // Assert
        completedJobs = completedBatches?.ToList();
        Assert.Equal(timeLimit, timeout);
        Assert.NotNull(completedJobs);
        Assert.Equal(2, completedJobs.Count);
        Assert.Equal(batchSize - 1, completedJobs[0].Batches.Count());;
        Assert.Equal(batchSize - 1, completedJobs[1].Batches.Count());;
        Assert.Equal(1, completedJobs.Count(j => j.BatchKey == "key3"));
        Assert.Equal(1, completedJobs.Count(j => j.BatchKey == "key1"));
    }
}