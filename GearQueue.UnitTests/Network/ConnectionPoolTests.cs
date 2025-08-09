using GearQueue.Network;
using GearQueue.Utils;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace GearQueue.UnitTests.Network;

public class ConnectionPoolTests
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly IConnectionFactory _connectionFactory;
    private readonly ITimeProvider _timeProvider;
    private readonly ServerInfo _serverInfo;
    private readonly ConnectionPoolOptions _options;
    private readonly DateTimeOffset _baseTime = new(2025, 1, 1, 12, 0, 0, TimeSpan.Zero);

    public ConnectionPoolTests()
    {
        _loggerFactory = Substitute.For<ILoggerFactory>();
        _loggerFactory.CreateLogger<ConnectionPool>().Returns(Substitute.For<ILogger<ConnectionPool>>());
        
        _connectionFactory = Substitute.For<IConnectionFactory>();
        _timeProvider = Substitute.For<ITimeProvider>();
        _timeProvider.Now.Returns(_baseTime);
        
        _serverInfo = new ServerInfo
        {
            Hostname = "test-host",
            Port = 8080
        };
        
        _options = new ConnectionPoolOptions
        {
            ServerInfo = _serverInfo,
            MaxConnections = 5,
            ConnectionMaxAge = TimeSpan.FromMinutes(10),
            NewConnectionTimeout = TimeSpan.FromSeconds(5),
            HealthErrorThreshold = 3,
            HealthCheckInterval = TimeSpan.FromSeconds(5)
        };
    }
    
    [Fact]
    public void Constructor_InitializesProperties()
    {
        // Arrange & Act
        var pool = CreatePool();
        
        // Assert
        Assert.True(pool.IsHealthy);
    }
    
    [Fact]
    public async Task Get_CreatesNewConnection_WhenPoolIsEmpty()
    {
        // Arrange
        var connection = CreateMockConnection(1);
        _connectionFactory.CreateConnection(_loggerFactory, _options.ConnectionTimeout, _options.SendTimeout, _options.ReceiveTimeout).Returns(connection);
        var pool = CreatePool();
        
        // Act
        var result = await pool.Get(TestContext.Current.CancellationToken);
        
        // Assert
        Assert.Same(connection, result);
        await connection.Received(1).Connect(_serverInfo.Hostname, _serverInfo.Port, Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task Get_ReusesExistingConnection_WhenAvailable()
    {
        // Arrange
        var connection = CreateMockConnection(1);
        _connectionFactory.CreateConnection(_loggerFactory, _options.ConnectionTimeout, _options.SendTimeout, _options.ReceiveTimeout).Returns(connection);
        var pool = CreatePool();
        
        // Get a connection and return it to the pool
        var firstConnection = await pool.Get(TestContext.Current.CancellationToken);
        pool.Return(firstConnection);
        
        // Act
        var result = await pool.Get(TestContext.Current.CancellationToken);
        
        // Assert
        Assert.Same(connection, result);
        await connection.Received(1).Connect(_serverInfo.Hostname, _serverInfo.Port, Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task Get_DiscardsExpiredConnections()
    {
        // Arrange
        var connection = CreateMockConnection(1);
        _connectionFactory.CreateConnection(_loggerFactory, _options.ConnectionTimeout, _options.SendTimeout, _options.ReceiveTimeout).Returns(connection);
        var pool = CreatePool();
        
        // Get a connection and return it to the pool
        var firstConnection = await pool.Get(TestContext.Current.CancellationToken);
        pool.Return(firstConnection);
        
        // Advance time past the connection max age
        _timeProvider.Now.Returns(_baseTime.Add(_options.ConnectionMaxAge).AddMinutes(1));
        
        // Setup for a new connection
        var newConnection = CreateMockConnection(2);
        _connectionFactory.CreateConnection(_loggerFactory, _options.ConnectionTimeout, _options.SendTimeout, _options.ReceiveTimeout).Returns(newConnection);
        
        // Act
        var result = await pool.Get(TestContext.Current.CancellationToken);
        
        // Assert
        Assert.Same(newConnection, result);
        connection.Received(1).Dispose();
    }
    
    [Fact]
    public async Task Get_ThrowsOperationCanceledException_WhenPoolIsFull()
    {
        // Arrange
        var pool = CreatePool();

        var id = 0;
        _connectionFactory.CreateConnection(_loggerFactory, _options.ConnectionTimeout, _options.SendTimeout, _options.ReceiveTimeout).Returns(_ => CreateMockConnection(id++));
        
        // Fill the pool to capacity
        for (var i = 0; i < _options.MaxConnections; i++)
        {
            var connection = await pool.Get(TestContext.Current.CancellationToken);
            connection.Dispose();
            // Don't return the connection to the pool
        }
        
        // Act & Assert
        var exception = await Assert.ThrowsAsync<OperationCanceledException>(() => 
            pool.Get(new CancellationTokenSource(TimeSpan.FromMilliseconds(100)).Token));
        
        Assert.NotNull(exception);
    }
    
    [Fact]
    public async Task Return_DiscardsConnection_WhenHasErrorIsTrue()
    {
        // Arrange
        var connection = CreateMockConnection(1);
        _connectionFactory.CreateConnection(_loggerFactory, _options.ConnectionTimeout, _options.SendTimeout, _options.ReceiveTimeout).Returns(connection);
        var pool = CreatePool();
        
        var obtainedConnection = await pool.Get(TestContext.Current.CancellationToken);
        
        // Act
        pool.Return(obtainedConnection, true);
        
        // Assert
        connection.Received(1).Dispose();
        
        // Verify the connection isn't reused
        var newConnection = CreateMockConnection(2);
        _connectionFactory.CreateConnection(_loggerFactory, _options.ConnectionTimeout, _options.SendTimeout, _options.ReceiveTimeout).Returns(newConnection);
        var nextConnection = await pool.Get(TestContext.Current.CancellationToken);
        Assert.Same(newConnection, nextConnection);
    }
    
    [Fact]
    public async Task Return_DiscardsConnection_WhenConnectionIsExpired()
    {
        // Arrange
        var connection = CreateMockConnection(1);
        _connectionFactory.CreateConnection(_loggerFactory, _options.ConnectionTimeout, _options.SendTimeout, _options.ReceiveTimeout).Returns(connection);
        var pool = CreatePool();
        
        var obtainedConnection = await pool.Get(TestContext.Current.CancellationToken);
        
        // Advance time past the connection max age
        _timeProvider.Now.Returns(_baseTime.Add(_options.ConnectionMaxAge).AddMinutes(1));
        
        // Act
        pool.Return(obtainedConnection);
        
        // Assert
        connection.Received(1).Dispose();
    }
    
    [Fact]
    public async Task Return_EnqueuesConnection_WhenConnectionIsValid()
    {
        // Arrange
        var connection = CreateMockConnection(1);
        _connectionFactory.CreateConnection(_loggerFactory, _options.ConnectionTimeout, _options.SendTimeout, _options.ReceiveTimeout).Returns(connection);
        var pool = CreatePool();
        
        var obtainedConnection = await pool.Get(TestContext.Current.CancellationToken);
        
        // Act
        pool.Return(obtainedConnection);
        
        // Assert
        connection.DidNotReceive().Dispose();
        
        // Verify the connection is reused
        var reusedConnection = await pool.Get(TestContext.Current.CancellationToken);
        Assert.Same(connection, reusedConnection);
    }
    
    [Fact]
    public Task Return_DoesNothing_WhenConnectionIsNotFromPool()
    {
        // Arrange
        var pool = CreatePool();
        var externalConnection = CreateMockConnection(999);
        
        // Act
        pool.Return(externalConnection);
        
        // Assert
        externalConnection.DidNotReceive().Dispose();
        return Task.CompletedTask;
    }
    
    [Fact]
    public async Task Return_ReportsConnectionError_WhenDisposalThrowsException()
    {
        // Arrange
        var connection = CreateMockConnection(1);
        connection.When(x => x.Dispose()).Do(_ => throw new Exception("Disposal failed"));
        _connectionFactory.CreateConnection(_loggerFactory, _options.ConnectionTimeout, _options.SendTimeout, _options.ReceiveTimeout).Returns(connection);
        
        var pool = CreatePool();
        var obtainedConnection = await pool.Get(TestContext.Current.CancellationToken);
        
        // Act
        pool.Return(obtainedConnection, true); // Return with error to trigger disposal
    }
    
    [Fact]
    public async Task Dispose_DisposesAllConnections()
    {
        // Arrange
        var connections = new List<IConnection>();

        var i = 0;
        _connectionFactory.CreateConnection(_loggerFactory, _options.ConnectionTimeout, _options.SendTimeout, _options.ReceiveTimeout).Returns(_ =>
        {
            var connection =  CreateMockConnection(i++);
            connections.Add(connection);
            return connection;
        });
        
        var pool = CreatePool();
        
        // Get and return some connections
        var conn1 = await pool.Get(TestContext.Current.CancellationToken);
        var conn2 = await pool.Get(TestContext.Current.CancellationToken);
        var conn3 = await pool.Get(TestContext.Current.CancellationToken);
        
        pool.Return(conn1); // Return to free pool
        // Keep conn2 reserved
        pool.Return(conn3); // Return to free pool
        
        // Act
        pool.Dispose();
        
        // Assert
        foreach (var connection in connections)
        {
            connection.Received(1).Dispose();
        }
        
        // Should throw ObjectDisposedException after disposal
        await Assert.ThrowsAsync<ObjectDisposedException>(() => pool.Get(TestContext.Current.CancellationToken));
        Assert.Throws<ObjectDisposedException>(() => pool.Return(conn2));
    }
    
    [Fact]
    public void Dispose_HandlesMultipleDisposeCalls()
    {
        // Arrange
        var pool = CreatePool();
        
        // Act
        pool.Dispose();
        pool.Dispose(); // Second call should not throw
        
        // Assert - no explicit assertion needed, test passes if no exception is thrown
    }
    
    [Fact]
    public async Task MultithreadedScenario_HandlesConcurrentOperations()
    {
        // Arrange
        var currentConnectionIndex = 0;
        _connectionFactory
            .CreateConnection(_loggerFactory, _options.ConnectionTimeout, _options.SendTimeout, _options.ReceiveTimeout)
            .Returns(_ => CreateMockConnection(currentConnectionIndex++));
        
        var pool = CreatePool();
        var random = new Random(42);
        var errors = 0;
        
        // Act - run concurrent operations
        var tasks = Enumerable.Range(0, 50)
            .Select(_ => Task.Run(async () =>
            {
                try
                {
                    // Randomly get or return connections
                    if (random.Next(2) == 0)
                    {
                        var conn = await pool.Get(new CancellationTokenSource(1000).Token);
                        // Sometimes return with error
                        if (random.Next(3) == 0)
                        {
                            pool.Return(conn, true);
                        }
                        else
                        {
                            pool.Return(conn);
                        }
                    }
                }
                catch
                {
                    Interlocked.Increment(ref errors);
                }
            }, TestContext.Current.CancellationToken));
        
        await Task.WhenAll(tasks);
        
        // Assert
        Assert.Equal(0, errors);
    }
    
    [Fact]
    public async Task Get_HonorsCancellationToken()
    {
        // Arrange
        var pool = CreatePool();
        var cts = new CancellationTokenSource();
    
        // Act & Assert
        await cts.CancelAsync();
        await Assert.ThrowsAsync<TaskCanceledException>(() => 
            pool.Get(cts.Token));
    }
    
    [Fact]
    public async Task Get_PoolBecomesUnhealthy_AfterMultipleFailures()
    {
        // Arrange
        var connection = CreateMockConnection(1);
        connection.Connect(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Throws(new Exception("Connection failed"));
        _connectionFactory.CreateConnection(_loggerFactory, _options.ConnectionTimeout, _options.SendTimeout, _options.ReceiveTimeout).Returns(connection);
    
        var pool = CreatePool();
    
        // Act - cause multiple failures to exceed health threshold
        for (var i = 0; i < _options.HealthErrorThreshold; i++)
        {
            try { await pool.Get(TestContext.Current.CancellationToken); } catch { /* expected */ }
        }
    
        // Assert
        Assert.False(pool.IsHealthy);
    }

    [Fact]
    public async Task Get_SemaphoreIsReleasedCorrectly_InAllReturnScenarios()
    {
        // Arrange
        var pool = CreatePool();
        var maxConnections = _options.MaxConnections;
        var acquiredConnections = new List<IConnection>();
    
        // Get all available connections
        for (int i = 0; i < maxConnections; i++)
        {
            var connection = CreateMockConnection(i);
            _connectionFactory.CreateConnection(_loggerFactory, _options.ConnectionTimeout, _options.SendTimeout, _options.ReceiveTimeout).Returns(connection);
            acquiredConnections.Add(await pool.Get(TestContext.Current.CancellationToken));
        }
    
        // Pool should be full now
        var getTask = pool.Get(new CancellationTokenSource(100).Token);
        await Assert.ThrowsAsync<OperationCanceledException>(() => getTask);
    
        // Act - return one connection with error
        pool.Return(acquiredConnections[0], true);
    
        // Should be able to get another connection now
        var newConnection = CreateMockConnection(100);
        _connectionFactory.CreateConnection(_loggerFactory, _options.ConnectionTimeout, _options.SendTimeout, _options.ReceiveTimeout).Returns(newConnection);
        var result = await pool.Get(TestContext.Current.CancellationToken);
    
        // Assert
        Assert.Same(newConnection, result);
        
        // Arrange again but with no error
        pool.Return(newConnection, false);
        
        // Act 
        result = await pool.Get(TestContext.Current.CancellationToken);
        Assert.Same(newConnection, result);
    }
    
    private ConnectionPool CreatePool()
    {
        return new ConnectionPool(_options, _loggerFactory, _connectionFactory, _timeProvider);
    }
    
    private IConnection CreateMockConnection(int id)
    {
        var connection = Substitute.For<IConnection>();
        connection.Id.Returns(id);
        connection.Connect(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        return connection;
    }
}