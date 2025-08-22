using GearQueue.Protocol.Response;

namespace GearQueue.Consumer;

public delegate JobContext JobContextCreatorDelegate(JobAssign jobAssign, CancellationToken cancellationToken);
    
public class HandlerOptions
{
    public required Type Type { get; init; }
    public required JobContextFactory JobContextFactory { get; init; }
}