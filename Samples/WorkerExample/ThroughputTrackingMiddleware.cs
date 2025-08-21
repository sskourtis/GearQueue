using GearQueue.Consumer;
using GearQueue.Consumer.Pipeline;
using SampleUtils;

namespace WorkerExample;

public class ThroughputTrackingMiddleware(ILogger<ExampleHandler> logger): IGearQueueMiddleware
{
    private static readonly InvocationsTracker InvocationsTracker = new();
    
    public Task InvokeAsync(JobContext context, ConsumerDelegate? next = null)
    {
        var (total, perSecond) = InvocationsTracker.InvokeAndGetInvocations();

        if (perSecond.HasValue)
        {
            logger.LogInformation("Consumed {Total} jobs - current rate {Jobs} jobs/s", 
                total, perSecond.Value);
        }

        return next?.Invoke(context) ?? Task.CompletedTask;
    }
}