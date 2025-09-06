using GearQueue.Protocol;

namespace GearQueue.Producer;

public record JobOptions
{
    public string? CorrelationId { get; init; }
    public string? BatchKey { get; init; }
    
    public JobPriority Priority { get; init; } = JobPriority.Normal;
};