using GearQueue.Consumer.Pipeline;
using GearQueue.Logging;
using Microsoft.Extensions.Logging;

namespace GearQueue.Consumer.Executor;

internal class SynchronousJobExecutor(ConsumerPipeline consumerPipeline, ILoggerFactory loggerFactory) 
    : AbstractJobExecutor(loggerFactory)
{
    internal override async Task<JobResult?> Execute(JobContext context, CancellationToken cancellationToken)
    {
        try
        {
            await consumerPipeline.InvokeAsync(context);

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
        catch (Exception e)
        {
            foreach (var batchedJobContext in context.Batches)
            {
                await NotifyCallback(batchedJobContext, JobResult.PermanentFailure);
            }
            
            Logger.LogConsumerException(e);
            return JobResult.PermanentFailure;
        }
    }
}