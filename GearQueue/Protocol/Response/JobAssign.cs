namespace GearQueue.Protocol.Response;

public class JobAssign
{
    private readonly int _dataOffset;
    private readonly byte[] _packetData;

    public required string JobHandle { get; init; }
    public required string FunctionName { get; init; }
    
    public ReadOnlySpan<byte> Data => _packetData.AsSpan()[_dataOffset..];

    protected JobAssign(byte[] packetData, int dataOffset)
    {
        _packetData = packetData;
        _dataOffset = dataOffset;
    }
    
    public static JobAssign Create(byte[] packetData)
    {
        ReadOnlySpan<byte> span = packetData;

        var jobHandle = span.GetNextStringAndIterate();
        var functionName = span.GetNextStringAndIterate();
        
        return new JobAssign(packetData, packetData.Length - span.Length)
        {
            JobHandle = jobHandle,
            FunctionName = functionName
        };
    }
}