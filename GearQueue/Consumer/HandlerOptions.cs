using GearQueue.Options;
using GearQueue.Serialization;

namespace GearQueue.Consumer;

public class HandlerOptions
{
    public required Type Type { get; init; }
    
    public IGearQueueJobSerializer? Serializer { get; set; }
    
    public BatchOptions? Batch { get; init; }
}