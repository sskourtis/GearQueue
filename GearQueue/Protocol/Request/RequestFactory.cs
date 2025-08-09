using System.Text;

using static GearQueue.Protocol.Request.RequestPacketUtils;

namespace GearQueue.Protocol.Request;

internal static class RequestFactory
{
    private const int HeaderSize = 12;

    internal static RequestPacket Echo(byte[] data)
    {
        var request = Create(PacketType.EchoReq, data.Length);
        return request.WriteBytes(data);
    }
    
    internal static RequestPacket GetStatus(string jobHandle)
    {
        var request = Create(PacketType.GetStatus, jobHandle.Length);

        return request.WriteString(jobHandle);
    }
    
    internal static RequestPacket GetStatusUnique(string uniqueId)
    {
        var request = Create(PacketType.GetStatusUnique, uniqueId.Length);

        return request.WriteString(uniqueId);
    }
    
    internal static RequestPacket CanDo(string function)
    {
        var functionByteCount = Encoding.UTF8.GetByteCount(function);
        
        var request = Create(PacketType.CanDo, functionByteCount);
        
        return request.WriteString(function);
    }
    
    internal static RequestPacket CantDo(string function)
    {
        var functionByteCount = Encoding.UTF8.GetByteCount(function);
        
        var request = Create(PacketType.CantDo, functionByteCount);
        
        return request.WriteString(function);
    }
    
    internal static RequestPacket ResetAbilities()
    {
        return Create(PacketType.ResetAbilities);
    }
    
    internal static RequestPacket PreSleep()
    {
        return Create(PacketType.PreSleep);
    }

    internal static RequestPacket GrabJob()
    {
        return Create(PacketType.GrabJob);
    }
    
    internal static RequestPacket GrabJobUniq()
    {
        return Create(PacketType.GrabJobUniq);
    }

    internal static RequestPacket GrabJobAll()
    {
        return Create(PacketType.GrabJobAll);
    }

    internal static RequestPacket WorkComplete(string jobHandle, byte[]? data = null)
    {
        var jobHandleCount = Encoding.UTF8.GetByteCount(jobHandle);
        
        var request = Create(PacketType.WorkComplete, jobHandleCount + 1 + (data?.Length ?? 0));;

        var offset = 0;
        
        request
            .WriteString(jobHandle, jobHandleCount, ref offset)
            .WriteNullByte(ref offset);

        if (data != null)
        {
            request.WriteBytes(data, ref offset);
        }

        return request;
    }
    
    internal static RequestPacket WorkFail(string jobHandle)
    {
        var jobHandleCount = Encoding.UTF8.GetByteCount(jobHandle);
        
        var request = Create(PacketType.WorkFail, jobHandleCount);;
        
        return request.WriteString(jobHandle);
    }

    internal static RequestPacket SubmitJob(string function, 
        string uniqueId,
        byte[] data, 
        JobPriority priority = JobPriority.Normal, 
        bool background = true)
    {
        var functionByteCount = Encoding.UTF8.GetByteCount(function);
        var uniqueIdByteCount = Encoding.UTF8.GetByteCount(uniqueId);
        
        var request = Create(priority switch
            {
                JobPriority.High when background => PacketType.SubmitJobHighBg,
                JobPriority.High => PacketType.SubmitJobHigh,
                JobPriority.Normal when background => PacketType.SubmitJobBg,
                JobPriority.Normal => PacketType.SubmitJob,
                JobPriority.Low when background => PacketType.SubmitJobLowBg,
                JobPriority.Low => PacketType.SubmitJobLow,
                _ => throw new ArgumentOutOfRangeException(nameof(priority), priority, null)
            }, functionByteCount + 1 + uniqueIdByteCount + 1 + data.Length);

        var offset = 0;
        
        return request
            .WriteString(function, functionByteCount, ref offset)
            .WriteNullByte(ref offset)
            .WriteString(uniqueId, uniqueIdByteCount, ref offset)
            .WriteNullByte(ref offset)
            .WriteBytes(data, ref offset);
    }
}