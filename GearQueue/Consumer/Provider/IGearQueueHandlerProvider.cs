namespace GearQueue.Consumer.Provider;

public interface IGearQueueHandlerProvider : IDisposable
{
    IGearQueueHandler? Get<T>() where T : IGearQueueHandler;

    IGearQueueHandler? Get(Type type);
}