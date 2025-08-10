using GearQueue.Consumer;
using SampleUtils;

namespace WorkerExample;

public class ExampleHandler(ILogger<ExampleHandler> logger) : IGearQueueHandler
{
    private static readonly InvocationsTracker InvocationsTracker = new();
    
    public async Task<JobStatus> Consume(JobContext job)
    {
        var (total, perSecond) = InvocationsTracker.InvokeAndGetInvocations();

        if (perSecond.HasValue)
        {
            logger.LogInformation("Consumed {Total} jobs - current rate {Jobs} jobs/s", 
                total, perSecond.Value);
        }
            
        return JobStatus.Success;
    }
}