using GearQueue.Consumer.Pipeline;
using GearQueue.Metrics;
using Microsoft.Extensions.Logging;

namespace GearQueue.Consumer.Executor;

internal class SynchronousJobExecutor(IConsumerPipeline consumerPipeline, ILoggerFactory loggerFactory, IMetricsCollector? metricsCollector = null) 
    : AbstractJobExecutor(loggerFactory)
{
    internal override async Task<JobResult?> Execute(JobContext context, CancellationToken cancellationToken)
    {
        try
        {
            await CallPipeline(consumerPipeline, context, metricsCollector);

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