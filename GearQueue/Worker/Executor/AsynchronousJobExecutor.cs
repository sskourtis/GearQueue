using System.Diagnostics;
using GearQueue.Metrics;
using GearQueue.Options;
using GearQueue.Worker.Pipeline;
using Microsoft.Extensions.Logging;

namespace GearQueue.Worker.Executor;

internal class AsynchronousJobExecutor(
    WorkerOptions options,
    IWorkerPipeline workerPipeline,
    ILoggerFactory loggerFactory,
    IMetricsCollector? metricsCollector = null) 
    : AbstractJobExecutor(loggerFactory)
{
    private readonly SemaphoreSlim _handlerSemaphore = new(options.MaxConcurrency, options.MaxConcurrency);
    
    internal override async Task<JobResult?> Execute(JobContext context, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        await _handlerSemaphore.WaitAsync(cancellationToken);
        metricsCollector?.HandlerWaitTime(stopwatch.Elapsed);
        
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
            await CallPipeline(workerPipeline, context, metricsCollector);

            var result = context.Result ?? JobResult.PermanentFailure;

            await NotifyCallback(context, result);
        }
        catch
        {
            await NotifyCallback(context, JobResult.PermanentFailure);
        }
        finally
        {
            taskCompletionSource.SetResult(true);
            ActiveJobs.Remove(executionId, out _);
            _handlerSemaphore.Release();
        }
    }
}