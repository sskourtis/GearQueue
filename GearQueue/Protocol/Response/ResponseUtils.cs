namespace GearQueue.Protocol.Response;


internal static class ResponseUtils
{
    internal static string GetNextStringAndIterate(this ref ReadOnlySpan<byte> span)
    {
        if (span.IsEmpty)
        {
            return string.Empty;
        }
        
        var offset = span.IndexOf((byte)0);

        string value;
        
        if (offset == -1)
        {
            value = System.Text.Encoding.UTF8.GetString(span);
            span = ReadOnlySpan<byte>.Empty;
            return value;
        }
        
        value = System.Text.Encoding.UTF8.GetString(span[..offset]);
        span = span[(offset + 1)..];

        return value;
    }
}