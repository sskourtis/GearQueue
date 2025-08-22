namespace GearQueue.Consumer.Provider;

public interface IGearQueueHandlerProvider : IDisposable
{
    IHandler? Get<T>() where T : IHandler;

    IHandler? Get(Type type);
}