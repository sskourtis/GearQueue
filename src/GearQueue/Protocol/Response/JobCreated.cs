namespace GearQueue.Protocol.Response;

internal class JobCreated
{
    public required string JobHandle { get; init; }
    
    internal static JobCreated Create(byte[] packetData)
    {
        ReadOnlySpan<byte> span = packetData;

        var jobHandle = span.GetNextStringAndIterate();

        return new JobCreated
        {
            JobHandle = jobHandle,
        };
    }
}