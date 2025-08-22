using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GearQueue.Serialization;

public class GearQueueJsonJobSerializer(JsonSerializerOptions options) : IGearQueueJobSerializer
{
    public GearQueueJsonJobSerializer() : this(new JsonSerializerOptions
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    })
    {
    }

    public T Deserialize<T>(ReadOnlySpan<byte> jobData)
    {
        return JsonSerializer.Deserialize<T>(jobData, options)!;
    }

    public byte[] Serialize<T>(T job)
    {
        var jsonString = JsonSerializer.Serialize(job, options);
        
        return Encoding.UTF8.GetBytes(jsonString);
    }
}