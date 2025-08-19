namespace GearQueue.Consumer;

public interface IGearQueueHandler
{
    Task<JobStatus> Consume(JobContext job);
}