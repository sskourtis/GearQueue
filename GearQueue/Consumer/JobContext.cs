using GearQueue.Protocol.Response;
using GearQueue.Serialization;

namespace GearQueue.Consumer;

public class JobContext
{
    private readonly IEnumerable<JobContext>? _batchContexts;
    private readonly JobAssign? _jobAssign;

    internal JobResult? Result { get; private set; }

    public CancellationToken CancellationToken { get; init; }

    public ReadOnlySpan<byte> Data => _jobAssign == null 
        ? throw new Exception("Job context has not been assigned, is this a batch context?") 
        : _jobAssign.Data;

    public virtual IEnumerable<JobContext> Batches => _batchContexts is null
        ? throw new Exception("This is not a batch context")
        : _batchContexts;
    
    public string FunctionName { get; init; }
    
    public Type? HandlerType { get; internal set; }
    
    public string? BatchKey { get; init; }
    
    public bool IsBatch => _batchContexts != null;


    protected JobContext(string functionName, CancellationToken cancellationToken)
    {
        FunctionName = functionName;
        CancellationToken = cancellationToken;
    }

    internal JobContext(JobAssign jobAssign, CancellationToken cancellationToken)
    {;
        CancellationToken = cancellationToken;
        _jobAssign = jobAssign;
        FunctionName = jobAssign.FunctionName;
    }
    
    internal JobContext(string functionName, IEnumerable<JobAssign> jobs, string? batchKey, CancellationToken cancellationToken)
    {
        CancellationToken = cancellationToken;
        _batchContexts = jobs.Select(x => new JobContext(x, cancellationToken));
        FunctionName = functionName;
        BatchKey = batchKey;
    }

    public void SetResult(JobResult result)
    {
        Result = result;
    }
}

public class JobContext<T> : JobContext
{
    private readonly IGearQueueSerializer _serializer;
    private readonly IEnumerable<JobContext<T>>? _batchContexts;
    public override IEnumerable<JobContext<T>> Batches => _batchContexts is null
        ? throw new Exception("This is not a batch context")
        : _batchContexts;
    
    private T? _job;
    
    internal JobContext(IGearQueueSerializer serializer, JobAssign jobAssign, CancellationToken cancellationToken) : base(jobAssign, cancellationToken)
    {
        _serializer = serializer;
    }

    internal JobContext(IGearQueueSerializer serializer, string functionName, IEnumerable<JobAssign> jobs, string? batchKey, CancellationToken cancellationToken) : base(functionName, cancellationToken)
    {
        _serializer = serializer;
        _batchContexts = jobs.Select(x => new JobContext<T>(serializer, x, cancellationToken));
        BatchKey = batchKey;
    }

    public T Job
    {
        get
        {
            _job ??= _serializer.Deserialize<T>(Data);

            return _job;
        }
    }
}