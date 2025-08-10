namespace GearQueue.Protocol.Response;

public class JobAssignAll : JobAssignUniq
{
    public required string Reducer { get; init; }

    private JobAssignAll(byte[] packetData, int dataOffset) : base(packetData, dataOffset)
    {
    }
    
    public new static JobAssignAll Create(byte[] packetData)
    {
        ReadOnlySpan<byte> span = packetData;
        
        var jobHandle = span.GetNextStringAndIterate();
        var functionName = span.GetNextStringAndIterate();
        var uniqueId = span.GetNextStringAndIterate();
        var reducer = span.GetNextStringAndIterate();

        return new JobAssignAll(packetData, packetData.Length - span.Length)
        {
            JobHandle = jobHandle,
            FunctionName = functionName,
            UniqueId = uniqueId,
            Reducer = reducer,
        };
    }
}