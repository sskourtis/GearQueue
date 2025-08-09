using GearQueue.Protocol.Response;

namespace GearQueue.Consumer;

public class JobContext
{
    private readonly JobAssign _jobAssign;
    public CancellationToken CancellationToken { get; init; }
    public string JobHandle => _jobAssign.JobHandle;
    public ReadOnlySpan<byte> Data => _jobAssign.Data;

    internal JobContext(JobAssign jobAssign, CancellationToken cancellationToken)
    {
        CancellationToken = cancellationToken;
        _jobAssign = jobAssign;
    }
}