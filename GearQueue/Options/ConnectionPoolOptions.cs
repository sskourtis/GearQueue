using GearQueue.Network;

namespace GearQueue.Options;

public class ConnectionPoolOptions
{
    /// <summary>
    /// The maximum time a connection is kept alive and re-used. 
    /// </summary>
    public TimeSpan ConnectionMaxAge { get; set; } = TimeSpan.FromMinutes(10);
    
    /// <summary>
    /// The maximum time for the caller to wait when requesting a connection
    /// </summary>
    public TimeSpan NewConnectionTimeout { get; set; } = TimeSpan.FromSeconds(10);
    
    /// <summary>
    /// The maximum number of connections kept alive by the pool at a time.
    /// </summary>
    public int MaxConnections { get; init; } = 50;
    
    /// <summary>
    /// Gearman job server connection info
    /// </summary>
    public required ServerInfo ServerInfo { get; set; }
    
    /// <summary>
    /// The error threshold after which the pool is marked as unhealthy (Default 5)
    /// </summary>
    public int HealthErrorThreshold { get; set; } = 5;
    
    /// <summary>
    /// The time after which an unhealthy pool will attempt to attempt new connections  
    /// </summary>
    public TimeSpan HealthCheckInterval { get; set; } = TimeSpan.FromSeconds(5);
    
    /// <summary>
    /// Socket connection timeout
    /// </summary>
    public TimeSpan ConnectionTimeout { get; set; } = TimeSpan.FromSeconds(5);
    
    /// <summary>
    /// Socket receive packet timeout
    /// </summary>
    public TimeSpan ReceiveTimeout { get; set; } = TimeSpan.FromSeconds(5);
    
    /// <summary>
    /// Socket send packet timeout
    /// </summary>
    public TimeSpan SendTimeout { get; set; } = TimeSpan.FromSeconds(3);
}