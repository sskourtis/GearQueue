using GearQueue.Consumer;
using GearQueue.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GearQueue.Extensions.Microsoft.DependencyInjection.Configurator;

public class ConsumerRegistration
{
    public Dictionary<string, (HandlerOptions, ServiceLifetime)> HandlerMapping { get; set; } = new();
    public IConfigurationSection? Section { get; set; }
    public string? ConnectionString { get; set; }
    public Action<GearQueueConsumerOptions>? ConfigureOptions { get; set; }
    public required ConsumerPipelineBuilder PipelineBuilder { get; init; }
}