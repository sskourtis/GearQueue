namespace GearQueue.Consumer;

public sealed class GearQueueConsumerOptions
{
    /// <summary>
    /// Options for each gearman job server.
    /// </summary>
    public required GearQueueConsumerServerOptions[] Servers { get; init; }

    /// <summary>
    /// This is a flag for the dependency injection extension library to create a scope when calling the handler
    /// </summary>
    public bool CreateScope { get; set; } = false;
}