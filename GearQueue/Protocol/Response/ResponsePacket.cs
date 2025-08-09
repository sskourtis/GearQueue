namespace GearQueue.Protocol.Response;

internal struct ResponsePacket
{
    internal required PacketType Type { get; set; }
    internal required byte[] Data { get; set; }
}