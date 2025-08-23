using System.Text;
using GearQueue.Producer;
using GearQueue.Serialization;
using GearQueue.UnitTests.Utils;
using NSubstitute;

namespace GearQueue.UnitTests.Producer;

public class TypedProducerTests
{
    private readonly string _functionName = "test-" + RandomData.GetString(10);
    private readonly IGearQueueJobSerializer _serializer = Substitute.For<IGearQueueJobSerializer>();
    private readonly IProducer _producer = Substitute.For<IProducer>();

    private class DummyJob
    {
        public string? FieldA { get; set; }
        public string? FieldB { get; set; }
    }

    [Fact]
    public async Task Produce_ShouldSucceed_WithoutJobOptions()
    {
        // Arrange
        var job = new DummyJob { FieldA = RandomData.GetString(10), FieldB = RandomData.GetString(20) };
        
        var data = Encoding.UTF8.GetBytes(job.FieldA).ToList();
        data.AddRange(Encoding.UTF8.GetBytes(job.FieldB));
        var rawData = data.ToArray();
        
        _serializer.Serialize(job).Returns(rawData);
        _producer.Produce(_functionName, rawData, Arg.Any<CancellationToken>()).Returns(true);
        
        var sut = new Producer<DummyJob>(_functionName, _serializer, _producer);
        
        // Act
        var result = await sut.Produce(job, CancellationToken.None);

        // Assert
        Assert.True(result);
        
        await _producer.Received(1).Produce(_functionName, rawData, Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task Produce_ShouldSucceed_WithJobOptions()
    {
        // Arrange
        var job = new DummyJob { FieldA = RandomData.GetString(10), FieldB = RandomData.GetString(20) };
        
        var data = Encoding.UTF8.GetBytes(job.FieldA).ToList();
        data.AddRange(Encoding.UTF8.GetBytes(job.FieldB));
        var rawData = data.ToArray();
        
        var jobOptions = new JobOptions
        {
            BatchKey = RandomData.GetString(5),
            CorrelationId = RandomData.GetString(10),
        };
        
        _serializer.Serialize(job).Returns(rawData);
        _producer.Produce(_functionName, rawData, jobOptions, Arg.Any<CancellationToken>()).Returns(true);
        
        var sut = new Producer<DummyJob>(_functionName, _serializer, _producer);
        
        // Act
        var result = await sut.Produce(job, jobOptions, CancellationToken.None);

        // Assert
        Assert.True(result);
        
        await _producer.Received(1).Produce(_functionName, rawData, jobOptions,Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task Produce_ShouldFail_WhenProducerWithoutJobOptionsFails()
    {
        // Arrange
        var job = new DummyJob { FieldA = RandomData.GetString(10), FieldB = RandomData.GetString(20) };
        
        var data = Encoding.UTF8.GetBytes(job.FieldA).ToList();
        data.AddRange(Encoding.UTF8.GetBytes(job.FieldB));
        var rawData = data.ToArray();
        
        _serializer.Serialize(job).Returns(rawData);
        _producer.Produce(_functionName, rawData, Arg.Any<CancellationToken>()).Returns(false);
        
        var sut = new Producer<DummyJob>(_functionName, _serializer, _producer);
        
        // Act
        var result = await sut.Produce(job, CancellationToken.None);

        // Assert
        Assert.False(result);
        
        await _producer.Received(1).Produce(_functionName, rawData, Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task Produce_ShouldFail_WhenProducerWithJobOptionsFails()
    {
        // Arrange
        var job = new DummyJob { FieldA = RandomData.GetString(10), FieldB = RandomData.GetString(20) };
        
        var data = Encoding.UTF8.GetBytes(job.FieldA).ToList();
        data.AddRange(Encoding.UTF8.GetBytes(job.FieldB));
        var rawData = data.ToArray();
        
        var jobOptions = new JobOptions
        {
            BatchKey = RandomData.GetString(5),
            CorrelationId = RandomData.GetString(10),
        };
        
        _serializer.Serialize(job).Returns(rawData);
        _producer.Produce(_functionName, rawData, jobOptions, Arg.Any<CancellationToken>()).Returns(false);
        
        var sut = new Producer<DummyJob>(_functionName, _serializer, _producer);
        
        // Act
        var result = await sut.Produce(job, jobOptions, CancellationToken.None);

        // Assert
        Assert.False(result);
        
        await _producer.Received(1).Produce(_functionName, rawData, jobOptions,Arg.Any<CancellationToken>());
    }
}