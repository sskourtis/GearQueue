using GearQueue.Consumer;
using SampleUtils;

namespace WorkerExample;

public class ExampleHandler(ILogger<ExampleHandler> logger) : IGearQueueConsumerHandler
{
    private readonly InvocationsTracker _invocationsTracker = new();
    
    public Task<JobStatus> Consume(JobContext job)
    {
        var (total, perSecond) = _invocationsTracker.InvokeAndGetInvocations();

        if (perSecond.HasValue)
        {
            logger.LogInformation("Consumed {Total} jobs - current rate {Jobs} jobs/s", 
                total, perSecond.Value);
        }
            
        return Task.FromResult(JobStatus.Success);
    }
}