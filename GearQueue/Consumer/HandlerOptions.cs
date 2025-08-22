using GearQueue.Serialization;

namespace GearQueue.Consumer;

public class HandlerOptions
{
    public required Type Type { get; init; }
    
    public IGearQueueSerializer? Serializer { get; set; }
}