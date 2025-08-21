using GearQueue.Serialization;

namespace GearQueue.Producer;

public interface IGearQueueProducer<in T>
{
    /// <summary>
    /// Create a new gearman job for the given function with the given job data.
    /// </summary>
    /// <param name="job">Job</param>
    /// <param name="cancellationToken">Optional cancellation token</param>
    /// <returns></returns>
    Task<bool> Produce(T job, CancellationToken cancellationToken = default);


    /// <summary>
    /// Create a new gearman job for the given function with the given job data.
    /// </summary>
    /// <param name="job">Job</param>
    /// <param name="options">Extra submission options</param>
    /// <param name="cancellationToken">Optional cancellation token</param>
    /// <returns></returns>
    Task<bool> Produce(T job, ProducerOptions options, CancellationToken cancellationToken = default);
}

public class GearQueueProducer<T>(string functionName, 
    IGearQueueSerializer serializer, 
    IGearQueueProducer producer) : IGearQueueProducer<T>
{
    public Task<bool> Produce(T job, CancellationToken cancellationToken = default)
    {
        return producer.Produce(functionName, serializer.Serialize(job), cancellationToken);
    }

    public Task<bool> Produce(T job, ProducerOptions options, CancellationToken cancellationToken = default)
    {
        return producer.Produce(functionName, serializer.Serialize(job), options, cancellationToken);   
    }
}