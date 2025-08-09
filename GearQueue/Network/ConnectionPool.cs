using System.Collections.Concurrent;
using GearQueue.Options;
using GearQueue.Utils;
using Microsoft.Extensions.Logging;

namespace GearQueue.Network;

internal interface IConnectionPool
{
    bool IsHealthy { get; }
    ConnectionPoolMetrics Metrics { get; }
    bool ShouldTryConnection();
    Task<IConnection> Get(CancellationToken cancellationToken = default);
    void Return(IConnection connection, bool hasError = false);
    void Dispose();
}

/// <summary>
/// Manages a pool of reusable connections to a specific host.
/// </summary>
internal class ConnectionPool : IDisposable, IConnectionPool
{
    private readonly ILogger<ConnectionPool> _logger;
    private readonly ServerInfo _serverInfo;
    private readonly ConnectionPoolOptions _options;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ConcurrentDictionary<int, ConnectionInfo> _reservedConnections = new();
    private readonly ConcurrentQueue<ConnectionInfo> _freeConnections = new();
    private readonly SemaphoreSlim _connectionsInUseSemaphore;
    private readonly ServerHealthTracker _healthTracker;
    private readonly IConnectionFactory _connectionFactory;
    private readonly ITimeProvider _timeProvider;

    private bool _disposed;

    public bool IsHealthy => _healthTracker.IsHealthy;
    public bool ShouldTryConnection() => _healthTracker.ShouldTryConnection();

    
    public ConnectionPoolMetrics Metrics { get; } = new();

    /// <summary>
    /// Initializes a new instance of the ConnectionPool class.
    /// </summary>
    /// <param name="options"></param>
    /// <param name="loggerFactory"></param>
    /// <param name="connectionFactory"></param>
    /// <param name="timeProvider"></param>
    internal ConnectionPool(ConnectionPoolOptions options, 
        ILoggerFactory loggerFactory,
        IConnectionFactory connectionFactory,
        ITimeProvider timeProvider)
    {
        _logger = loggerFactory.CreateLogger<ConnectionPool>();
        _options = options;
        _loggerFactory = loggerFactory;
        _connectionFactory = connectionFactory;
        _timeProvider = timeProvider;
        _healthTracker = new ServerHealthTracker(
            options.ServerInfo,
            options.HealthErrorThreshold, 
            options.HealthCheckInterval,
            loggerFactory);
        _serverInfo = options.ServerInfo;
        _connectionsInUseSemaphore = new SemaphoreSlim(options.MaxConnections, options.MaxConnections);
    }
    
    
    /// <summary>
    /// Gets an available connection from the pool or creates a new one if needed.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A connection to the configured host.</returns>
    /// <exception cref="Exception">Thrown when the connection pool is full and busy</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the pool has been disposed.</exception>
    /// <exception cref="OperationCanceledException">Thrown when cancellation is requested</exception>
    public async Task<IConnection> Get(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        if (!await _connectionsInUseSemaphore
                .WaitAsync(_options.NewConnectionTimeout, cancellationToken)
                .ConfigureAwait(false))
        {
            // TODO use custom exception?
            throw new Exception("Connection pool is full and busy, no free connection could be retrieved in reasonable time");
        }

        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var now = _timeProvider.Now;

            while (_freeConnections.TryDequeue(out var connectionInfo))
            {
                if ((now - connectionInfo.CreatedAt) > _options.ConnectionMaxAge)
                {
                    connectionInfo.Connection.Dispose();
                    Metrics.IncrementExpiredConnections();
                    continue;
                }

                Metrics.IncrementConnectionsReused();
                _reservedConnections.TryAdd(connectionInfo.Connection.Id, connectionInfo);
                return connectionInfo.Connection;
            }

            return await CreateNew(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            _healthTracker.ReportFailure();
            _connectionsInUseSemaphore.Release();
            throw;
        }
    }
    
    private async Task<IConnection> CreateNew(CancellationToken cancellationToken)
    {
        var newConnectionInfo = new ConnectionInfo
        {
            Connection = _connectionFactory.CreateConnection(_loggerFactory, _options.ConnectionTimeout, _options.SendTimeout, _options.ReceiveTimeout),
            CreatedAt = _timeProvider.Now
        };

        _reservedConnections.TryAdd(newConnectionInfo.Connection.Id, newConnectionInfo);

        await newConnectionInfo.Connection.Connect(_serverInfo.Hostname, _serverInfo.Port, cancellationToken).ConfigureAwait(false);
        Metrics.IncrementConnectionsCreated();

        return newConnectionInfo.Connection;
    }
    
    /// <summary>
    /// Return a connection to the pool
    /// </summary>
    /// <param name="connection"></param>
    /// <param name="hasError">Marks whether there was an error with the connection</param>
    public void Return(IConnection connection, bool hasError = false)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ThrowIfDisposed();

        if (!_reservedConnections.TryRemove(connection.Id, out var connectionInfo))
        {
            // This connection is not ours. do nothing
            return;
        }

        if (hasError)
        {
            _healthTracker.ReportFailure();
        }
        else
        {
            _healthTracker.ReportSuccess();
        }

        try
        {
            if (hasError)
            {
                connection.Dispose();
                Metrics.IncrementConnectionsDiscarded();
                return;
            }
            
            // Only return to pool if connection is still valid
            if ((_timeProvider.Now - connectionInfo.CreatedAt) <= _options.ConnectionMaxAge)
            {
                _freeConnections.Enqueue(connectionInfo);
                Metrics.IncrementConnectionsReturned();
                return;
            }

            
            // Connection is invalid or too old, dispose it
            connection.Dispose();
            Metrics.IncrementConnectionsDiscarded();
        }
        catch
        {
            // Ensure we always try to dispose the connection if there's an error
            try
            {
                connection.Dispose();
            }
            catch
            {
                /* Ignore */
            }

            Metrics.IncrementConnectionErrors();
        }
        finally
        {
            _connectionsInUseSemaphore.Release();
        }
    }

    /// <summary>
    /// Disposes all connections in the pool.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;
        
        _disposed = true;
        
        _connectionsInUseSemaphore.Dispose();
            
        while (_freeConnections.TryDequeue(out var connectionInfo))
        {
            try { connectionInfo.Connection.Dispose(); } catch { /* Ignore */ }
        }

        foreach (var reservedConnection in _reservedConnections.Values)
        {
            try { reservedConnection.Connection.Dispose(); } catch { /* Ignore */ }
        }
        
        _reservedConnections.Clear();
        
        GC.SuppressFinalize(this);
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(ConnectionPool));
        }
    }
    
    private class ConnectionInfo
    {
        public required IConnection Connection { get; init; }
        public DateTimeOffset CreatedAt { get; init; }
    }
}