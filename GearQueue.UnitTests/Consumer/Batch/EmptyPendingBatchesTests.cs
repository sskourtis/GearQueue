namespace GearQueue.UnitTests.Consumer.Batch;

using static JobContextUtils;

/// <summary>
/// Contains cases where the batch manager had no pending batches
/// </summary>
public partial class BatchJobManagerTests
{
    [Fact]
    public void TryGetJobs_ShouldReturnNullResult_WhenReceivesNullJobAndHasNoPendingBatches()
    {
        // Arrange
        var sut = SetupSut("test-batch", CreateOptions(10));
        
        // Act
        var (timeout, completedJobs) = sut.TryGetJobs(_connectionId, null);

        // Assert
        Assert.Null(timeout);
        Assert.Null(completedJobs);
    }
    
    [Fact]
    public void TryGetJobs_ShouldReturnNullResult_WhenReceivesIrrelevantJobAndHasNoPendingBatches()
    {
        // Arrange
        var sut = SetupSut("test-batch", CreateOptions(10));
        
        // Act
        var (timeout, completedJobs) = sut.TryGetJobs(_connectionId, CreateJobAssign("different-one"));

        // Assert
        Assert.Null(timeout);
        Assert.Null(completedJobs);
    }
    
    [Fact]
    public void TryGetJobs_ShouldReturnTimeLimit_WhenReceivesRelevantJobAndHasNoPendingBatches()
    {
        // Arrange
        var sut = SetupSut("test-batch", CreateOptions(10));
        
        // Act
        var (timeout, completedJobs) = sut.TryGetJobs(_connectionId, CreateJobAssign("test-batch"));

        // Assert
        Assert.Equal(_timeLimit, timeout);
        Assert.Null(completedJobs);
    }
}