using GearQueue.Consumer;
using SampleUtils;

namespace WorkerExample.Handlers;

public class SecondaryExampleHandler(ILogger<SecondaryExampleHandler> logger) : IHandler
{
    private readonly InvocationsTracker _invocationsTracker = new();
    
    public Task<JobResult> Consume(JobContext context)
    {
        var (total, perSecond) = _invocationsTracker.InvokeAndGetInvocations();

        if (perSecond.HasValue)
        {
            logger.LogInformation("SECONDARY {Total} jobs - current rate {Jobs} jobs/s", 
                total, perSecond.Value);
        }
            
        return Task.FromResult(JobResult.Success);
    }
}