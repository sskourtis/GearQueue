namespace GearQueue.Consumer;

public interface IGearQueueHandlerExecutor
{
    Task<(bool Success, JobStatus? Status)> TryExecute(Type handlerType, JobContext jobContext);
}