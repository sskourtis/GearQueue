namespace GearQueue.Consumer;

public sealed class GearQueueHandlerExecutor : IGearQueueHandlerExecutor
{
    public async Task<(bool Success, JobStatus? Status)> TryExecute(Type handlerType, JobContext jobContext)
    {
        if (Activator.CreateInstance(handlerType) is not IGearQueueConsumerHandler handler)
        {
            return (false, null);
        }

        return (true, await handler.Consume(jobContext).ConfigureAwait(false));
    }
}