using GearQueue.Worker;
using NSubstitute;

namespace GearQueue.UnitTests.Worker.Batch;

using static JobContextUtils;

public partial class BatchJobManagerTests
{
    [Fact]
    public void TryGetJobs_ShouldReturnMinTimeout_WhenReceivesNullJobAndHasPendingBatches()
    {
        // Arrange
        var timeLimit = TimeSpan.FromSeconds(5);
        
        _pendingBatches.Add(new BatchJobManager.BatchData
        {
            Created = new DateTimeOffset(
                2025, 1, 1, 0, 0, 0, 0, TimeSpan.Zero),
            Jobs = [(_connectionId, CreateJobAssign("test-batch"))],
            Key = "key1",
        });
        
        _pendingBatches.Add(new BatchJobManager.BatchData
        {
            Created = new DateTimeOffset(
                2025, 1, 1, 0, 0, 2, 0, TimeSpan.Zero),
            Jobs = [(_connectionId, CreateJobAssign("test-batch"))],
            Key = "key2",
        });
        
        _timeProvider.GetUtcNow().Returns(new DateTimeOffset(
            2025, 1, 1, 0, 0, 4, 0, TimeSpan.Zero));

        var sut = SetupSut("test-batch", CreateOptionsByKey(10, timeLimit));
        
        // Act
        var (timeout, completedJobs) = sut.TryGetJobs(_connectionId, null);

        // Assert
        Assert.Equal(TimeSpan.FromSeconds(1), timeout);
        Assert.Null(completedJobs);
    }
    
    [Fact]
    public void TryGetJobs_ShouldReturnMinTimeoutAndCompletedJob_WhenReceivesNullJobAndHasPendingBatches()
    {
        // Arrange
        var timeLimit = TimeSpan.FromSeconds(5);
        
        _pendingBatches.Add(new BatchJobManager.BatchData
        {
            Created = new DateTimeOffset(
                2025, 1, 1, 0, 0, 0, 0, TimeSpan.Zero),
            Jobs = [(_connectionId, CreateJobAssign("test-batch"))],
            Key = "key1",
        });
        
        _pendingBatches.Add(new BatchJobManager.BatchData
        {
            Created = new DateTimeOffset(
                2025, 1, 1, 0, 0, 4, 0, TimeSpan.Zero),
            Jobs = [(_connectionId, CreateJobAssign("test-batch"))],
            Key = "key2",
        });
        
        _timeProvider.GetUtcNow().Returns(new DateTimeOffset(
            2025, 1, 1, 0, 0, 5, 0, TimeSpan.Zero));

        var sut = SetupSut("test-batch", CreateOptionsByKey(10, timeLimit));
        
        // Act
        var (timeout, completedJobs) = sut.TryGetJobs(_connectionId, null);

        // Assert
        var jobs = completedJobs?.ToList();
        Assert.Equal(TimeSpan.FromSeconds(4), timeout);
        Assert.NotNull(jobs);
        Assert.Single(jobs);
        Assert.Equal("key1", jobs.Single().BatchKey);
        Assert.Single(jobs.Single().Batches);
    }
    
    [Fact]
    public void TryGetJobs_ShouldReturnNullTimeoutAndCompletedJobs_WhenReceivesNullJobAndHasAllPendingBatchesTimeout()
    {
        // Arrange
        var timeLimit = TimeSpan.FromSeconds(5);
        
        _pendingBatches.Add(new BatchJobManager.BatchData
        {
            Created = new DateTimeOffset(
                2025, 1, 1, 0, 0, 0, 0, TimeSpan.Zero),
            Jobs = [(_connectionId, CreateJobAssign("test-batch"))],
            Key = "key1",
        });
        
        _pendingBatches.Add(new BatchJobManager.BatchData
        {
            Created = new DateTimeOffset(
                2025, 1, 1, 0, 0, 4, 0, TimeSpan.Zero),
            Jobs = [(_connectionId, CreateJobAssign("test-batch"))],
            Key = "key2",
        });
        
        _timeProvider.GetUtcNow().Returns(new DateTimeOffset(
            2025, 1, 1, 0, 0, 10, 0, TimeSpan.Zero));

        var sut = SetupSut("test-batch", CreateOptionsByKey(10, timeLimit));
        
        // Act
        var (timeout, completedJobs) = sut.TryGetJobs(_connectionId, null);

        // Assert
        var jobs = completedJobs?.ToList();
        Assert.Null(timeout);
        Assert.NotNull(jobs);
        Assert.Equal(2, jobs.Count);
        Assert.Equal(1, jobs.Count(j => j.BatchKey == "key1"));
        Assert.Equal(1, jobs.Count(j => j.BatchKey == "key2"));
        Assert.Single(jobs[0].Batches);
        Assert.Single(jobs[1].Batches);
    }
    
    [Fact]
    public void TryGetJobs_ShouldReturnMinTimeout_WhenReceivesIrrelevantJobAndHasPendingBatches()
    {
        // Arrange
        var timeLimit = TimeSpan.FromSeconds(5);
        
        _pendingBatches.Add(new BatchJobManager.BatchData
        {
            Created = new DateTimeOffset(
                2025, 1, 1, 0, 0, 2, 0, TimeSpan.Zero),
            Jobs = [(_connectionId, CreateJobAssign("test-batch"))],
            Key = "key2",
        });
        
        _pendingBatches.Add(new BatchJobManager.BatchData
        {
            Created = new DateTimeOffset(
                2025, 1, 1, 0, 0, 0, 0, TimeSpan.Zero),
            Jobs = [(_connectionId, CreateJobAssign("test-batch"))],
            Key = "key1",
        });
        
        _timeProvider.GetUtcNow().Returns(new DateTimeOffset(
            2025, 1, 1, 0, 0, 4, 0, TimeSpan.Zero));

        var sut = SetupSut("test-batch", CreateOptionsByKey(10, timeLimit));
        
        // Act
        var (timeout, completedJobs) = sut.TryGetJobs(_connectionId, CreateJobAssign("different-one"));

        // Assert
        Assert.Equal(TimeSpan.FromSeconds(1), timeout);
        Assert.Null(completedJobs);
    }
    
    [Fact]
    public void TryGetJobs_ShouldReturnMinTimeoutAndCompletedJob_WhenReceivesIrrelevantJobAndHasPendingBatches()
    {
        // Arrange
        var timeLimit = TimeSpan.FromSeconds(5);
        
        _pendingBatches.Add(new BatchJobManager.BatchData
        {
            Created = new DateTimeOffset(
                2025, 1, 1, 0, 0, 2, 0, TimeSpan.Zero),
            Jobs = [(_connectionId, CreateJobAssign("test-batch"))],
            Key = "key2",
        });
        
        _pendingBatches.Add(new BatchJobManager.BatchData
        {
            Created = new DateTimeOffset(
                2025, 1, 1, 0, 0, 0, 0, TimeSpan.Zero),
            Jobs = [(_connectionId, CreateJobAssign("test-batch"))],
            Key = "key1",
        });
        
        _timeProvider.GetUtcNow().Returns(new DateTimeOffset(
            2025, 1, 1, 0, 0, 5, 0, TimeSpan.Zero));

        var sut = SetupSut("test-batch", CreateOptionsByKey(10, timeLimit));
        
        // Act
        var (timeout, completedJobs) = sut.TryGetJobs(_connectionId, CreateJobAssign("different-one"));

        // Assert
        var jobs = completedJobs?.ToList();
        Assert.Equal(TimeSpan.FromSeconds(2), timeout);
        Assert.NotNull(jobs);
        Assert.Single(jobs);
        Assert.Equal("key1", jobs.Single().BatchKey);
        Assert.Single(jobs.Single().Batches);
    }
    
    [Fact]
    public void TryGetJobs_ShouldReturnNullTimeoutAndCompletedJobs_WhenReceivesIrrelevantJobAndHasAllPendingBatchesTimeout()
    {
        // Arrange
        var timeLimit = TimeSpan.FromSeconds(5);
        
        _pendingBatches.Add(new BatchJobManager.BatchData
        {
            Created = new DateTimeOffset(
                2025, 1, 1, 0, 0, 0, 0, TimeSpan.Zero),
            Jobs = [(_connectionId, CreateJobAssign("test-batch"))],
            Key = "key1",
        });
        
        _pendingBatches.Add(new BatchJobManager.BatchData
        {
            Created = new DateTimeOffset(
                2025, 1, 1, 0, 0, 4, 0, TimeSpan.Zero),
            Jobs = [(_connectionId, CreateJobAssign("test-batch"))],
            Key = "key2",
        });
        
        _timeProvider.GetUtcNow().Returns(new DateTimeOffset(
            2025, 1, 1, 0, 0, 10, 0, TimeSpan.Zero));

        var sut = SetupSut("test-batch", CreateOptionsByKey(10, timeLimit));
        
        // Act
        var (timeout, completedJobs) = sut.TryGetJobs(_connectionId, CreateJobAssign("different-one"));

        // Assert
        var jobs = completedJobs?.ToList();
        Assert.Null(timeout);
        Assert.NotNull(jobs);
        Assert.Equal(2, jobs.Count);
        Assert.Equal(1, jobs.Count(j => j.BatchKey == "key1"));
        Assert.Equal(1, jobs.Count(j => j.BatchKey == "key2"));
        Assert.Single(jobs[0].Batches);
        Assert.Single(jobs[1].Batches);
    }
}