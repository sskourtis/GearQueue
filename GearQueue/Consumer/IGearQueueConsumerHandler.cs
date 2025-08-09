namespace GearQueue.Consumer;

public interface IGearQueueConsumerHandler
{
    Task<JobStatus> Consume(JobContext job);
}