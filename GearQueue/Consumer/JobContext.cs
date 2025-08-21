using GearQueue.Protocol.Response;

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

    public IEnumerable<JobContext> Batches => _batchContexts is null
        ? throw new Exception("This is not a batch context")
        : _batchContexts;
    
    public string FunctionName { get; init; }
    
    public Type? HandlerType { get; internal set; }
    

    internal JobContext(JobAssign jobAssign, CancellationToken cancellationToken)
    {
        CancellationToken = cancellationToken;
        _jobAssign = jobAssign;
        FunctionName = jobAssign.FunctionName;
    }
    
    internal JobContext(string functionName, IEnumerable<JobAssign> jobs, CancellationToken cancellationToken)
    {
        CancellationToken = cancellationToken;
        _batchContexts = jobs.Select(x => new JobContext(x, cancellationToken));
        FunctionName = functionName;
    }

    public void SetResult(JobResult result)
    {
        Result = result;
    }
}