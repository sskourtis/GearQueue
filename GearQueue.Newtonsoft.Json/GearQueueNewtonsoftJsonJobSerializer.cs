using System.Text;
using Newtonsoft.Json;

namespace GearQueue.Serialization;

public class GearQueueNewtonsoftJsonJobSerializer(JsonSerializerSettings settings) : IGearQueueJobSerializer
{
    public GearQueueNewtonsoftJsonJobSerializer() : this(new JsonSerializerSettings
    {
        NullValueHandling = NullValueHandling.Ignore
    })
    {
    }
    
    public T Deserialize<T>(ReadOnlySpan<byte> jobData)
    {
        return JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(jobData), settings)!;
    }

    public byte[] Serialize<T>(T job)
    {
        return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(job, settings));
    }
}