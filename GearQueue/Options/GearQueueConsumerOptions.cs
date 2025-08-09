namespace GearQueue.Options;

public sealed class GearQueueConsumerOptions
{
    /// <summary>
    /// Options for each gearman job server.
    /// </summary>
    public List<GearQueueConsumerServerOptions> Servers { get; init; } = new();
    
    public int MaxConcurrency { get; set; } = 1;
    
    public string Function { get; set; } = string.Empty;
    
    
    public BatchOptions? Batch { get; set; } 

    /// <summary>
    /// This is a flag for the dependency injection extension library to create a scope when calling the handler
    /// </summary>
    public bool CreateScope { get; set; } = false;
}