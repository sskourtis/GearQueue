namespace GearQueue.Consumer.Provider;

public interface IHandlerProvider : IDisposable
{
    IHandler? Get<T>() where T : IHandler;

    IHandler? Get(Type type);
}