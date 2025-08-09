using GearQueue.Producer;

namespace GearQueue.Extensions.Microsoft.DependencyInjection;

public interface IGearQueueProducerFactory
{
    IGearQueueProducer? Get(string name = "default");

    IGearQueueProducer GetRequired(string name);
}

public class GearQueueProducerFactory(IEnumerable<INamedGearQueueProducer> producerInstances) : IGearQueueProducerFactory
{
    private readonly Dictionary<string, INamedGearQueueProducer> _producers = producerInstances
        .ToDictionary(x => x.Name, x => x);

    public IGearQueueProducer? Get(string name = "default")
    {
        return _producers.GetValueOrDefault(name);
    }

    public IGearQueueProducer GetRequired(string name = "default")
    {
        return _producers.TryGetValue(name, out var producer) 
            ? producer 
            : throw new InvalidOperationException($"No service registered with name '{name}'");
    }
}