using ProtoBuf;

namespace GearQueue.Serialization;

public class GearQueueProtoBufJobSerializer : IGearQueueJobSerializer
{
    public T Deserialize<T>(ReadOnlySpan<byte> jobData)
    {
        return Serializer.Deserialize<T>(jobData);
    }

    public byte[] Serialize<T>(T job)
    {
        using var memoryStream = new MemoryStream();
        
        Serializer.Serialize(memoryStream, job);
        memoryStream.Position = 0;
        
        return memoryStream.ToArray();
    }
}