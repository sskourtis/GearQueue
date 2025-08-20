namespace GearQueue.Consumer;

public interface IGearQueueHandlerExecutor
{
    Task<(bool Success, JobResult? Status)> TryExecute(Type handlerType, JobContext jobContext);
}