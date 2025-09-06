using GearQueue.Options;
using GearQueue.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GearQueue.DependencyInjection.Configurator;

internal class WorkerRegistration
{
    public Dictionary<string, (HandlerOptions, ServiceLifetime)> HandlerMapping { get; set; } = new();
    public IConfigurationSection? Section { get; set; }
    public string? ConnectionString { get; set; }
    public Action<WorkerOptions>? ConfigureOptions { get; set; }
    public required WorkerPipelineBuilder PipelineBuilder { get; init; }
}