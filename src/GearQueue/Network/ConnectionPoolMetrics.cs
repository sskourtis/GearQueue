namespace GearQueue.Network;

public class ConnectionPoolMetrics
{
    private long _connectionsCreated;
    private long _connectionsReused;
    private long _connectionsReturned;
    private long _connectionsDiscarded;
    private long _expiredConnections;
    private long _connectionErrors;
    private long _cleanupRuns;
    private long _cleanedConnections;
    private long _cleanupErrors;
    
    // Access methods for reading metrics
    public long ConnectionsCreated => Interlocked.Read(ref _connectionsCreated);
    public long ConnectionsReused => Interlocked.Read(ref _connectionsReused);
    public long ConnectionsReturned => Interlocked.Read(ref _connectionsReturned);
    public long ConnectionsDiscarded => Interlocked.Read(ref _connectionsDiscarded);
    public long ExpiredConnections => Interlocked.Read(ref _expiredConnections);
    public long ConnectionErrors => Interlocked.Read(ref _connectionErrors);
    public long CleanupRuns => Interlocked.Read(ref _cleanupRuns);
    public long CleanedConnections => Interlocked.Read(ref _cleanedConnections);
    public long CleanupErrors => Interlocked.Read(ref _cleanupErrors);
    
    
    // Update methods
    internal void IncrementConnectionsCreated() => Interlocked.Increment(ref _connectionsCreated);
    internal void IncrementConnectionsReused() => Interlocked.Increment(ref _connectionsReused);
    internal void IncrementConnectionsReturned() => Interlocked.Increment(ref _connectionsReturned);
    internal void IncrementConnectionsDiscarded() => Interlocked.Increment(ref _connectionsDiscarded);
    internal void IncrementExpiredConnections() => Interlocked.Increment(ref _expiredConnections);
    internal void IncrementConnectionErrors() => Interlocked.Increment(ref _connectionErrors);
    internal void IncrementCleanupRuns() => Interlocked.Increment(ref _cleanupRuns);
    internal void AddCleanedConnections(int count) => Interlocked.Add(ref _cleanedConnections, count);
    internal void IncrementCleanupErrors() => Interlocked.Increment(ref _cleanupErrors);
    
    
    /// <summary>
    /// Resets all metrics to zero.
    /// </summary>
    public void Reset()
    {
        Interlocked.Exchange(ref _connectionsCreated, 0);
        Interlocked.Exchange(ref _connectionsReused, 0);
        Interlocked.Exchange(ref _connectionsReturned, 0);
        Interlocked.Exchange(ref _connectionsDiscarded, 0);
        Interlocked.Exchange(ref _expiredConnections, 0);
        Interlocked.Exchange(ref _connectionErrors, 0);
        Interlocked.Exchange(ref _cleanupRuns, 0);
        Interlocked.Exchange(ref _cleanedConnections, 0);
        Interlocked.Exchange(ref _cleanupErrors, 0);
    }
    
    /// <summary>
    /// Gets a snapshot of the current metrics.
    /// </summary>
    /// <returns>Dictionary containing all metric values.</returns>
    public Dictionary<string, long> GetSnapshot()
    {
        return new Dictionary<string, long>
        {
            ["ConnectionsCreated"] = ConnectionsCreated,
            ["ConnectionsReused"] = ConnectionsReused,
            ["ConnectionsReturned"] = ConnectionsReturned,
            ["ConnectionsDiscarded"] = ConnectionsDiscarded,
            ["ExpiredConnections"] = ExpiredConnections,
            ["ConnectionErrors"] = ConnectionErrors,
            ["CleanupRuns"] = CleanupRuns,
            ["CleanedConnections"] = CleanedConnections,
            ["CleanupErrors"] = CleanupErrors,
        };
    }
}
