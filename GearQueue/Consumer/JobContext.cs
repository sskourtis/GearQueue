using GearQueue.Protocol.Response;
using GearQueue.Serialization;

namespace GearQueue.Consumer;

public class JobContext
{
    private readonly IGearQueueJobSerializer? _serializer;
    private readonly IEnumerable<JobContext>? _batchContexts;
    private readonly JobAssign? _jobAssign;
    
    internal int? ConnectionId { get; set; }
    
    internal JobResult? Result { get; private set; }
    
    internal string JobHandle => _jobAssign!.JobHandle;

    public CancellationToken CancellationToken { get; init; }

    public ReadOnlySpan<byte> Data => _jobAssign == null 
        ? throw new Exception("Job context has not been assigned, is this a batch context?") 
        : _jobAssign.Data;

    public virtual IEnumerable<JobContext> Batches => _batchContexts is null
        ? throw new Exception("This is not a batch context")
        : _batchContexts;
    
    public string FunctionName => _jobAssign?.FunctionName ?? _batchContexts?.First().FunctionName ?? throw new Exception("Job context has not been assigned");
    
    public Type? HandlerType { get; internal set; }
    
    public string? BatchKey { get; init; }
    
    public bool IsBatch => _batchContexts != null;
    
    public T ToJob<T>() 
    {
        return _serializer!.Deserialize<T>(Data);
    }

    internal JobContext(JobAssign jobAssign, IGearQueueJobSerializer? serializer, int? connectionId, CancellationToken cancellationToken)
    {;
        _serializer = serializer;
        ConnectionId = connectionId;
        CancellationToken = cancellationToken;
        _jobAssign = jobAssign;
    }
    
    internal JobContext(List<(int ConnectionId, JobAssign Job)> jobs, string? batchKey, IGearQueueJobSerializer? serializer, CancellationToken cancellationToken)
    {
        _serializer = serializer;
        CancellationToken = cancellationToken;
        _batchContexts = jobs.Select(x => new JobContext(x.Job, serializer, x.ConnectionId, cancellationToken)).ToArray();
        BatchKey = batchKey;
    }

    public void SetResult(JobResult result)
    {
        Result = result;
    }
}