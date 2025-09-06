using GearQueue.Worker;
using NSubstitute;
using static GearQueue.UnitTests.Worker.JobContextUtils;

namespace GearQueue.UnitTests.Worker.Batch;

/// <summary>
/// Contains cases where the batch manager has incomplete batches
/// </summary>
public partial class BatchJobManagerTests
{
    [Fact]
    public void TryGetJobs_ShouldReturnCorrectTimeout_WhenReceivesNullJobAndHasIncompleteBatches()
    {
        // Arrange
        var timeLimit = TimeSpan.FromSeconds(5);
        _pendingBatches.Add(new BatchJobManager.BatchData
        {
            Created = new DateTimeOffset(
                2025, 1, 1, 0, 0, 0, 0, TimeSpan.Zero),
            Jobs = [(_connectionId, CreateJobAssign("test-batch"))]
        });
        _timeProvider.GetUtcNow().Returns(new DateTimeOffset(
            2025, 1, 1, 0, 0, 4, 0, TimeSpan.Zero));

        var sut = SetupSut("test-batch", CreateOptions(10, timeLimit));
        
        // Act
        var (timeout, completedJobs) = sut.TryGetJobs(_connectionId, null);

        // Assert
        Assert.Equal(TimeSpan.FromSeconds(1), timeout);
        Assert.Null(completedJobs);
    }
    
    [Fact]
    public void TryGetJobs_ShouldReturnNullTimeoutAndCompletedBatch_WhenReceivesNullJobAndIncompleteBatchReachesTimeout()
    {
        // Arrange
        var timeLimit = TimeSpan.FromSeconds(5);
        _pendingBatches.Add(new BatchJobManager.BatchData
        {
            Created = new DateTimeOffset(
                2025, 1, 1, 0, 0, 0, 0, TimeSpan.Zero),
            Jobs = [(_connectionId, CreateJobAssign("test-batch"))]
        });
        _timeProvider.GetUtcNow().Returns(new DateTimeOffset(
            2025, 1, 1, 0, 0, 5, 0, TimeSpan.Zero));

        var sut = SetupSut("test-batch", CreateOptions(10, timeLimit));
        
        // Act
        var (timeout, completedJobs) = sut.TryGetJobs(_connectionId, null);

        // Assert
        var jobs = completedJobs?.ToList();
        Assert.Null(timeout);
        Assert.NotNull(jobs);
        Assert.Single(jobs);
        Assert.Null(jobs.Single().BatchKey);
        Assert.Single(jobs.Single().Batches);
    }
    
    [Fact]
    public void TryGetJobs_ShouldReturnCorrectTimeout_WhenReceivesIrrelevantJobAndHasIncompleteBatches()
    {
        // Arrange
        var timeLimit = TimeSpan.FromSeconds(5);
        _pendingBatches.Add(new BatchJobManager.BatchData
        {
            Created = new DateTimeOffset(
                2025, 1, 1, 0, 0, 0, 0, TimeSpan.Zero),
            Jobs = [(_connectionId, CreateJobAssign("test-batch"))]
        });
        _timeProvider.GetUtcNow().Returns(new DateTimeOffset(
            2025, 1, 1, 0, 0, 4, 0, TimeSpan.Zero));

        var sut = SetupSut("test-batch", CreateOptions(10, timeLimit));
        
        // Act
        var (timeout, completedJobs) = sut.TryGetJobs(_connectionId,  CreateJobAssign("different-one"));

        // Assert
        Assert.Equal(TimeSpan.FromSeconds(1), timeout);
        Assert.Null(completedJobs);
    }
    
    [Fact]
    public void TryGetJobs_ShouldReturnNullTimeoutAndCompletedBatch_WhenReceivesIrrelevantJobAndIncompleteBatchReachesTimeout()
    {
        // Arrange
        var timeLimit = TimeSpan.FromSeconds(5);
        _pendingBatches.Add(new BatchJobManager.BatchData
        {
            Created = new DateTimeOffset(
                2025, 1, 1, 0, 0, 0, 0, TimeSpan.Zero),
            Jobs = [(_connectionId, CreateJobAssign("test-batch"))]
        });
        _timeProvider.GetUtcNow().Returns(new DateTimeOffset(
            2025, 1, 1, 0, 0, 5, 0, TimeSpan.Zero));

        var sut = SetupSut("test-batch", CreateOptions(10, timeLimit));
        
        // Act
        var (timeout, completedJobs) = sut.TryGetJobs(_connectionId,  CreateJobAssign("different-one"));

        // Assert
        var jobs = completedJobs?.ToList();
        Assert.Null(timeout);
        Assert.NotNull(jobs);
        Assert.Single(jobs);
        Assert.Null(jobs.Single().BatchKey);
        Assert.Single(jobs.Single().Batches);
    }
    
    [Fact]
    public void TryGetJobs_ShouldReturnCorrectTimeout_WhenReceivesRelevantJobAndHasIncompleteBatches()
    {
        // Arrange
        var timeLimit = TimeSpan.FromSeconds(5);
        _pendingBatches.Add(new BatchJobManager.BatchData
        {
            Created = new DateTimeOffset(
                2025, 1, 1, 0, 0, 0, 0, TimeSpan.Zero),
            Jobs = [(_connectionId, CreateJobAssign("test-batch"))]
        });
        _timeProvider.GetUtcNow().Returns(new DateTimeOffset(
            2025, 1, 1, 0, 0, 4, 0, TimeSpan.Zero));

        var sut = SetupSut("test-batch", CreateOptions(10, timeLimit));
        
        // Act
        var (timeout, completedJobs) = sut.TryGetJobs(_connectionId,  CreateJobAssign("test-batch"));

        // Assert
        Assert.Equal(TimeSpan.FromSeconds(1), timeout);
        Assert.Null(completedJobs);
    }
    
    [Fact]
    public void TryGetJobs_ShouldReturnNullTimeoutAndCompletedBatch_WhenReceivesRelevantJobAndIncompleteBatchReachesTimeout()
    {
        // Arrange
        var timeLimit = TimeSpan.FromSeconds(5);
        _pendingBatches.Add(new BatchJobManager.BatchData
        {
            Created = new DateTimeOffset(
                2025, 1, 1, 0, 0, 0, 0, TimeSpan.Zero),
            Jobs = [(_connectionId, CreateJobAssign("test-batch"))]
        });
        _timeProvider.GetUtcNow().Returns(new DateTimeOffset(
            2025, 1, 1, 0, 0, 5, 0, TimeSpan.Zero));

        var sut = SetupSut("test-batch", CreateOptions(10, timeLimit));
        
        // Act
        var (timeout, completedJobs) = sut.TryGetJobs(_connectionId,  CreateJobAssign("test-batch"));

        // Assert
        var jobs = completedJobs?.ToList();
        Assert.Null(timeout);
        Assert.NotNull(jobs);
        Assert.Single(jobs);
        Assert.Null(jobs.Single().BatchKey);
        Assert.Equal(2, jobs.Single().Batches.Count());
    }
    
    [Fact]
    public void TryGetJobs_ShouldReturnNullTimeoutAndCompletedBatch_WhenReceivesRelevantJobAndThatCompletesBatch()
    {
        // Arrange
        var timeLimit = TimeSpan.FromSeconds(5);
        _pendingBatches.Add(new BatchJobManager.BatchData
        {
            Created = new DateTimeOffset(
                2025, 1, 1, 0, 0, 0, 0, TimeSpan.Zero),
            Jobs = [
                (_connectionId, CreateJobAssign("test-batch")),
                (_connectionId, CreateJobAssign("test-batch")),
                (_connectionId, CreateJobAssign("test-batch")),
                (_connectionId, CreateJobAssign("test-batch"))
            ]
        });
        _timeProvider.GetUtcNow().Returns(new DateTimeOffset(
            2025, 1, 1, 0, 0, 4, 0, TimeSpan.Zero));

        var sut = SetupSut("test-batch", CreateOptions(5, timeLimit));
        
        // Act
        var (timeout, completedJobs) = sut.TryGetJobs(_connectionId,  CreateJobAssign("test-batch"));

        // Assert
        var jobs = completedJobs?.ToList();
        Assert.Null(timeout);
        Assert.NotNull(jobs);
        Assert.Single(jobs);
        Assert.Null(jobs.Single().BatchKey);
        Assert.Equal(5, jobs.Single().Batches.Count());
    }
}