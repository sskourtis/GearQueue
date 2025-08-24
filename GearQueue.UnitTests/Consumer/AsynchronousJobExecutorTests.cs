using System.Diagnostics;
using GearQueue.Consumer;
using GearQueue.Consumer.Executor;
using GearQueue.Consumer.Pipeline;
using GearQueue.Options;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

using static GearQueue.UnitTests.Consumer.JobContextUtils;

namespace GearQueue.UnitTests.Consumer;

public class AsynchronousJobExecutorTests
{
    private const int MaxConcurrency = 5;
    private readonly int _connectionId = Random.Shared.Next(1, 100);
    private readonly IConsumerPipeline _pipeline = Substitute.For<IConsumerPipeline>();
    private readonly AsynchronousJobExecutor _sut;

    public AsynchronousJobExecutorTests()
    {
        _sut = new AsynchronousJobExecutor(new ConsumerOptions
            {
                MaxConcurrency = MaxConcurrency,
            },
            _pipeline,
            Substitute.For<ILoggerFactory>());
    }
    
    [InlineData(JobResult.Success)]
    [InlineData(JobResult.PermanentFailure)]
    [InlineData(JobResult.Failure)]
    [Theory]
    public async Task Execute_ShouldReturnCorrectResult_WhenCalledForRegularJob(JobResult returnedResult)
    {
        // Arrange
        var context = CreateJobContext(_connectionId);
        _pipeline.InvokeAsync(context).Returns(_ =>
        {
            context.SetResult(returnedResult);
            return Task.CompletedTask;
        });
        
        var receivedResults = _sut.RegisterResults(_connectionId);

        // Act
        var result = await _sut.Execute(context, CancellationToken.None);
        await _sut.WaitAllExecutions();

        // Assert
        Assert.Null(result);
        Assert.Equal([(context.JobHandle, returnedResult)], receivedResults);
    }
    
    [Fact]
    public async Task Execute_ShouldReturnPermanentFailure_WhenCalledForRegularJobAndResultIsUnset()
    {
        // Arrange
        var context = CreateJobContext(_connectionId);
        _pipeline.InvokeAsync(context).Returns(Task.CompletedTask);
        var receivedResults = _sut.RegisterResults(_connectionId);

        // Act
        var result = await _sut.Execute(context, CancellationToken.None);
        await _sut.WaitAllExecutions();
        
        // Assert
        Assert.Null(result);
        Assert.Equal([(context.JobHandle, JobResult.PermanentFailure)], receivedResults);
    }
    
    [Fact]
    public async Task Execute_ShouldReturnPermanentFailure_WhenPipelineThrows()
    {
        // Arrange
        var context = CreateJobContext(_connectionId);
        _pipeline.InvokeAsync(context).Throws(new Exception("handler error"));
        var receivedResults = _sut.RegisterResults(_connectionId);
        
        // Act
        var result = await _sut.Execute(context, CancellationToken.None);
        await _sut.WaitAllExecutions();
        
        // Assert
        Assert.Null(result);
        Assert.Equal([(context.JobHandle, JobResult.PermanentFailure)], receivedResults);
    }
    
    [InlineData(JobResult.Success)]
    [InlineData(JobResult.PermanentFailure)]
    [InlineData(JobResult.Failure)]
    [Theory]
    public async Task Execute_ShouldReturnCorrectResult_WhenCalledForBatchJob(JobResult returnedResult)
    {
        // Arrange
        var context = CreateBatchJobContext(Random.Shared.Next(4,8), _connectionId);
        _pipeline.InvokeAsync(context).Returns(_ =>
        {
            context.SetResult(returnedResult);
            return Task.CompletedTask;
        });
        var receivedResults = _sut.RegisterResults(_connectionId);

        // Act
        var result = await _sut.Execute(context, CancellationToken.None);
        await _sut.WaitAllExecutions();
        
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
        var context = CreateBatchJobContext(Random.Shared.Next(4,8), _connectionId);
        _pipeline.InvokeAsync(context).Returns(Task.CompletedTask);
        var receivedResults = _sut.RegisterResults(_connectionId);

        // Act
        var result = await _sut.Execute(context, CancellationToken.None);
        await _sut.WaitAllExecutions();
        
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
        var context = CreateBatchJobContext(Random.Shared.Next(4,8), _connectionId);
        _pipeline.InvokeAsync(context).Throws(new Exception("handler error"));
        var receivedResults = _sut.RegisterResults(_connectionId);

        // Act
        var result = await _sut.Execute(context, CancellationToken.None);
        await _sut.WaitAllExecutions();
        
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
    public async Task Execute_ShouldExecuteJobsConcurrently_WhenCalledMultipleTimes()
    {
        const int taskDelayMs = 50;
        var receivedResults = _sut.RegisterResults(_connectionId);
        _pipeline.InvokeAsync(Arg.Any<JobContext>()).Returns(async (info) =>
        {
            info.Arg<JobContext>().SetResult(JobResult.Success);
                
            await Task.Delay(taskDelayMs);
        });
        
        var stopwatch = Stopwatch.StartNew();
        for (var i = 0; i < MaxConcurrency; i++)
        {
            // Arrange
            var context = CreateJobContext( _connectionId);

            // Act
            var result = await _sut.Execute(context, CancellationToken.None);
            
            // Assert
            Assert.Null(result);
        }

        await _sut.WaitAllExecutions();
        var elapsed = stopwatch.ElapsedMilliseconds;

        // Assert
        Assert.InRange(elapsed, taskDelayMs - 5, taskDelayMs + 5);
        foreach (var (_, result) in receivedResults)
        {
            Assert.Equal(JobResult.Success, result);;
        }
    }
    
    [Fact]
    public async Task Execute_ShouldExecuteJobsConcurrentlyUpToLimit_WhenCalledMultipleTimes()
    {
        // Arrange
        const int taskDelayMs = 50;
        var receivedResults = _sut.RegisterResults(_connectionId);
        _pipeline.InvokeAsync(Arg.Any<JobContext>()).Returns(async (info) =>
        {
            info.Arg<JobContext>().SetResult(JobResult.Success);
                
            await Task.Delay(taskDelayMs);
        });
        
        var stopwatch = Stopwatch.StartNew();
        for (var i = 0; i < (MaxConcurrency * 2); i++)
        {
            // Arrange
            var context = CreateJobContext( _connectionId);

            // Act
            var result = await _sut.Execute(context, CancellationToken.None);
            
            // Assert #1
            Assert.Null(result);
        }
        var elapsed = stopwatch.ElapsedMilliseconds;
        
        // Assert #2 - If we get out of the loop it means we're not blocked anymore.
        Assert.InRange(elapsed, taskDelayMs - 5, taskDelayMs + 5);

        await _sut.WaitAllExecutions();
        elapsed = stopwatch.ElapsedMilliseconds;
        
        // Assert #3
        Assert.InRange(elapsed, (taskDelayMs * 2) - 5 , (taskDelayMs * 2) + 5);
        foreach (var (_, result) in receivedResults)
        {
            Assert.Equal(JobResult.Success, result);;
        }
    }
}