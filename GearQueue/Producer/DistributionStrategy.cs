namespace GearQueue.Producer;

public enum DistributionStrategy
{
    /// <summary>
    /// Alternates between servers in a round-robin fashion
    /// </summary>
    RoundRobin,
        
    /// <summary>
    /// Uses the primary server and only fails over to the secondary
    /// </summary>
    PrimaryWithFailover,
        
    /// <summary>
    /// Randomly chooses a server for each request
    /// </summary>
    Random
}