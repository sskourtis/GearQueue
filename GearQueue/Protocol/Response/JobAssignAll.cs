namespace GearQueue.Protocol.Response;

public class JobAssignAll
{
    private readonly int _dataOffset;
    private readonly byte[] _packetData;

    public required string JobHandle { get; init; }
    public required string FunctionName { get; init; }
    public required string UniqueId { get; init; }
    public required string Reducer { get; init; }
    
    public ReadOnlySpan<byte> Data => _packetData.AsSpan()[_dataOffset..];

    private JobAssignAll(byte[] packetData, int dataOffset)
    {
        _packetData = packetData;
        _dataOffset = dataOffset;
    }
    
    public static JobAssignAll Create(byte[] packetData)
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