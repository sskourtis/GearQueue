using GearQueue.Protocol.Response;
using GearQueue.Serialization;

namespace GearQueue.Consumer;

public class JobContextFactory
{
    internal virtual JobContext Create(JobAssign jobAssign, CancellationToken cancellationToken)
    {
        return new JobContext(jobAssign, cancellationToken);
    }

    internal virtual JobContext CreateBatch(string functionName, IEnumerable<JobAssign> jobs, string? batchKey,
        CancellationToken cancellationToken)
    {
        return new JobContext(functionName, jobs, batchKey, cancellationToken);
    }
}

public class JobContextFactory<T>(IGearQueueSerializer serializer) : JobContextFactory
{
    internal override JobContext Create(JobAssign jobAssign, CancellationToken cancellationToken)
    {
        return new JobContext<T>(serializer, jobAssign, cancellationToken);
    }

    internal override JobContext CreateBatch(string functionName, IEnumerable<JobAssign> jobs, string? batchKey,
        CancellationToken cancellationToken)
    {
        return new JobContext<T>(serializer, functionName, jobs, batchKey, cancellationToken);
    }
}