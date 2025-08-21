using GearQueue.Protocol;

namespace GearQueue.Producer;

public record ProducerOptions
{
    public string? CorrelationId { get; init; }
    public string? GroupKey { get; init; }
    
    public JobPriority Priority { get; init; } = JobPriority.Normal;
};