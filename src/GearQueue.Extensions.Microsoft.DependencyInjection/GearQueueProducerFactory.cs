using GearQueue.Producer;

namespace GearQueue.Extensions.Microsoft.DependencyInjection;

public interface IProducerFactory
{
    IProducer? Get(string name = "default");

    IProducer GetRequired(string name);
}

public class ProducerFactory(IEnumerable<INamedProducer> producerInstances) : IProducerFactory
{
    private readonly Dictionary<string, INamedProducer> _producers = producerInstances
        .ToDictionary(x => x.Name, x => x);

    public IProducer? Get(string name = "default")
    {
        return _producers.GetValueOrDefault(name);
    }

    public IProducer GetRequired(string name = "default")
    {
        return _producers.TryGetValue(name, out var producer) 
            ? producer 
            : throw new InvalidOperationException($"No service registered with name '{name}'");
    }
}