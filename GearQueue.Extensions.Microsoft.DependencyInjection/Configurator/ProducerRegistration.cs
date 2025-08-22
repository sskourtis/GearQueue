using GearQueue.Options;
using GearQueue.Serialization;
using Microsoft.Extensions.Configuration;

namespace GearQueue.Extensions.Microsoft.DependencyInjection.Configurator;

public class ProducerRegistration
{
    public required string Name { get; set; }
    public IConfigurationSection? Section { get; set; }
    
    public string? ConnectionString { get; set; }
    public Action<GearQueueProducerOptions>? ConfigureOptions { get; set; }
    public IGearQueueSerializer? Serializer { get; init; }
}