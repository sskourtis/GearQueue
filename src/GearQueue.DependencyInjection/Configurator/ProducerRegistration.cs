using GearQueue.Options;
using GearQueue.Serialization;
using Microsoft.Extensions.Configuration;

namespace GearQueue.DependencyInjection.Configurator;

internal class ProducerRegistration
{
    public required string Name { get; set; }
    public IConfigurationSection? Section { get; set; }
    
    public string? ConnectionString { get; set; }
    public Action<ProducerOptions>? ConfigureOptions { get; set; }
    public IGearQueueJobSerializer? Serializer { get; init; }
}