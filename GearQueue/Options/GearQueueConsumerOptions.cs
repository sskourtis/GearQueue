namespace GearQueue.Options;

public sealed class GearQueueConsumerOptions
{
    /// <summary>
    /// Options for each gearman job server.
    /// </summary>
    public List<GearQueueConsumerHostsOptions> Hosts { get; set; } = new();

    /// <summary>
    /// The maximum number of jobs to be handled at a time.
    /// (Default: 1)
    /// </summary>
    public int MaxConcurrency { get; set; } = 1;

    /// <summary>
    /// Determines how <see cref="MaxConcurrency"/> limits are applied.
    /// <para>
    /// When set to <see cref="ConcurrencyStrategy.PerConnection"/>, the limit is enforced separately for each server connection.
    /// When set to <see cref="ConcurrencyStrategy.PerServer"/>, the limit is enforced across connections to a single server.
    /// When set to <see cref="ConcurrencyStrategy.AcrossServers"/>, the limit is enforced across all servers combined.
    /// </para>
    /// <para>
    /// If batching is enabled, you must use <see cref="ConcurrencyStrategy.AcrossServers"/>.
    /// This ensures batches can be filled using jobs from multiple servers rather than being restricted to a single server.
    /// </para>
    /// </summary>
    public ConcurrencyStrategy ConcurrencyStrategy { get; set; } = ConcurrencyStrategy.AcrossServers;
    
    /// <summary>
    /// Enable consuming jobs in batches
    /// </summary>
    public BatchOptions? Batch { get; set; } 

    /// <summary>
    /// This is a flag for the dependency injection extension library to create a scope when calling the handler
    /// </summary>
    public bool CreateScope { get; set; } = false;
}