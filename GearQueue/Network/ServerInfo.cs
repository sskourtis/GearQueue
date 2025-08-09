namespace GearQueue.Network;

public sealed class ServerInfo
{
    /// <summary>
    /// Gearman job server hostname or IP 
    /// </summary>
    public required string Hostname { get; init; }
    
    /// <summary>
    /// Gearman job server port, default 4730 
    /// </summary>
    public int Port { get; init; } = 4730;
}