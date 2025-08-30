using GearQueue.Consumer;
using GearQueue.Consumer.Executor;
using GearQueue.Options;
using GearQueue.Serialization;
using GearQueue.UnitTests.Consumer.Batch;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace GearQueue.UnitTests.Consumer;

using static JobContextUtils;

public class JobManagerTests
{
    private readonly int _connectionId = Random.Shared.Next(1, 200);
    private readonly AbstractJobExecutor _jobExecutor = Substitute.For<AbstractJobExecutor>();

    private JobManager SetupSut(Dictionary<string, HandlerOptions> handlers)
    {
        return new JobManager(_jobExecutor, Substitute.For<ILoggerFactory>(), handlers);
    }
    
    private JobManager SetupSut(Dictionary<string, HandlerOptions> handlers, params IBatchJobManager[] batchManagers)
    {
        return new JobManager(_jobExecutor, Substitute.For<ILoggerFactory>(), handlers, batchManagers);
    }

    private static IBatchJobManager[] SetupBatchManagers(int count)
    {
        var managers = new List<IBatchJobManager>();
        
        for (var i = 1; i <= count; i++)
        {
            var batchJobManager = Substitute.For<IBatchJobManager>();
            batchJobManager.FunctionName.Returns($"test-function-batch-{i}");
            managers.Add(batchJobManager);
        }
        
        return managers.ToArray();
    }

    private static HandlerOptions CreateOptions(IGearQueueJobSerializer? serializer = null)
    {
        return new HandlerOptions
        {
            // For testing purpose we just need to have a type
            // it doesn't have to be a valid handler.
            Type = typeof(BatchJobManagerTests),
            Serializer = serializer,
        };
    }
    
    private static HandlerOptions CreateBatchOptions(IGearQueueJobSerializer? serializer = null)
    {
        return new HandlerOptions
        {
            // For testing purpose we just need to have a type
            // it doesn't have to be a valid handler.
            Type = typeof(BatchJobManagerTests),
            Serializer = serializer,
            Batch = new BatchOptions
            {
                Size = 10,
                TimeLimit = TimeSpan.FromSeconds(10),
            }
        };
    }
    
    [Fact]
    public async Task ArrangeExecution_ShouldReturnEmpty_WhenReceivingNullJobAndDoesntHaveBatches()
    {
        // Arrange
        var sut = SetupSut(new Dictionary<string, HandlerOptions>
        {
            ["test-function-1"] = CreateOptions(),
            ["test-function-2"] = CreateOptions(),
        });
        
        // Act
        var result = await sut.ArrangeExecution(_connectionId, null, CancellationToken.None);
        
        // Assert
        Assert.Null(result.MaximumSleepDelay);
        Assert.Null(result.ResultingStatus);
    }
    
    [Fact]
    public async Task ArrangeExecution_ShouldReturnPermanentFailure_WhenReceivingUnknownJobAndDoesntHaveBatches()
    {
        // Arrange
        var sut = SetupSut(new Dictionary<string, HandlerOptions>
        {
            ["test-function-1"] = CreateOptions(),
            ["test-function-2"] = CreateOptions(),
        });
        
        // Act
        var result = await sut.ArrangeExecution(_connectionId, CreateJobAssign("test-function-3"), CancellationToken.None);
        
        // Assert
        Assert.Null(result.MaximumSleepDelay);
        Assert.Equal(JobResult.PermanentFailure, result.ResultingStatus);
        await _jobExecutor.DidNotReceiveWithAnyArgs().Execute(Arg.Any<JobContext>(), Arg.Any<CancellationToken>());
    }
    
    [InlineData(JobResult.PermanentFailure)]
    [InlineData(JobResult.Failure)]
    [InlineData(JobResult.Success)]
    [InlineData(null)]
    [Theory]
    public async Task ArrangeExecution_ShouldReturnExecutorsResult_WhenReceivingJobAndDoesntHaveBatches(JobResult? expectedResult)
    {
        // Arrange
        var sut = SetupSut(new Dictionary<string, HandlerOptions>
        {
            ["test-function-1"] = CreateOptions(),
            ["test-function-2"] = CreateOptions(),
        });
        _jobExecutor.Execute(
            Arg.Is<JobContext>(j => !j.IsBatch && j.FunctionName == "test-function-1" && j.ConnectionId == _connectionId && j.CancellationToken == CancellationToken.None),
            Arg.Any<CancellationToken>()).Returns(expectedResult);
        
        // Act
        var result = await sut.ArrangeExecution(_connectionId, CreateJobAssign("test-function-1"), CancellationToken.None);
        
        // Assert
        Assert.Null(result.MaximumSleepDelay);
        Assert.Equal(expectedResult, result.ResultingStatus);
        await _jobExecutor.Received(1).Execute(Arg.Any<JobContext>(), Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task ArrangeExecution_ShouldReturnEmptyResultAndTimeout_WhenReceivingNullJobAndHasBatches()
    {
        // Arrange
        var batchManagers = SetupBatchManagers(3);
        
        var sut = SetupSut(new Dictionary<string, HandlerOptions>
        {
            ["test-function-1"] = CreateOptions(),
            ["test-function-2"] = CreateOptions(),
            ["test-function-batch-1"] = CreateOptions(),
            ["test-function-batch-2"] = CreateOptions(),
            ["test-function-batch-3"] = CreateOptions(),
        }, batchManagers);

        batchManagers[0].TryGetJobs(_connectionId, null).Returns((TimeSpan.FromSeconds(5), null));
        batchManagers[1].TryGetJobs(_connectionId, null).Returns((null, null));
        batchManagers[2].TryGetJobs(_connectionId, null).Returns((TimeSpan.FromSeconds(10), null));
        
        // Act
        var result = await sut.ArrangeExecution(_connectionId, null, CancellationToken.None);
        
        // Assert
        Assert.Equal(TimeSpan.FromSeconds(5), result.MaximumSleepDelay);;
        Assert.Null(result.ResultingStatus);
        await _jobExecutor.DidNotReceiveWithAnyArgs().Execute(Arg.Any<JobContext>(), Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task ArrangeExecution_ShouldReturnEmptyResultAndTimeout_WhenReceivingNullJobAndHasBatchesThatComplete()
    {
        // Arrange
        var batchManagers = SetupBatchManagers(4);
        
        var sut = SetupSut(new Dictionary<string, HandlerOptions>
        {
            ["test-function-1"] = CreateOptions(),
            ["test-function-2"] = CreateOptions(),
            ["test-function-batch-1"] = CreateOptions(),
            ["test-function-batch-2"] = CreateOptions(),
            ["test-function-batch-3"] = CreateOptions(),
            ["test-function-batch-4"] = CreateOptions(),
        }, batchManagers);

        batchManagers[0].TryGetJobs(_connectionId, null).Returns((TimeSpan.FromSeconds(5), null));
        batchManagers[1].TryGetJobs(_connectionId, null).Returns((null, [
            CreateBatchJobContext(5, _connectionId)
        ]));
        batchManagers[2].TryGetJobs(_connectionId, null).Returns((TimeSpan.FromSeconds(10), [
            CreateBatchJobContext(5, _connectionId)
        ]));
        batchManagers[3].TryGetJobs(_connectionId, null).Returns((null, null));
        
        // Act
        var result = await sut.ArrangeExecution(_connectionId, null, CancellationToken.None);
        
        // Assert
        Assert.Equal(TimeSpan.FromSeconds(5), result.MaximumSleepDelay);;
        Assert.Null(result.ResultingStatus);
        await _jobExecutor.Received(2)
            .Execute(Arg.Is<JobContext>(j => j.IsBatch), Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task ArrangeExecution_ShouldReturnEmptyResult_WhenReceivingJobIsHandledByBatchManager()
    {
        // Arrange
        var batchManagers = SetupBatchManagers(3);
        
        var sut = SetupSut(new Dictionary<string, HandlerOptions>
        {
            ["test-function-1"] = CreateOptions(),
            ["test-function-2"] = CreateOptions(),
            ["test-function-batch-1"] = CreateOptions(),
            ["test-function-batch-2"] = CreateOptions(),
            ["test-function-batch-3"] = CreateOptions(),
        }, batchManagers);

        batchManagers[0].TryGetJobs(_connectionId, null).Returns((TimeSpan.FromSeconds(5), null));
        batchManagers[1].TryGetJobs(_connectionId, null).Returns((null, null));
        batchManagers[2].TryGetJobs(_connectionId, null).Returns((TimeSpan.FromSeconds(10), null));
        
        // Act
        var result = await sut.ArrangeExecution(_connectionId, CreateJobAssign("test-function-batch-1"), CancellationToken.None);
        
        // Assert
        Assert.Null(result.MaximumSleepDelay);;
        Assert.Null(result.ResultingStatus);
        await _jobExecutor.DidNotReceiveWithAnyArgs().Execute(Arg.Any<JobContext>(), Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task ArrangeExecution_ShouldReturnJobResult_WhenReceivingJobThatIsHandledSynchronouslyNotByBatchManager()
    {
        // Arrange
        var batchManagers = SetupBatchManagers(3);
        
        var sut = SetupSut(new Dictionary<string, HandlerOptions>
        {
            ["test-function-1"] = CreateOptions(),
            ["test-function-2"] = CreateOptions(),
            ["test-function-batch-1"] = CreateOptions(),
            ["test-function-batch-2"] = CreateOptions(),
            ["test-function-batch-3"] = CreateOptions(),
        }, batchManagers);
        
        var job = CreateJobAssign("test-function-1");
        
        batchManagers[0].TryGetJobs(_connectionId, job).Returns((TimeSpan.FromSeconds(5), null));
        batchManagers[1].TryGetJobs(_connectionId, job).Returns((null, null));
        batchManagers[2].TryGetJobs(_connectionId, job).Returns((TimeSpan.FromSeconds(10), null));
        
        _jobExecutor.Execute(
            Arg.Is<JobContext>(j => !j.IsBatch && j.FunctionName == "test-function-1" && j.ConnectionId == _connectionId && j.CancellationToken == CancellationToken.None),
            Arg.Any<CancellationToken>()).Returns(JobResult.Success);
        
        // Act
        var result = await sut.ArrangeExecution(_connectionId, job, CancellationToken.None);
        
        // Assert
        Assert.Null(result.MaximumSleepDelay);
        Assert.Equal(JobResult.Success, result.ResultingStatus);
        await _jobExecutor.Received(1).Execute(Arg.Any<JobContext>(), Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task ArrangeExecution_ShouldReturnTimeout_WhenReceivingJobThatIsHandledAsynchronouslyNotByBatchManager()
    {
        // Arrange
        var batchManagers = SetupBatchManagers(3);
        
        var sut = SetupSut(new Dictionary<string, HandlerOptions>
        {
            ["test-function-1"] = CreateOptions(),
            ["test-function-2"] = CreateOptions(),
            ["test-function-batch-1"] = CreateOptions(),
            ["test-function-batch-2"] = CreateOptions(),
            ["test-function-batch-3"] = CreateOptions(),
        }, batchManagers);

        var job = CreateJobAssign("test-function-1");

        batchManagers[0].TryGetJobs(_connectionId, job).Returns((TimeSpan.FromSeconds(5), null));
        batchManagers[1].TryGetJobs(_connectionId, job).Returns((null, null));
        batchManagers[2].TryGetJobs(_connectionId, job).Returns((TimeSpan.FromSeconds(10), null));
        
        _jobExecutor.Execute(
            Arg.Is<JobContext>(j => !j.IsBatch && j.FunctionName == "test-function-1"),
            Arg.Any<CancellationToken>()).Returns((JobResult?)null);
        
        // Act
        var result = await sut.ArrangeExecution(_connectionId, job, CancellationToken.None);
        
        // Assert
        Assert.Equal(TimeSpan.FromSeconds(5), result.MaximumSleepDelay);
        Assert.Null(result.ResultingStatus);
        await _jobExecutor.Received(1).Execute(Arg.Any<JobContext>(), Arg.Any<CancellationToken>());
    }
}