namespace GearQueue.Serialization;

public interface IGearQueueSerializer
{
    T Deserialize<T>(ReadOnlySpan<byte> jobData);
    
    byte[] Serialize<T>(T job);
}