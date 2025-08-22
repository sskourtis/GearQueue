namespace GearQueue.Consumer;

public interface IConsumer
{
    /// <summary>
    /// Starts consuming gearman jobs. The returned task will not complete until cancellation is request 
    /// </summary>
    /// <param name="cancellationToken"></param>
    Task Start(CancellationToken cancellationToken);
}