using GearQueue.Protocol.Response;

namespace GearQueue.BatchConsumer;

public class BatchJobContext
{
    public IEnumerable<JobAssign> Jobs { get; init; }
    
    public CancellationToken CancellationToken { get; init; }
    
    internal BatchJobContext(IEnumerable<JobAssign> jobs, CancellationToken cancellationToken)
    {
        CancellationToken = cancellationToken;
        Jobs = jobs;
    }
}