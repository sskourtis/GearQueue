using System.Runtime.CompilerServices;
using System.Text;

namespace GearQueue.Protocol.Request;

internal static class RequestPacketUtils
{
    private const int HeaderSize = 12;
    
    internal static RequestPacket Create(PacketType type, int length = 0)
    {
        var request = new RequestPacket
        {
            FullData = new byte[HeaderSize + length]
        };

        var header = request.FullData.AsSpan();
        
        // Magic bytes
        header[0] = 0; // '\0'
        header[1] = (byte)'R';
        header[2] = (byte)'E';
        header[3] = (byte)'Q';
    
        // Type bytes (positions 4-7)
        WriteNetworkByteOrder(header.Slice(4, 4), (int)type);
        
        // Length bytes (positions 8-11)
        WriteNetworkByteOrder(header.Slice(8, 4), length);

        return request;
    }
    
    internal static ref RequestPacket WriteString(this ref RequestPacket request, 
        string value,
        int length,
        ref int offset)
    {
        request.WriteString(value, offset);
        
        offset += length;

        return ref request;
    }
    
    internal static ref RequestPacket WriteString(this ref RequestPacket request, 
        string value,
        int offset = 0)
    {
        Encoding.UTF8.GetBytes(value,
            0, 
            value.Length, 
            request.FullData, 
            HeaderSize + offset);
        
        return ref request;
    }
    
    internal static ref RequestPacket WriteBytes(this ref RequestPacket request, 
        byte[] value,
        int offset = 0)
    {
        Buffer.BlockCopy(value, 
            0, 
            request.FullData, 
            HeaderSize + offset, 
            value.Length);
        
        return ref request;
    }

    internal static ref RequestPacket WriteNullByte(this ref RequestPacket request, ref int offset)
    {
        request.FullData[HeaderSize + offset] = 0;
        
        offset++;
        
        return ref request;
    }
    
    internal static ref RequestPacket WriteBytes(this ref RequestPacket request, 
        byte[] value,
        ref int offset)
    {
        request.WriteBytes(value, offset);;
        
        offset += value.Length;
        
        return ref request;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void WriteNetworkByteOrder(Span<byte> destination, int value)
    {
        if (BitConverter.IsLittleEndian)
        {
            destination[0] = (byte)(value >> 24);
            destination[1] = (byte)(value >> 16);
            destination[2] = (byte)(value >> 8);
            destination[3] = (byte)value;
        }
        else
        {
            // On big-endian systems, can write directly
            Unsafe.WriteUnaligned(ref destination[0], value);
        }
    }
}