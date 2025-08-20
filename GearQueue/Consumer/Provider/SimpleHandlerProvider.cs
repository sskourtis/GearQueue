namespace GearQueue.Consumer.Provider;

public class SimpleHandlerProvider : IGearQueueHandlerProvider
{
    public IGearQueueHandler? Get<T>() where T : IGearQueueHandler
    {
        return Activator.CreateInstance<T>();
    }

    public IGearQueueHandler? Get(Type type)
    {
        return Activator.CreateInstance(type) as IGearQueueHandler;
    }
    
    public void Dispose()
    {
    }
}