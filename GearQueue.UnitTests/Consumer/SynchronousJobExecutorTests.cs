using GearQueue.Consumer;
using GearQueue.Consumer.Executor;
using GearQueue.Consumer.Pipeline;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

using static GearQueue.UnitTests.Consumer.JobContextUtils;

namespace GearQueue.UnitTests.Consumer;

public class SynchronousJobExecutorTests
{
    private readonly IConsumerPipeline _pipeline = Substitute.For<IConsumerPipeline>();
    private readonly SynchronousJobExecutor _sut;
    
    public SynchronousJobExecutorTests()
    {
        _sut = new SynchronousJobExecutor(_pipeline, Substitute.For<ILoggerFactory>());
    }

    [InlineData(JobResult.Success)]
    [InlineData(JobResult.PermanentFailure)]
    [InlineData(JobResult.Failure)]
    [Theory]
    public async Task Execute_ShouldReturnCorrectResult_WhenCalledForRegularJob(JobResult returnedResult)
    {
        // Arrange
        var context = CreateJobContext();
        _pipeline.InvokeAsync(context).Returns(_ =>
        {
            context.SetResult(returnedResult);
            return Task.CompletedTask;
        });

        // Act
        var result = await _sut.Execute(context, CancellationToken.None);

        // Assert
        Assert.Equal(returnedResult, result);
    }
    
    [Fact]
    public async Task Execute_ShouldReturnPermanentFailure_WhenCalledForRegularJobAndResultIsUnset()
    {
        // Arrange
        var context = CreateJobContext();
        _pipeline.InvokeAsync(context).Returns(Task.CompletedTask);

        // Act
        var result = await _sut.Execute(context, CancellationToken.None);

        // Assert
        Assert.Equal(JobResult.PermanentFailure, result);
    }
    
    [Fact]
    public async Task Execute_ShouldReturnPermanentFailure_WhenPipelineThrows()
    {
        // Arrange
        var context = CreateJobContext();
        _pipeline.InvokeAsync(context).Throws(new Exception("handler error"));

        // Act
        var result = await _sut.Execute(context, CancellationToken.None);

        // Assert
        Assert.Equal(JobResult.PermanentFailure, result);
    }
    
    [InlineData(JobResult.Success)]
    [InlineData(JobResult.PermanentFailure)]
    [InlineData(JobResult.Failure)]
    [Theory]
    public async Task Execute_ShouldReturnCorrectResult_WhenCalledForBatchJob(JobResult returnedResult)
    {
        // Arrange
        var connectionId = Random.Shared.Next(1, 100);
        var context = CreateBatchJobContext(Random.Shared.Next(4,8), connectionId);
        _pipeline.InvokeAsync(context).Returns(_ =>
        {
            context.SetResult(returnedResult);
            return Task.CompletedTask;
        });
        
        var receivedResults = _sut.RegisterResults(connectionId);

        // Act
        var result = await _sut.Execute(context, CancellationToken.None);

        // Assert
        Assert.Null(result);
        Assert.Equal(context.Batches.Count(), receivedResults.Count);
        Assert.Equal(receivedResults.Count, receivedResults.Select(c => c.Handle).Distinct().Count());;
        foreach (var receivedResult in receivedResults)
        {
            Assert.Equal(returnedResult, receivedResult.Result);
            Assert.Contains(receivedResult.Handle, context.Batches.Select(b => b.JobHandle));
        }
    }
    

    [Fact]
    public async Task Execute_ShouldReturnPermanentFailure_WhenCalledForBatchJobAndResultIsUnset()
    {
        // Arrange
        var connectionId = Random.Shared.Next(1, 100);
        var context = CreateBatchJobContext(Random.Shared.Next(4,8), connectionId);
        _pipeline.InvokeAsync(context).Returns(Task.CompletedTask);
        
        var receivedResults = _sut.RegisterResults(connectionId);

        // Act
        var result = await _sut.Execute(context, CancellationToken.None);

        // Assert
        Assert.Null(result);
        Assert.Equal(context.Batches.Count(), receivedResults.Count);
        Assert.Equal(receivedResults.Count, receivedResults.Select(c => c.Handle).Distinct().Count());;
        foreach (var receivedResult in receivedResults)
        {
            Assert.Equal(JobResult.PermanentFailure, receivedResult.Result);
            Assert.Contains(receivedResult.Handle, context.Batches.Select(b => b.JobHandle));
        }
    }
    
    [Fact]
    public async Task Execute_ShouldReturnPermanentFailure_WhenCalledForBatchJobAndPipelineThrows()
    {
        // Arrange
        var connectionId = Random.Shared.Next(1, 100);
        var context = CreateBatchJobContext(Random.Shared.Next(4,8), connectionId);
        _pipeline.InvokeAsync(context).Throws(new Exception("handler error"));

        var receivedResults = _sut.RegisterResults(connectionId);

        // Act
        var result = await _sut.Execute(context, CancellationToken.None);

        // Assert
        Assert.Null(result);
        Assert.Equal(context.Batches.Count(), receivedResults.Count);
        Assert.Equal(receivedResults.Count, receivedResults.Select(c => c.Handle).Distinct().Count());;
        foreach (var receivedResult in receivedResults)
        {
            Assert.Equal(JobResult.PermanentFailure, receivedResult.Result);
            Assert.Contains(receivedResult.Handle, context.Batches.Select(b => b.JobHandle));
        }
    }
}