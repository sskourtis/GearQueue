using System.Text;
using GearQueue.Consumer;
using SampleUtils;

namespace WorkerExample;

public class ExampleHandler(ILogger<ExampleHandler> logger) : IGearQueueHandler
{
    private static readonly InvocationsTracker InvocationsTracker = new();
    
    public async Task<JobResult> Consume(JobContext job)
    {
        logger.LogInformation(Encoding.UTF8.GetString(job.Data));
        var (total, perSecond) = InvocationsTracker.InvokeAndGetInvocations();

        if (perSecond.HasValue)
        {
            logger.LogInformation("Consumed {Total} jobs - current rate {Jobs} jobs/s", 
                total, perSecond.Value);
        }
            
        return JobResult.Success;
    }
}