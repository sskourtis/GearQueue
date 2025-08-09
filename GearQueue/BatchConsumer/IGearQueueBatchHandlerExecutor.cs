using GearQueue.Consumer;

namespace GearQueue.BatchConsumer;

public interface IGearQueueBatchHandlerExecutor
{
    Task<(bool Success, JobStatus? Status)> TryExecute(Type handlerType, BatchJobContext jobContext);
}