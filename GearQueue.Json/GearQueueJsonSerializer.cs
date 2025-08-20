using System.Text;
using System.Text.Json;
using GearQueue.Serialization;

namespace GearQueue.Json;

public class GearQueueJsonSerializer(JsonSerializerOptions options) : IGearQueueSerializer
{
    public GearQueueJsonSerializer() : this(new JsonSerializerOptions())
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