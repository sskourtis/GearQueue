namespace GearQueue.Options;

public sealed class GearQueueHostOptions
{
    /// <summary>
    /// Gearman job server hostname or IP 
    /// </summary>
    public required string Hostname { get; set; }
    
    /// <summary>
    /// Gearman job server port, default 4730 
    /// </summary>
    public int Port { get; set; } = 4730;
    
    /// <summary>
    /// Socket connection timeout
    /// </summary>
    public TimeSpan ConnectionTimeout { get; set; } = TimeSpan.FromSeconds(5);
    
    /// <summary>
    /// Socket receive packet timeout
    /// </summary>
    public TimeSpan ReceiveTimeout { get; set; } = TimeSpan.FromSeconds(30);
    
    /// <summary>
    /// Socket send packet timeout
    /// </summary>
    public TimeSpan SendTimeout { get; set; } = TimeSpan.FromSeconds(5);
}