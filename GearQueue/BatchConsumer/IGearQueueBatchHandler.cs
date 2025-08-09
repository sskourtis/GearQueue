using GearQueue.Consumer;

namespace GearQueue.BatchConsumer;

public interface IGearQueueBatchHandler : IGearQueueBaseHandler
{
    Task<JobStatus> Consume(BatchJobContext job);
}