using GearQueue.Consumer;

namespace GearQueue.Metrics;

public interface IMetricsCollector
{
    void PoolIsHealthy(string host, int port);
    void PoolIsUnhealthy(string host, int port);
    void PoolConnectionCreated(string host, int port, TimeSpan waitTime);
    void PoolConnectionReused(string host, int port, TimeSpan waitTime);
    void PoolConnectionDiscarded(string host, int port);
    void PoolConnectionErrored(string host, int port);
    void JobSubmitted(string functionName);
    void JobSubmitErrored(string functionName);
    void JobsHandled(string functionName, JobResult result, TimeSpan duration, int amount);
    void BatchedJobPreparedWithSize(string functionName, string? key, int size);
    void HandlerWaitTime(TimeSpan duration);
}