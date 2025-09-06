namespace GearQueue.Worker;

public interface IHandler
{
    Task<JobResult> Handle(JobContext context);
}

public abstract class AbstractHandler<T> : IHandler where T : class
{
    public abstract Task<JobResult> Handle(T job, JobContext context);
    
    public Task<JobResult> Handle(JobContext context)
    {
        return Handle(context.ToJob<T>(), context);
    }
}

public abstract class AbstractBatchHandler<T> : IHandler where T : class
{
    public abstract Task<JobResult> Handle(IEnumerable<T> job, JobContext context);
    
    public Task<JobResult> Handle(JobContext context)
    {
        return Handle(context.Batches.Select(b => b.ToJob<T>()), context);
    }
}