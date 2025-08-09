using GearQueue.Consumer;

namespace GearQueue.BatchConsumer;

public class GearQueueBatchHandlerExecutor : IGearQueueBatchHandlerExecutor
{
    public async Task<(bool Success, JobStatus? Status)> TryExecute(Type handlerType, BatchJobContext jobContext)
    {
        if (Activator.CreateInstance(handlerType) is not IGearQueueBatchHandler handler)
        {
            return (false, null);
        }

        return (true, await handler.Consume(jobContext).ConfigureAwait(false));
    }
}