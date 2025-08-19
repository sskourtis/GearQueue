namespace GearQueue.Consumer;

internal sealed class SimpleHandlerExecutor : IGearQueueHandlerExecutor
{
    public async Task<(bool Success, JobStatus? Status)> TryExecute(Type handlerType, JobContext jobContext)
    {
        if (Activator.CreateInstance(handlerType) is not IGearQueueHandler handler)
        {
            return (false, null);
        }

        return (true, await handler.Consume(jobContext).ConfigureAwait(false));
    }
}