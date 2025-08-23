using System.Net.Sockets;
using GearQueue.Network;
using GearQueue.Options;
using GearQueue.Producer;
using GearQueue.Protocol;
using GearQueue.Protocol.Request;
using GearQueue.Protocol.Response;
using GearQueue.Serialization;
using GearQueue.UnitTests.Utils;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace GearQueue.UnitTests.Producer;

public class ProducerTests
{
    private readonly string _functionName = "test-" + RandomData.GetString(10);
    private readonly byte[] _data = RandomData.GetRandomBytes(10);
    private readonly string _cid = Guid.NewGuid().ToString("N");
    
    private readonly IGearQueueJobSerializer _serializer = Substitute.For<IGearQueueJobSerializer>();
    private readonly IConnectionPoolFactory _connectionPoolFactory = Substitute.For<IConnectionPoolFactory>();

    private GearQueue.Producer.Producer _sut;

    private (IConnectionPool First, IConnectionPool Second) SetupSut(ProducerOptions options)
    {
        var firstPool = Substitute.For<IConnectionPool>();
        var secondPool = Substitute.For<IConnectionPool>();

        _connectionPoolFactory.Create(Arg.Any<ConnectionPoolOptions>())
            .Returns(firstPool, secondPool);

        _sut = new GearQueue.Producer.Producer(options, _serializer, _connectionPoolFactory,
            Substitute.For<ILoggerFactory>())
        {
            Name = "default"
        };

        return (firstPool, secondPool);
    }
    
    private IConnection SetupPool(IConnectionPool pool,
        PacketType? responsePacketType,
        bool isHealthy = true,
        bool isSendPacketSuccessful = true)
    {
        pool.ShouldTryConnection().Returns(isHealthy);
        
        var connection = Substitute.For<IConnection>();

        pool.Get(Arg.Any<CancellationToken>())
            .Returns(connection);

        if (isSendPacketSuccessful)
        {
            connection.SendPacket(Arg.Any<RequestPacket>(), Arg.Any<CancellationToken>())
                .Returns(Task.CompletedTask);    
        }
        else
        {
            connection.SendPacket(Arg.Any<RequestPacket>(), Arg.Any<CancellationToken>())
                .Throws(new SocketException());
        }

        if (responsePacketType.HasValue)
        {
            connection.GetPacket(Arg.Any<CancellationToken>(), Arg.Any<bool>())
                .Returns(new ResponsePacket
                {
                    Type = responsePacketType.Value,
                    Data = []
                });
        }
        else
        {
            connection.GetPacket(Arg.Any<CancellationToken>(), Arg.Any<bool>())
                .Throws(new SocketException());   
        }

        return connection;
    }

    [InlineData(null, null)]
    [InlineData(JobPriority.High, null)]
    [InlineData(JobPriority.Normal, null)]
    [InlineData(JobPriority.Low, null)]
    [InlineData(JobPriority.High, "123456")]
    [InlineData(null, "654321")]
    [Theory]
    public async Task Produce_ShouldSucceed_WithSingleHealthyPool(JobPriority? priority, string? batchKey)
    {
        // Arrange
        var (pool, _) = SetupSut(new ProducerOptions
        {
            DistributionStrategy = DistributionStrategy.RoundRobin,
            ConnectionPools = [new ConnectionPoolOptions { Host = new HostOptions { Hostname = "localhost", }, }]
        });

        var connection = SetupPool(pool, PacketType.JobCreated);

        var requestPacket = RequestFactory.SubmitJob(_functionName,
            UniqueId.Create(_cid, batchKey), _data, priority ?? JobPriority.Normal);

        var jobOptions = priority.HasValue
            ? new JobOptions
            {
                CorrelationId = _cid,
                Priority = priority.Value,
                BatchKey = batchKey,
            }
            : new JobOptions
            {
                CorrelationId = _cid,
                BatchKey = batchKey,
            };

        // Act
        var result = await _sut.Produce(_functionName, _data, jobOptions, CancellationToken.None);

        // Assert
        Assert.True(result);

        await connection
            .Received(1)
            .SendPacket(Arg.Is<RequestPacket>(r => r.FullData.IsEquivalent(requestPacket.FullData)),
                CancellationToken.None);

        pool.Received(1).Return(connection, false);
    }
    
    [InlineData(null, null)]
    [InlineData(JobPriority.High, null)]
    [InlineData(JobPriority.Normal, null)]
    [InlineData(JobPriority.Low, null)]
    [InlineData(JobPriority.High, "123456")]
    [InlineData(null, "654321")]
    [Theory]
    public async Task Produce_ShouldSucceed_WithSingleHealthyPoolWhenSendingTypedPayload(JobPriority? priority, string? batchKey)
    {
        // Arrange
        var (pool, _) = SetupSut(new ProducerOptions
        {
            DistributionStrategy = DistributionStrategy.RoundRobin,
            ConnectionPools = [new ConnectionPoolOptions { Host = new HostOptions { Hostname = "localhost", }, }]
        });

        var connection = SetupPool(pool, PacketType.JobCreated);

        var testObject = new
        {
            Test = "Test"
        };
        
        var serializedData = RandomData.GetRandomBytes(10);
        _serializer.Serialize(testObject).Returns(serializedData);
        
        var requestPacket = RequestFactory.SubmitJob(_functionName,
            UniqueId.Create(_cid, batchKey), serializedData, priority ?? JobPriority.Normal);

        var jobOptions = priority.HasValue
            ? new JobOptions
            {
                CorrelationId = _cid,
                Priority = priority.Value,
                BatchKey = batchKey,
            }
            : new JobOptions
            {
                CorrelationId = _cid,
                BatchKey = batchKey,
            };
        
        // Act
        var result = await _sut.Produce(_functionName, testObject, jobOptions, CancellationToken.None);

        // Assert
        Assert.True(result);

        await connection
            .Received(1)
            .SendPacket(Arg.Is<RequestPacket>(r => r.FullData.IsEquivalent(requestPacket.FullData)),
                CancellationToken.None);

        pool.Received(1).Return(connection, false);
    }
    
    [Fact]
    public async Task Produce_ShouldFail_WhenSendPacketThrows()
    {
        // Arrange
        var (pool, _) = SetupSut(new ProducerOptions
        {
            DistributionStrategy = DistributionStrategy.RoundRobin,
            ConnectionPools = [new ConnectionPoolOptions { Host = new HostOptions { Hostname = "localhost", }, }]
        });

        var connection = SetupPool(pool, PacketType.JobCreated, true, false);

        var requestPacket = RequestFactory.SubmitJob(_functionName,
            UniqueId.Create(_cid, null), 
            _data);

        var jobOptions = new JobOptions
        {
            CorrelationId = _cid,
        };

        // Act
        var result = await _sut.Produce(_functionName, _data, jobOptions, CancellationToken.None);

        // Assert
        Assert.False(result);

        await connection
            .Received(1)
            .SendPacket(Arg.Is<RequestPacket>(r => r.FullData.IsEquivalent(requestPacket.FullData)),
                CancellationToken.None);

        pool.Received(1).Return(connection, true);
    }
    
    [Fact]
    public async Task Produce_ShouldFail_WhenGetPacketThrows()
    {
        // Arrange
        var (pool, _) = SetupSut(new ProducerOptions
        {
            DistributionStrategy = DistributionStrategy.RoundRobin,
            ConnectionPools = [new ConnectionPoolOptions { Host = new HostOptions { Hostname = "localhost", }, }]
        });

        var connection = SetupPool(pool, null, true, true);

        var requestPacket = RequestFactory.SubmitJob(_functionName,
            UniqueId.Create(_cid, null), 
            _data);

        var jobOptions = new JobOptions
        {
            CorrelationId = _cid,
        };

        // Act
        var result = await _sut.Produce(_functionName, _data, jobOptions, CancellationToken.None);

        // Assert
        Assert.False(result);

        await connection
            .Received(1)
            .SendPacket(Arg.Is<RequestPacket>(r => r.FullData.IsEquivalent(requestPacket.FullData)),
                CancellationToken.None);

        pool.Received(1).Return(connection, true);
    }
    
    [Fact]
    public async Task Produce_ShouldFail_WhenGetPacketReturnsInvalidType()
    {
        // Arrange
        var (pool, _) = SetupSut(new ProducerOptions
        {
            DistributionStrategy = DistributionStrategy.RoundRobin,
            ConnectionPools = [new ConnectionPoolOptions { Host = new HostOptions { Hostname = "localhost", }, }]
        });

        var connection = SetupPool(pool, PacketType.Error);

        var requestPacket = RequestFactory.SubmitJob(_functionName,
            UniqueId.Create(_cid, null), 
            _data);

        var jobOptions = new JobOptions
        {
            CorrelationId = _cid,
        };

        // Act
        var result = await _sut.Produce(_functionName, _data, jobOptions, CancellationToken.None);

        // Assert
        Assert.False(result);

        await connection
            .Received(1)
            .SendPacket(Arg.Is<RequestPacket>(r => r.FullData.IsEquivalent(requestPacket.FullData)),
                CancellationToken.None);

        pool.Received(1).Return(connection, false);
    }

    [Fact]
    public async Task Produce_ShouldSucceed_WithTwoHealthyPoolsRoundRobin()
    {
        // Arrange
        var (firstPool, secondPool) = SetupSut(new ProducerOptions
        {
            DistributionStrategy = DistributionStrategy.RoundRobin,
            ConnectionPools =
            [
                new ConnectionPoolOptions { Host = new HostOptions { Hostname = "localhost", }, },
                new ConnectionPoolOptions { Host = new HostOptions { Hostname = "localhost", }, }
            ]
        });

        var firstConnection = SetupPool(firstPool, PacketType.JobCreated);
        var secondConnection = SetupPool(secondPool, PacketType.JobCreated);

        var requestPacket = RequestFactory.SubmitJob(_functionName,
            UniqueId.Create(_cid, null), _data);

        var jobOptions = new JobOptions
        {
            CorrelationId = _cid,
        };

        // Act
        var firstResult = await _sut.Produce(_functionName, _data, jobOptions, CancellationToken.None);
        var secondResult = await _sut.Produce(_functionName, _data, jobOptions, CancellationToken.None);

        // Assert
        Assert.True(firstResult);
        Assert.True(secondResult);

        await firstConnection
            .Received(1)
            .SendPacket(Arg.Is<RequestPacket>(r => r.FullData.IsEquivalent(requestPacket.FullData)),
                CancellationToken.None);

        await secondConnection
            .Received(1)
            .SendPacket(Arg.Is<RequestPacket>(r => r.FullData.IsEquivalent(requestPacket.FullData)),
                CancellationToken.None);

        firstPool.Received(1).Return(firstConnection, false);
        secondPool.Received(1).Return(secondConnection, false);
    }

    [Fact]
    public async Task Produce_ShouldSucceed_WithTwoHealthyPoolsPrimaryFailover()
    {
        // Arrange
        var (firstPool, secondPool) = SetupSut(new ProducerOptions
        {
            DistributionStrategy = DistributionStrategy.PrimaryWithFailover,
            ConnectionPools =
            [
                new ConnectionPoolOptions { Host = new HostOptions { Hostname = "localhost", }, },
                new ConnectionPoolOptions { Host = new HostOptions { Hostname = "localhost", }, }
            ]
        });
        
        var firstConnection = SetupPool(firstPool, PacketType.JobCreated);
        _ = SetupPool(secondPool, PacketType.JobCreated);
        
        var requestPacket = RequestFactory.SubmitJob(_functionName,
            UniqueId.Create(_cid, null), _data);

        var jobOptions = new JobOptions
        {
            CorrelationId = _cid,
        };

        // Act
        var firstResult = await _sut.Produce(_functionName, _data, jobOptions, CancellationToken.None);
        var secondResult = await _sut.Produce(_functionName, _data, jobOptions, CancellationToken.None);

        // Assert
        Assert.True(firstResult);
        Assert.True(secondResult);

        await firstConnection
            .Received(2)
            .SendPacket(Arg.Is<RequestPacket>(r => r.FullData.IsEquivalent(requestPacket.FullData)),
                CancellationToken.None);

        firstPool.Received(2).Return(firstConnection, false);
        secondPool.Received(0).Return(Arg.Any<IConnection>(), false);
    }

    [Fact]
    public async Task Produce_ShouldSucceed_WithTwoHealthyPoolsFailoverToSecondary()
    {
        // Arrange
        var (firstPool, secondPool) = SetupSut(new ProducerOptions
        {
            DistributionStrategy = DistributionStrategy.PrimaryWithFailover,
            ConnectionPools =
            [
                new ConnectionPoolOptions { Host = new HostOptions { Hostname = "localhost", }, },
                new ConnectionPoolOptions { Host = new HostOptions { Hostname = "localhost", }, }
            ]
        });

        _ = SetupPool(firstPool, PacketType.JobCreated, false);
        var connection = SetupPool(secondPool, PacketType.JobCreated);

        var requestPacket = RequestFactory.SubmitJob(_functionName,
            UniqueId.Create(_cid, null), _data);

        var jobOptions = new JobOptions
        {
            CorrelationId = _cid,
        };

        // Act
        var firstResult = await _sut.Produce(_functionName, _data, jobOptions, CancellationToken.None);
        var secondResult = await _sut.Produce(_functionName, _data, jobOptions, CancellationToken.None);

        // Assert
        Assert.True(firstResult);
        Assert.True(secondResult);

        await connection
            .Received(2)
            .SendPacket(Arg.Is<RequestPacket>(r => r.FullData.IsEquivalent(requestPacket.FullData)),
                CancellationToken.None);

        firstPool.Received(0).Return(Arg.Any<IConnection>(), false);
        secondPool.Received(2).Return(connection, false);
    }

    [Fact]
    public async Task Produce_ShouldSucceedUsingHealthyPool_WithOnePoolHealthyAndSecondUnhealthy()
    {
        // Arrange
        var (firstPool, secondPool) = SetupSut(new ProducerOptions
        {
            DistributionStrategy = DistributionStrategy.RoundRobin,
            ConnectionPools =
            [
                new ConnectionPoolOptions { Host = new HostOptions { Hostname = "localhost", }, },
                new ConnectionPoolOptions { Host = new HostOptions { Hostname = "localhost", }, }
            ]
        });

        var firstConnection = SetupPool(firstPool, PacketType.JobCreated, false);
        var secondConnection = SetupPool(secondPool, PacketType.JobCreated);

        var requestPacket = RequestFactory.SubmitJob(_functionName,
            UniqueId.Create(_cid, null), _data);

        var jobOptions = new JobOptions
        {
            CorrelationId = _cid,
        };

        // Act
        var firstResult = await _sut.Produce(_functionName, _data, jobOptions, CancellationToken.None);
        var secondResult = await _sut.Produce(_functionName, _data, jobOptions, CancellationToken.None);

        // Assert
        Assert.True(firstResult);
        Assert.True(secondResult);

        await firstConnection
            .Received(0)
            .SendPacket(Arg.Any<RequestPacket>(), CancellationToken.None);

        await secondConnection
            .Received(2)
            .SendPacket(Arg.Is<RequestPacket>(r => r.FullData.IsEquivalent(requestPacket.FullData)),
                CancellationToken.None);

        firstPool.Received(0).Return(Arg.Any<IConnection>(), false);
        secondPool.Received(2).Return(secondConnection, false);
    }

    [Fact]
    public async Task Produce_ShouldTryNextPool_WhenFirstPoolReturnsError()
    {
        // Arrange
        var (secondPool, firstPool) = SetupSut(new ProducerOptions
        {
            DistributionStrategy = DistributionStrategy.RoundRobin,
            ConnectionPools =
            [
                new ConnectionPoolOptions { Host = new HostOptions { Hostname = "localhost", }, },
                new ConnectionPoolOptions { Host = new HostOptions { Hostname = "localhost", }, }
            ]
        });

        var firstConnection = SetupPool(firstPool, null);
        var secondConnection = SetupPool(secondPool, PacketType.JobCreated);

        var requestPacket = RequestFactory.SubmitJob(_functionName,
            UniqueId.Create(_cid, null), _data);

        var jobOptions = new JobOptions
        {
            CorrelationId = _cid,
        };

        // Act
        var result = await _sut.Produce(_functionName, _data, jobOptions, CancellationToken.None);

        // Assert
        Assert.True(result);

        await firstConnection
            .Received(1)
            .SendPacket(Arg.Is<RequestPacket>(r => r.FullData.IsEquivalent(requestPacket.FullData)),
                CancellationToken.None);

        await secondConnection
            .Received(1)
            .SendPacket(Arg.Is<RequestPacket>(r => r.FullData.IsEquivalent(requestPacket.FullData)),
                CancellationToken.None);

        firstPool.Received(1).Return(firstConnection, true);
        secondPool.Received(1).Return(secondConnection, false);
    }
}