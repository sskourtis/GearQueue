using GearQueue.Metrics;
using GearQueue.Worker.Pipeline;
using Microsoft.Extensions.Logging;

namespace GearQueue.Worker.Executor;

internal class SynchronousJobExecutor(IWorkerPipeline workerPipeline, ILoggerFactory loggerFactory, IMetricsCollector? metricsCollector = null) 
    : AbstractJobExecutor(loggerFactory)
{
    internal override async Task<JobResult?> Execute(JobContext context, CancellationToken cancellationToken)
    {
        try
        {
            await CallPipeline(workerPipeline, context, metricsCollector);

            var result = context.Result ?? JobResult.PermanentFailure;

            if (!context.IsBatch)
            {
                return result;
            }

            foreach (var batchedJobContext in context.Batches)
            {
                await NotifyCallback(batchedJobContext, result);
            }

            return null;
        }
        catch
        {
            if (!context.IsBatch)
            {
                return JobResult.PermanentFailure;
            }
            
            foreach (var batchedJobContext in context.Batches)
            {
                await NotifyCallback(batchedJobContext, JobResult.PermanentFailure);
            }

            return null;

        }
    }
}