namespace GearQueue.Options;

public class ProducerOptions
{
    /// <summary>
    /// Connection pool options. For each gearman job server, the library will manage a separate connection pool
    /// </summary>
    public IList<ConnectionPoolOptions> ConnectionPools { get; set; } =  new List<ConnectionPoolOptions>();
    
    /// <summary>
    /// Connection selection strategy between the available connection pools.
    /// Only applicable when there is more than one server 
    /// </summary>
    public DistributionStrategy DistributionStrategy { get; set; } = DistributionStrategy.RoundRobin;
}