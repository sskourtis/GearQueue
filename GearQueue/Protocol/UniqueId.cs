namespace GearQueue.Protocol;

internal static class UniqueId
{
    internal static string Create(string correlationId, string? batchKey)
    {
        if (string.IsNullOrWhiteSpace(batchKey))
        {
            return EscapeIfContainsDelimiter(correlationId);       
        }
        
        return $"{EscapeIfContainsDelimiter(correlationId)}|{EscapeIfContainsDelimiter(batchKey)}";
    }

    internal static (string CorrelationId, string? BatchKey) Decode(string uniqueId)
    {
        // correlation id is always included, if it contains a delimiter then the second part is the batch key
        
        if (uniqueId.Contains('|'))
        {
            var split = uniqueId.Split('|');
            
            return (DeEscapeIfContainsDelimiter(split[0]), DeEscapeIfContainsDelimiter(split[1]));
        }

        return (DeEscapeIfContainsDelimiter(uniqueId), null);
    }

    private static string EscapeIfContainsDelimiter(string val)
    {
        return val.Contains('|') ? val.Replace("|", "%7C") : val;
    }
    
    private static string DeEscapeIfContainsDelimiter(string val)
    {
        return val.Contains("%7C") ? val.Replace("%7C", "|") : val;
    }
}