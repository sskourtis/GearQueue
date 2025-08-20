using System.Collections.Concurrent;
using GearQueue.Consumer.Provider;
using GearQueue.Options;
using GearQueue.Protocol.Response;
using Microsoft.Extensions.Logging;

namespace GearQueue.Consumer.Coordinators;

internal class AsynchronousAbstractHandlerExecutionCoordinator(
    IGearQueueHandlerProviderFactory handlerProviderFactory,
    Dictionary<string, Type> handlers,
    GearQueueConsumerOptions options,
    ILoggerFactory loggerFactory) 
    : AbstractHandlerExecutionCoordinator(
        loggerFactory,
        handlerProviderFactory, 
        handlers,
        new Dictionary<int, Func<string, JobResult, Task>>(), 
        new ConcurrentDictionary<Guid, Task>())
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

        var task = CallHandler(batchId, connectionId, job, cancellationToken);
        
        ActiveJobs!.TryAdd(batchId, task);

        return new ExecutionResult();
    }
    
    private async Task CallHandler(
        Guid executionId,
        int connectionId,
        JobAssign job, 
        CancellationToken cancellationToken)
    {
        try
        {
            var jobContext = new JobContext(job, cancellationToken);
            
            var result = await InvokeHandler(job.FunctionName, jobContext, cancellationToken);

            if (JobResultCallback!.TryGetValue(connectionId, out var callback))
            {
                await callback.Invoke(job.JobHandle, result)
                    .ConfigureAwait(false);
            }
        }
        finally
        {
            ActiveJobs!.Remove(executionId, out _);
            _handlerSemaphore.Release();
        }
    }
    
}