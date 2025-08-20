namespace GearQueue.Consumer.Provider;

public class SimpleHandlerProviderFactory : IGearQueueHandlerProviderFactory
{
    private static readonly SimpleHandlerProvider Instance = new();
    
    public IGearQueueHandlerProvider Create()
    {
        return Instance;
    }
}