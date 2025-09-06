namespace GearQueue.Options;

public class ConnectionPoolOptions
{
    /// <summary>
    /// Gearman job server connection info
    /// </summary>
    public required HostOptions Host { get; set; }
    
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
    public int MaxConnections { get; set; } = 50;
    
    /// <summary>
    /// The error threshold after which the pool is marked as unhealthy (Default 5)
    /// </summary>
    public int HealthErrorThreshold { get; set; } = 5;
    
    /// <summary>
    /// The time after which an unhealthy pool will attempt to attempt new connections  
    /// </summary>
    public TimeSpan HealthCheckInterval { get; set; } = TimeSpan.FromSeconds(5);
}