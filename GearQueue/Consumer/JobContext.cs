using GearQueue.Protocol.Response;
using GearQueue.Serialization;

namespace GearQueue.Consumer;

public class JobContext
{
    private readonly IGearQueueSerializer? _serializer;
    private readonly IEnumerable<JobContext>? _batchContexts;
    private readonly JobAssign? _jobAssign;

    internal JobResult? Result { get; private set; }

    public CancellationToken CancellationToken { get; init; }

    public ReadOnlySpan<byte> Data => _jobAssign == null 
        ? throw new Exception("Job context has not been assigned, is this a batch context?") 
        : _jobAssign.Data;

    public IEnumerable<JobContext> Batches => _batchContexts is null
        ? throw new Exception("This is not a batch context")
        : _batchContexts;
    
    public string FunctionName { get; init; }
    
    public Type? HandlerType { get; internal set; }
    
    public string? BatchKey { get; init; }
    
    public bool IsBatch => _batchContexts != null;
    

    internal JobContext(IGearQueueSerializer? serializer, JobAssign jobAssign, CancellationToken cancellationToken)
    {
        _serializer = serializer;
        CancellationToken = cancellationToken;
        _jobAssign = jobAssign;
        FunctionName = jobAssign.FunctionName;
    }
    
    internal JobContext(IGearQueueSerializer? serializer, string functionName, IEnumerable<JobAssign> jobs, string? batchKey, CancellationToken cancellationToken)
    {
        _serializer = serializer;
        CancellationToken = cancellationToken;
        _batchContexts = jobs.Select(x => new JobContext(serializer, x, cancellationToken));
        FunctionName = functionName;
        BatchKey = batchKey;
    }

    public void SetResult(JobResult result)
    {
        Result = result;
    }

    public T To<T>()
    {
        return _serializer!.Deserialize<T>(Data);
    }

    public IEnumerable<T> ToBatch<T>()
    {
        foreach (var job in Batches)
        {
            yield return job.To<T>();
        }
    }
}