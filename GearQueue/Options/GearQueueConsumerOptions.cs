namespace GearQueue.Options;

public sealed class GearQueueConsumerOptions
{
    /// <summary>
    /// Options for each gearman job server.
    /// </summary>
    public List<GearQueueConsumerServerOptions> Servers { get; init; } = new();
    
    /// <summary>
    /// The maximum number of jobs to be handled at a time.
    /// </summary>
    public int MaxConcurrency { get; set; } = 1;

    /// <summary>
    /// Defines the context where MaxConcurrency will apply
    ///
    /// If batching is enabled, it is required to have across all servers concurrency
    /// </summary>
    public ConcurrencyStrategy ConcurrencyStrategy { get; set; } = ConcurrencyStrategy.AcrossServers;

    /// <summary>
    /// Gearman function from where to consume jobs
    /// </summary>
    public string Function { get; set; } = string.Empty;
    
    /// <summary>
    /// Enable consuming jobs in batches
    /// </summary>
    public BatchOptions? Batch { get; set; } 

    /// <summary>
    /// This is a flag for the dependency injection extension library to create a scope when calling the handler
    /// </summary>
    public bool CreateScope { get; set; } = false;
}