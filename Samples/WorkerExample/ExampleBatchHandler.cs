using GearQueue.Consumer;
using SampleUtils;

namespace WorkerExample;

public class ExampleBatchHandler(ILogger<ExampleBatchHandler> logger) : IGearQueueHandler
{
    private static readonly InvocationsTracker InvocationsTracker = new();
    
    public Task<JobResult> Consume(JobContext job)
    {
        logger.LogInformation("Consuming batch job {Job}", job.Batches.Count());

        var (total, perSecond) = InvocationsTracker.InvokeAndGetInvocations(job.Batches.Count());

        if (perSecond.HasValue)
        {
            logger.LogInformation("Consumed {Total} jobs - current rate {Jobs} jobs/s", 
                total, perSecond.Value);
        }

        return Task.FromResult(JobResult.Success);
    }
}