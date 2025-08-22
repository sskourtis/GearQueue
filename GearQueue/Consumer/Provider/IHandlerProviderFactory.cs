namespace GearQueue.Consumer.Provider;

public interface IHandlerProviderFactory
{
    IHandlerProvider Create();
}