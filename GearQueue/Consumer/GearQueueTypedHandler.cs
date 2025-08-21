using GearQueue.Serialization;

namespace GearQueue.Consumer;

public abstract class GearQueueTypedHandler<T>(IGearQueueSerializer serializer) : IGearQueueHandler
{
    public Task<JobResult> Consume(JobContext context)
    {
        var message = serializer.Deserialize<T>(context.Data);
        
        return Consume(message, context);
    }

    public abstract Task<JobResult> Consume(T job, JobContext context);
}