using GearQueue.BatchConsumer;
using GearQueue.Consumer;
using SampleUtils;

namespace WorkerExample;

public class ExampleBatchHandler(ILogger<ExampleBatchHandler> logger) : IGearQueueBatchHandler
{
    private static readonly InvocationsTracker InvocationsTracker = new();
    
    public Task<JobStatus> Consume(BatchJobContext job)
    {
        //logger.LogInformation("Consuming batch job {Job}", job.Jobs.Count());

        var (total, perSecond) = InvocationsTracker.InvokeAndGetInvocations(job.Jobs.Count());

        if (perSecond.HasValue)
        {
            logger.LogInformation("Consumed {Total} jobs - current rate {Jobs} jobs/s", 
                total, perSecond.Value);
        }

        return Task.FromResult(JobStatus.Success);
    }
}