namespace GearQueue.Consumer;

public interface IGearQueueHandler
{
    Task<JobResult> Consume(JobContext job);
}