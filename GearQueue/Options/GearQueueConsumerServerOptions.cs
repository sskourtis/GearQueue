using GearQueue.Network;

namespace GearQueue.Options;

public sealed class GearQueueConsumerServerOptions
{
    /// <summary>
    /// Gearman job server connection info
    /// </summary>
    public required ServerInfo ServerInfo { get; init; }
    
    /// <summary>
    /// Defines the number of parallel jobs to be executed in parallel.
    /// Each extra consumer will create its own connection to the gearman job server
    /// </summary>
    public int Connections { get; set; } = 1;
    
    /// <summary>
    /// Timeout between reconnection attempts when the TCP connection is dead
    /// </summary>
    public TimeSpan ReconnectTimeout { get; set; } = TimeSpan.FromSeconds(3);
    
    /// <summary>
    /// When enabled, the consumer will be immediately notified by the gearman job server when new jobs
    /// are available without polling.
    ///
    /// Default: true
    /// </summary>
    public bool UsePreSleep { get; set; } = true;
    
    /// <summary>
    /// When UsePreSleep is false, this value is used for the delay between polling attempts for new jobs
    /// </summary>
    public TimeSpan PollingDelay { get; set; } = TimeSpan.FromSeconds(1);
   
    /// <summary>
    /// Socket connection timeout
    /// </summary>
    public TimeSpan ConnectionTimeout { get; set; } = TimeSpan.FromSeconds(5);
    
    /// <summary>
    /// Socket receive packet timeout
    /// </summary>
    public TimeSpan ReceiveTimeout { get; set; } = TimeSpan.FromMinutes(1);
    
    /// <summary>
    /// Socket send packet timeout
    /// </summary>
    public TimeSpan SendTimeout { get; set; } = TimeSpan.FromSeconds(3);
}