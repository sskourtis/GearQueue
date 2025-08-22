using System.Net.Sockets;
using GearQueue.Logging;
using GearQueue.Network;
using GearQueue.Options;
using GearQueue.Protocol;
using GearQueue.Protocol.Request;
using GearQueue.Serialization;
using Microsoft.Extensions.Logging;
using TimeProvider = GearQueue.Utils.TimeProvider;

namespace GearQueue.Producer;

public interface INamedGearQueueProducer : IGearQueueProducer
{
    string Name { get; init; }
}

public interface IGearQueueProducer
{
    /// <summary>
    /// Create a new gearman job for the given function with the given job data.
    /// </summary>
    /// <param name="functionName">Gearman function name</param>
    /// <param name="data">Job data</param>
    /// <param name="cancellationToken">Optional cancellation token</param>
    /// <returns></returns>
    Task<bool> Produce(string functionName, byte[] data, CancellationToken cancellationToken = default);


    /// <summary>
    /// Create a new gearman job for the given function with the given job data.
    /// </summary>
    /// <param name="functionName">Gearman function name</param>
    /// <param name="data">Job data</param>
    /// <param name="options">Extra submission options</param>
    /// <param name="cancellationToken">Optional cancellation token</param>
    /// <returns></returns>
    Task<bool> Produce(string functionName, byte[] data, ProducerOptions options, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Create a new gearman job for the given function with the given job data.
    /// </summary>
    /// <param name="functionName">Gearman function name</param>
    /// <param name="job">Job</param>
    /// <param name="cancellationToken">Optional cancellation token</param>
    /// <returns></returns>
    Task<bool> Produce<T>(string functionName, T job, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Create a new gearman job for the given function with the given job data.
    /// </summary>
    /// <param name="functionName">Gearman function name</param>
    /// <param name="job">Job</param>
    /// <param name="options">Extra submission options</param>
    /// <param name="cancellationToken">Optional cancellation token</param>
    /// <returns></returns>
    Task<bool> Produce<T>(string functionName, T job, ProducerOptions options, CancellationToken cancellationToken = default);
}

public class GearQueueProducer : IDisposable, INamedGearQueueProducer
{
    private static readonly ProducerOptions DefaultOptions = new();
    
    private bool _disposed;
    private readonly ILogger _logger;
    private readonly GearQueueProducerOptions _options;
    private readonly IConnectionPool[] _connectionPools;
    private readonly IGearQueueSerializer? _serializer;
    
    private int _distributionStrategyCounter;

    public ConnectionPoolMetrics Metrics => _connectionPools.First().Metrics;
    
    public required string Name { get; init; }

    public GearQueueProducer(GearQueueProducerOptions options, ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<GearQueueProducer>();
        _options = options;
        _connectionPools = options.ConnectionPools
            .Select(x => new ConnectionPool(x, loggerFactory, new ConnectionFactory(), new TimeProvider()))
            .ToArray<IConnectionPool>();
    }
    
    public GearQueueProducer(GearQueueProducerOptions options, IGearQueueSerializer? serializer, ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<GearQueueProducer>();
        _options = options;
        _serializer = serializer;
        _connectionPools = options.ConnectionPools
            .Select(x => new ConnectionPool(x, loggerFactory, new ConnectionFactory(), new TimeProvider()))
            .ToArray<IConnectionPool>();
    }
    
    internal GearQueueProducer(GearQueueProducerOptions options, IConnectionPoolFactory connectionPoolFactory, ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<GearQueueProducer>();
        _options = options;
        _connectionPools = options.ConnectionPools
            .Select(connectionPoolFactory.Create)
            .ToArray();
    }

    internal GearQueueProducer(GearQueueProducerOptions options, IGearQueueSerializer serializer, IConnectionPoolFactory connectionPoolFactory, ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<GearQueueProducer>();
        _options = options;
        _serializer = serializer;
        _connectionPools = options.ConnectionPools
            .Select(connectionPoolFactory.Create)
            .ToArray();
    }
    
    public async Task<bool> Produce(string functionName, byte[] data, CancellationToken cancellationToken = default)
    {
        if (_connectionPools.Length == 1)
        {
            return await Produce(0, functionName, data, DefaultOptions, cancellationToken).ConfigureAwait(false);
        }
        
        return await MultiServerProduce(functionName, data, DefaultOptions, cancellationToken).ConfigureAwait(false);
    }
    
    public async Task<bool> Produce(string functionName, byte[] data, ProducerOptions options, CancellationToken cancellationToken = default)
    {
        if (_connectionPools.Length == 1)
        {
            return await Produce(0, functionName, data, options, cancellationToken).ConfigureAwait(false);
        }
        
        return await MultiServerProduce(functionName, data, options, cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> Produce<T>(string functionName, T job, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(_serializer);

        return await Produce(functionName, _serializer.Serialize(job), cancellationToken);
    }
    
    public async Task<bool> Produce<T>(string functionName, T job, ProducerOptions options, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(_serializer);

        return await Produce(functionName, _serializer.Serialize(job), options, cancellationToken);
    }
    
    private async Task<bool> MultiServerProduce(string functionName, byte[] data, ProducerOptions options, CancellationToken cancellationToken = default)
    {
        var serverIndex = SelectServerIndex();

        /*
         * Try all servers until we find one that is healthy and that succeeds
         */
        for (var i = serverIndex; i < _connectionPools.Length + serverIndex; i++)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return false;
            }
            
            var adjustedIndex = i >= _connectionPools.Length ? i - _connectionPools.Length : i;

            if (!_connectionPools[adjustedIndex].ShouldTryConnection())
            {
                continue;
            }

            if (await Produce(adjustedIndex, functionName, data, options, cancellationToken).ConfigureAwait(false))
            {
                if (_options.DistributionStrategy == DistributionStrategy.PrimaryWithFailover)
                {
                    Interlocked.Exchange(ref _distributionStrategyCounter, adjustedIndex);
                    
                    var serverInfo = _options.ConnectionPools[adjustedIndex].Host;
                    
                    _logger.LogNewPrimaryServer(serverInfo.Hostname, serverInfo.Port);
                }

                return true;
            }
        }

        return false;
    }
    
    private int SelectServerIndex()
    {
        switch (_options.DistributionStrategy)
        {
            case DistributionStrategy.RoundRobin:
                var next = Interlocked.Increment(ref _distributionStrategyCounter);
                
                return next % _connectionPools.Length;
            case DistributionStrategy.PrimaryWithFailover:
                return Volatile.Read(ref _distributionStrategyCounter);
            case DistributionStrategy.Random:
                return Random.Shared.Next(_connectionPools.Length);
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
    
    private async Task<bool> Produce(int serverIndex, 
        string functionName, 
        byte[] data,
        ProducerOptions options,
        CancellationToken cancellationToken = default)
    {
        IConnection? connection = null;
        try
        {
            connection = await _connectionPools[serverIndex].Get(cancellationToken).ConfigureAwait(false);

            
            RequestPacket requestPacket;
            
            if (options.CorrelationId is not null || options.BatchKey is not null)
            {
                requestPacket = RequestFactory.SubmitJob(functionName,
                    UniqueId.Create(options.CorrelationId ?? Guid.NewGuid().ToString("N"), options.BatchKey), data, options.Priority);
            }
            else
            {
                requestPacket = RequestFactory.SubmitJob(functionName, Guid.NewGuid().ToString("N"), data, options.Priority);
            }
            
            await connection.SendPacket(requestPacket,
                cancellationToken)
                .ConfigureAwait(false);

            var response = await connection.GetPacket(cancellationToken, true).ConfigureAwait(false);
            
            _connectionPools[serverIndex].Return(connection);

            if (response is null || response.Value.Type != PacketType.JobCreated)
            {
                _logger.LogUnexpectedResponseType(response?.Type, PacketType.JobCreated);
                return false;
            }

            return response.Value.Type == PacketType.JobCreated;
        }
        catch (SocketException e)
        {
            var serverInfo = _options.ConnectionPools[serverIndex].Host;
            _logger.LogSocketError(serverInfo.Hostname, serverInfo.Port, e);

            if (connection is not null)
            {
                _connectionPools[serverIndex].Return(connection, true);
            }
            
            return false;
        }
        catch
        {
            if (connection is not null)
            {
                _connectionPools[serverIndex].Return(connection);
            }

            throw;
        }
    }

    public void Dispose()
    {
        lock (this)
        {
            if (_disposed)
            {
                return;
            }
            
            foreach (var connectionPool in _connectionPools)
            {
                connectionPool.Dispose();    
            }

            _disposed = true;
        }
    }
}