using System.Collections.Concurrent;
using GearQueue.Consumer.Pipeline;
using GearQueue.Options;
using GearQueue.Protocol.Response;
using Microsoft.Extensions.Logging;

namespace GearQueue.Consumer.Coordinators;

internal class AsynchronousAbstractHandlerExecutionCoordinator(
    ConsumerPipeline consumerPipeline,
    Dictionary<string, HandlerOptions> handlers,
    ConsumerOptions options,
    ILoggerFactory loggerFactory) 
    : AbstractHandlerExecutionCoordinator(
        loggerFactory,
        consumerPipeline, 
        handlers,
        new Dictionary<int, Func<string, JobResult, Task>>(), 
        new ConcurrentDictionary<Guid, TaskCompletionSource<bool>>()), IDisposable
{
    private readonly ILogger<AsynchronousAbstractHandlerExecutionCoordinator> _logger = loggerFactory.CreateLogger<AsynchronousAbstractHandlerExecutionCoordinator>();
    private readonly SemaphoreSlim _handlerSemaphore = new(options.MaxConcurrency, options.MaxConcurrency);

    internal override async Task<ExecutionResult> ArrangeExecution(int connectionId, JobAssign? job, CancellationToken cancellationToken)
    {
        if (job is null)
        {
            return new ExecutionResult();
        }
        
        await _handlerSemaphore.WaitAsync(cancellationToken);
        
        var batchId = Guid.NewGuid();

        var taskCompletionSource = new TaskCompletionSource<bool>();
        
        ActiveJobs!.TryAdd(batchId, taskCompletionSource);
        
        _ = CallHandler(batchId, connectionId, job, taskCompletionSource, cancellationToken);
        
        return new ExecutionResult();
    }
    
    private async Task CallHandler(
        Guid executionId,
        int connectionId,
        JobAssign job, 
        TaskCompletionSource<bool> taskCompletionSource,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await InvokeHandler(job, cancellationToken);

            if (JobResultCallback!.TryGetValue(connectionId, out var callback))
            {
                await callback.Invoke(job.JobHandle, result)
                    .ConfigureAwait(false);
            }
        }
        finally
        {
            taskCompletionSource.SetResult(true);
            ActiveJobs!.Remove(executionId, out _);
            _handlerSemaphore.Release();
        }
    }

    public void Dispose()
    {
        _handlerSemaphore.Dispose();
    }
}