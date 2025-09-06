namespace GearQueue.Serialization;

public interface IGearQueueJobSerializer
{
    T Deserialize<T>(ReadOnlySpan<byte> jobData);
    
    byte[] Serialize<T>(T job);
}