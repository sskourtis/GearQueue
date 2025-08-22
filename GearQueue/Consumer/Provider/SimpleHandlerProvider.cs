namespace GearQueue.Consumer.Provider;

public class SimpleHandlerProvider : IGearQueueHandlerProvider
{
    public IHandler? Get<T>() where T : IHandler
    {
        return Activator.CreateInstance<T>();
    }

    public IHandler? Get(Type type)
    {
        return Activator.CreateInstance(type) as IHandler;
    }
    
    public void Dispose()
    {
    }
}