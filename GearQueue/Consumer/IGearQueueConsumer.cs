namespace GearQueue.Consumer;

public interface IGearQueueConsumer
{
    /// <summary>
    /// Starts consuming gearman jobs. The returned task will not complete until cancellation is request 
    /// </summary>
    /// <param name="cancellationToken"></param>
    Task StartConsuming(CancellationToken cancellationToken);
}