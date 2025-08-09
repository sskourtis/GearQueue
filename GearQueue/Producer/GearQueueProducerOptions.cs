using GearQueue.Network;

namespace GearQueue.Producer;

public class GearQueueProducerOptions
{
    public required ConnectionPoolOptions[] Servers { get; init; }
        
    public DistributionStrategy DistributionStrategy { get; init; } = DistributionStrategy.RoundRobin;
}