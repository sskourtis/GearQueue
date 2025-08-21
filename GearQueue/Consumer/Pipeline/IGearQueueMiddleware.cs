namespace GearQueue.Consumer.Pipeline;

public interface IGearQueueMiddleware
{
    public Task InvokeAsync(JobContext context, ConsumerDelegate? next = null);
}