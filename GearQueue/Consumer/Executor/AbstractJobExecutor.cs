using System.Collections.Concurrent;
using System.Diagnostics;
using GearQueue.Consumer.Pipeline;
using GearQueue.Logging;
using GearQueue.Metrics;
using Microsoft.Extensions.Logging;

namespace GearQueue.Consumer.Executor;

internal abstract class AbstractJobExecutor(ILoggerFactory? loggerFactory) : IJobExecutor
{
    protected readonly ILogger<AbstractJobExecutor>? Logger = loggerFactory?.CreateLogger<SynchronousJobExecutor>();
    
    internal abstract Task<JobResult?> Execute(JobContext context, CancellationToken cancellationToken);
    
    protected readonly Dictionary<int, Func<string, JobResult, Task>> JobResultCallback = new();
    protected readonly ConcurrentDictionary<Guid, TaskCompletionSource<bool>> ActiveJobs = new();

    // needed by unit tests
    public AbstractJobExecutor() : this(null)
    {
    }
    
    public void RegisterAsyncResultCallback(int connectionId, Func<string, JobResult, Task> callback)
    {
        JobResultCallback[connectionId] = callback;
    }

    public async Task WaitAllExecutions()
    {
        await Task.WhenAll(ActiveJobs.Values.Select(t => t.Task)).ConfigureAwait(false);;
    }

    protected async Task CallPipeline(IConsumerPipeline pipeline, JobContext context, IMetricsCollector? metricsCollector)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            await pipeline.InvokeAsync(context);
            
            metricsCollector?.JobsHandled(context.FunctionName, context.Result ?? JobResult.PermanentFailure, stopwatch.Elapsed,
                context.IsBatch ? context.Batches.Length : 1);
        }
        catch (Exception e)
        {
            Logger?.LogConsumerException(e);
            
            metricsCollector?.JobsHandled(context.FunctionName, JobResult.PermanentFailure, stopwatch.Elapsed,
                context.IsBatch ? context.Batches.Length : 1);
            throw;
        }
    }

    protected async Task NotifyCallback(JobContext context, JobResult result)
    {
        if (context.IsBatch)
        {
            foreach (var batch in context.Batches)
            {
                if (JobResultCallback.TryGetValue(batch.ConnectionId!.Value, out var batchCallback))
                {
                    await batchCallback.Invoke(batch.JobHandle, result).ConfigureAwait(false);
                }
            }

            return;
        }
        
        if (JobResultCallback.TryGetValue(context.ConnectionId!.Value, out var callback))
        {
            await callback.Invoke(context.JobHandle, result).ConfigureAwait(false);
        }
    }
}