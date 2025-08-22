namespace GearQueue.Consumer;

public interface IHandler
{
    Task<JobResult> Consume(JobContext context);
}

public abstract class AbstractHandler<T> : IHandler where T : class
{
    public abstract Task<JobResult> Consume(T job, JobContext context);
    
    public Task<JobResult> Consume(JobContext context)
    {
        return Consume(context.ToJob<T>(), context);
    }
}

public abstract class AbstractBatchHandler<T> : IHandler where T : class
{
    public abstract Task<JobResult> Consume(IEnumerable<T> job, JobContext context);
    
    public Task<JobResult> Consume(JobContext context)
    {
        return Consume(context.Batches.Select(b => b.ToJob<T>()), context);
    }
}