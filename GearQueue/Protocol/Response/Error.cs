namespace GearQueue.Protocol.Response;

public class Error
{
    public required string ErrorCode { get; init; }
    public required string ErrorText { get; init; }
    
    public static Error Create(byte[] packetData)
    {
        ReadOnlySpan<byte> span = packetData;

        var code = span.GetNextStringAndIterate();
        var text = span.GetNextStringAndIterate();

        return new Error
        {
            ErrorCode = code,
            ErrorText = text
        };
    }
}