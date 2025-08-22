using GearQueue.Consumer.Pipeline;
using GearQueue.Logging;
using GearQueue.Options;
using Microsoft.Extensions.Logging;

namespace GearQueue.Consumer.Executor;

internal class AsynchronousAbstractJobExecutor(
    ConsumerOptions options,
    ConsumerPipeline consumerPipeline,
    ILoggerFactory loggerFactory) 
    : AbstractJobExecutor(loggerFactory)
{
    private readonly SemaphoreSlim _handlerSemaphore = new(options.MaxConcurrency, options.MaxConcurrency);
    
    internal override async Task<JobResult?> Execute(JobContext context, CancellationToken cancellationToken)
    {
        await _handlerSemaphore.WaitAsync(cancellationToken);
        
        var executionId = Guid.NewGuid();
        var taskCompletionSource = new TaskCompletionSource<bool>();
        ActiveJobs.TryAdd(executionId, taskCompletionSource);
        
        _ = ExecuteAsync(context, executionId, taskCompletionSource);
        
        return null;
    }

    private async Task ExecuteAsync(JobContext context,
        Guid executionId,
        TaskCompletionSource<bool> taskCompletionSource)
    {
        try
        {
            await consumerPipeline.InvokeAsync(context);

            var result = context.Result ?? JobResult.PermanentFailure;

            await NotifyCallback(context, result);
        }
        catch (Exception e)
        {
            await NotifyCallback(context, JobResult.PermanentFailure);
            
            Logger.LogConsumerException(e);
        }
        finally
        {
            taskCompletionSource.SetResult(true);
            ActiveJobs.Remove(executionId, out _);
            _handlerSemaphore.Release();
        }
    }
}