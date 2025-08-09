using GearQueue.Consumer;
using SampleUtils;

namespace WorkerExample;

public class SecondaryExampleHandler(ILogger<SecondaryExampleHandler> logger) : IGearQueueHandler
{
    private readonly InvocationsTracker _invocationsTracker = new();
    
    public Task<JobStatus> Consume(JobContext job)
    {
        var (total, perSecond) = _invocationsTracker.InvokeAndGetInvocations();

        if (perSecond.HasValue)
        {
            logger.LogInformation("SECONDARY {Total} jobs - current rate {Jobs} jobs/s", 
                total, perSecond.Value);
        }
            
        return Task.FromResult(JobStatus.Success);
    }
}