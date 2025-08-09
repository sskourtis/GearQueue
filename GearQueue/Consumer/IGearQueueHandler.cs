namespace GearQueue.Consumer;

public interface IGearQueueHandler : IGearQueueBaseHandler
{
    Task<JobStatus> Consume(JobContext job);
}