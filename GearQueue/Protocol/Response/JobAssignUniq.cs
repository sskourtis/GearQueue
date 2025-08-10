namespace GearQueue.Protocol.Response;

public class JobAssignUniq : JobAssign
{
    public required string UniqueId { get; init; }

    protected JobAssignUniq(byte[] packetData, int dataOffset) : base(packetData, dataOffset)
    {
    }
    
    public new static JobAssignUniq Create(byte[] packetData)
    {
        ReadOnlySpan<byte> span = packetData;
        
        var jobHandle = span.GetNextStringAndIterate();
        var functionName = span.GetNextStringAndIterate();
        var uniqueId = span.GetNextStringAndIterate();

        return new JobAssignUniq(packetData, packetData.Length - span.Length)
        {
            JobHandle = jobHandle,
            FunctionName = functionName,
            UniqueId = uniqueId
        };
    }
}