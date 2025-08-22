namespace GearQueue.Consumer.Provider;

public class SimpleHandlerProviderFactory : IHandlerProviderFactory
{
    private static readonly SimpleHandlerProvider Instance = new();
    
    public IHandlerProvider Create()
    {
        return Instance;
    }
}