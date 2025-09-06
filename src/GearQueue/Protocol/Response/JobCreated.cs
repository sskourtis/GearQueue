namespace GearQueue.Protocol.Response;

public class JobCreated
{
    public required string JobHandle { get; init; }
    
    public static JobCreated Create(byte[] packetData)
    {
        ReadOnlySpan<byte> span = packetData;

        var jobHandle = span.GetNextStringAndIterate();

        return new JobCreated
        {
            JobHandle = jobHandle,
        };
    }
}