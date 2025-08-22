using GearQueue.Consumer;
using SampleUtils;

namespace WorkerExample;

public class ExampleBatchHandler(ILogger<ExampleBatchHandler> logger) : AbstractBatchHandler<JobContract>
{
    private static readonly InvocationsTracker InvocationsTracker = new();
    
    public override Task<JobResult> Consume(IEnumerable<JobContract> jobs, JobContext context)
    {
        var count = jobs.Count();
        
        logger.LogInformation("Consuming batch job {Job} for {Key}", count, context.BatchKey ?? "-");

        var (total, perSecond) = InvocationsTracker.InvokeAndGetInvocations(count);

        if (perSecond.HasValue)
        {
            logger.LogInformation("Consumed {Total} jobs - current rate {Jobs} jobs/s", 
                total, perSecond.Value);
        }

        return Task.FromResult(JobResult.Success);
    }
}