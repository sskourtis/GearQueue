using GearQueue.Logging;
using GearQueue.Metrics;
using GearQueue.Protocol.Request;
using GearQueue.Protocol.Response;
using GearQueue.Worker.Executor;
using Microsoft.Extensions.Logging;

namespace GearQueue.Worker;

internal interface IJobManager
{
    Task<ExecutionResult> ArrangeExecution(int connectionId,
        JobAssign? job,
        CancellationToken cancellationToken);
    
    IJobExecutor Executor { get; }
    
    ref RequestPacket GrabJobPacket { get; }
}

internal class JobManager(
    AbstractJobExecutor executor,
    ILoggerFactory loggerFactory,
    Dictionary<string, HandlerOptions> handlers,
    IMetricsCollector? metricsCollector = null) : IJobManager
{
    private readonly IBatchJobManager[] _batchJobManagers = handlers
        .Where(h => h.Value.Batch is not null)
        .Select(c => new BatchJobManager(c.Key, c.Value, metricsCollector))
        .ToArray<IBatchJobManager>();

    // This constructor exists only for unit testing purposes
    public JobManager(AbstractJobExecutor executor, ILoggerFactory logger, Dictionary<string, HandlerOptions> handlers, IBatchJobManager[] batchJobManagers)
        : this(executor, logger, handlers)
    {
        _batchJobManagers = batchJobManagers;
    }
    
    private readonly ILogger _logger = loggerFactory.CreateLogger<JobManager>();

    public IJobExecutor Executor => executor;
    
    
    private RequestPacket _grabJobPacket = handlers.Any(o => o.Value.Batch?.ByKey == true)
        ? RequestFactory.GrabJobUniq()
        : RequestFactory.GrabJob();
    
    public ref RequestPacket GrabJobPacket => ref _grabJobPacket;
    
    public async Task<ExecutionResult> ArrangeExecution(int connectionId,
        JobAssign? job,
        CancellationToken cancellationToken)
    {
        TimeSpan? nextTimeout = null;
        
        if (_batchJobManagers.Length > 0)
        {
            (var currentJobHandledInBatch, nextTimeout) = await ArrangeBatchManagers(connectionId, job, cancellationToken);

            if (currentJobHandledInBatch)
            {
                return new ExecutionResult();
            }
        }
        
        if (job is null)
        {
            return nextTimeout ?? new ExecutionResult();
        }
        
        if (!handlers.TryGetValue(job.FunctionName, out var handlerOptions))
        {
            _logger.LogMissingHandlerType(job.FunctionName);

            return JobResult.PermanentFailure;
        }

        var jobContext = new JobContext(job, handlerOptions.Serializer, connectionId, cancellationToken)
        {
            HandlerType = handlerOptions.Type
        };
        
        var result = await executor.Execute(jobContext, cancellationToken);
        
        return result ?? nextTimeout ?? new ExecutionResult();
    }

    private async Task<(bool CurrentJobHandled, TimeSpan? NextTimeout)> ArrangeBatchManagers(int connectionId, JobAssign? job, CancellationToken cancellationToken)
    {
        TimeSpan? nextTimeout = null;
        var currentJobIsHandled = false;
            
        foreach (var batchJobManager in _batchJobManagers)
        {
            var (batchNextTimeout, readToRunBatchJobs) = batchJobManager.TryGetJobs(connectionId, job);

            if (batchNextTimeout.HasValue)
            {
                nextTimeout = nextTimeout < batchNextTimeout ? nextTimeout : batchNextTimeout;    
            }

            currentJobIsHandled = currentJobIsHandled || batchJobManager.FunctionName == job?.FunctionName;

            if (readToRunBatchJobs is null)
            {
                continue;
            }                
                
            foreach (var context in readToRunBatchJobs)
            {
                await executor.Execute(context, cancellationToken);
            }
        }

        return (currentJobIsHandled, nextTimeout);
    }
}