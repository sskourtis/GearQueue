using GearQueue.Network;

namespace GearQueue.Producer;

public class GearQueueProducerOptions
{
    /// <summary>
    /// Connection pool options. For each gearman job server, the library will manage a separate connection pool
    /// </summary>
    public required ConnectionPoolOptions[] Servers { get; init; }
    
    /// <summary>
    /// Connection selection strategy between the available connection pools.
    /// Only applicable when there is more than one server 
    /// </summary>
    public DistributionStrategy DistributionStrategy { get; init; } = DistributionStrategy.RoundRobin;
}