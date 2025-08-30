using GearQueue.Consumer;
using GearQueue.Options;
using GearQueue.Serialization;
using NSubstitute;

namespace GearQueue.UnitTests.Consumer.Batch;

public partial class BatchJobManagerTests
{
    private readonly int _connectionId = Random.Shared.Next(1, 200);
    private readonly TimeSpan _timeLimit = TimeSpan.FromSeconds(Random.Shared.Next(2, 5));
    private readonly List<BatchJobManager.BatchData> _pendingBatches = [];
    private readonly TimeProvider _timeProvider = Substitute.For<TimeProvider>();
    
    private BatchJobManager SetupSut(string functionName, HandlerOptions handlerOptions)
    {
        return new BatchJobManager(functionName, handlerOptions, _timeProvider, _pendingBatches);
    }
    
    private HandlerOptions CreateOptions(int size, TimeSpan? timeLimit = null, IGearQueueJobSerializer? serializer = null)
    {
        return new HandlerOptions
        {
            // For testing purpose we just need to have a type
            // it doesn't have to be a valid handler.
            Type = typeof(BatchJobManagerTests),
            Serializer = serializer,
            Batch = new BatchOptions
            {
                Size = size,
                TimeLimit = timeLimit ?? _timeLimit,
            }
        };
    }
    
    private HandlerOptions CreateOptions<T>(int size, TimeSpan? timeLimit = null, IGearQueueJobSerializer? serializer = null)
    {
        return new HandlerOptions
        {
            // For testing purpose we just need to have a type
            // it doesn't have to be a valid handler.
            Type = typeof(T),
            Serializer = serializer,
            Batch = new BatchOptions
            {
                Size = size,
                TimeLimit = timeLimit ?? _timeLimit,
            }
        };
    }
    
    private HandlerOptions CreateOptionsByKey(int size, TimeSpan? timeLimit = null, IGearQueueJobSerializer? serializer = null)
    {
        var options = CreateOptions(size, timeLimit, serializer);
        options.Batch!.ByKey = true;
        return options;
    }
    
    private HandlerOptions CreateOptionsByKey<T>(int size, TimeSpan? timeLimit = null, IGearQueueJobSerializer? serializer = null)
    {
        var options = CreateOptions<T>(size, timeLimit, serializer);
        options.Batch!.ByKey = true;
        return options;
    }
}