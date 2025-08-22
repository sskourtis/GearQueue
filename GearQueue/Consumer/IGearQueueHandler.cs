namespace GearQueue.Consumer;

public interface IGearQueueHandler
{
    Task<JobResult> Consume(JobContext context);
}

public abstract class GearQueueTypedHandler<T> : IGearQueueHandler
{
    public Task<JobResult> Consume(JobContext context)
    {
        return Consume((context as JobContext<T>)!);
    }

    public abstract Task<JobResult> Consume(JobContext<T> context);
}