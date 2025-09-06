namespace GearQueue.Worker.Pipeline;

public interface IGearQueueMiddleware
{
    public Task InvokeAsync(JobContext context, WorkerDelegate? next = null);
}