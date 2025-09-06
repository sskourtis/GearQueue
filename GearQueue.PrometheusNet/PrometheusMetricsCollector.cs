using System.Collections.Concurrent;
using GearQueue.Metrics;
using GearQueue.Options;
using GearQueue.Worker;

namespace GearQueue.PrometheusNet;

public class PrometheusMetricsCollector(MetricsOptions options, GearQueueMetrics? metrics = null) : IMetricsCollector
{
    private readonly GearQueueMetrics _metrics = metrics ?? new GearQueueMetrics();
    
    private static readonly ConcurrentDictionary<(string, int), string[]> HostLabelsCache = new();
    private static readonly ConcurrentDictionary<string, string[]> FunctionNameLabelsCache = new();
    private static readonly ConcurrentDictionary<(string, JobResult), string[]> FunctionNameWithStatusLabelsCache = new();
    private static readonly ConcurrentDictionary<(string, string?), string[]> FunctionNameWithBatchKeyLabelsCache = new();
    
    public void PoolIsHealthy(string host, int port)
    {
        if (!options.PoolHealthTracking)
        {
            return;
        }
        
        var labels = HostLabelsCache.GetOrAdd((host, port), _ => [$"{host}:{port}"]);
        _metrics.PoolHealthGauge.WithLabels(labels).Set(1);
    }

    public void PoolIsUnhealthy(string host, int port)
    {
        if (!options.PoolHealthTracking)
        {
            return;
        }
        
        var labels = HostLabelsCache.GetOrAdd((host, port), _ => [$"{host}:{port}"]);
        _metrics.PoolHealthGauge.WithLabels(labels).Set(0);
    }

    public void PoolConnectionCreated(string host, int port, TimeSpan waitTime)
    {
        if (!options.PoolConnectionsTracking)
        {
            return;
        }
        
        var labels = HostLabelsCache.GetOrAdd((host, port), _ => [$"{host}:{port}"]);
        
        _metrics.PoolConnectionsReturnedWaitTime.WithLabels(labels).Observe(waitTime.TotalSeconds);
        _metrics.PoolTotalConnectionsGauge.WithLabels(labels).Inc();
        _metrics.PoolConnectionsCreatedCounter.WithLabels(labels).Inc();
    }

    public void PoolConnectionReused(string host, int port, TimeSpan waitTime)
    {
        if (!options.PoolConnectionsTracking)
        {
            return;
        }
        
        var labels = HostLabelsCache.GetOrAdd((host, port), _ => [$"{host}:{port}"]);
        
        _metrics.PoolConnectionsReturnedWaitTime.WithLabels(labels).Observe(waitTime.TotalSeconds);
        _metrics.PoolConnectionsReusedCounter.WithLabels(labels).Inc();
    }

    public void PoolConnectionDiscarded(string host, int port)
    {
        if (!options.PoolConnectionsTracking)
        {
            return;
        }
        
        var labels = HostLabelsCache.GetOrAdd((host, port), _ => [$"{host}:{port}"]);
        _metrics.PoolTotalConnectionsGauge.WithLabels(labels).Dec();
        _metrics.PoolConnectionsDiscardedCounter.WithLabels(labels).Inc();
    }

    public void PoolConnectionErrored(string host, int port)
    {
        if (!options.PoolConnectionsTracking)
        {
            return;
        }
        
        var labels = HostLabelsCache.GetOrAdd((host, port), _ => [$"{host}:{port}"]);
        _metrics.PoolConnectionsFailedCounter.WithLabels(labels).Inc();
    }

    public void JobSubmitted(string functionName)
    {
        if (!options.JobSubmissionsTracking)
        {
            return;
        }
        
        var labels = FunctionNameLabelsCache.GetOrAdd(functionName, _ => [functionName]);
        _metrics.JobsSubmittedCounter.WithLabels(labels).Inc();
    }

    public void JobSubmitErrored(string functionName)
    {
        if (!options.JobSubmissionsTracking)
        {
            return;
        }
        
        var labels = FunctionNameLabelsCache.GetOrAdd(functionName, _ => [functionName]);
        _metrics.JobsFailedToSubmitCounter.WithLabels(labels).Inc();
    }

    public void JobHandlingStarted(string functionName, int amount)
    {
        if (!options.JobExecutionsTracking)
        {
            return;
        }
        
        var labels = FunctionNameLabelsCache.GetOrAdd(functionName, _ => [functionName]);
        _metrics.JobsInProgressGauge.WithLabels(labels).Inc(amount);
    }

    public void JobsHandled(string functionName, JobResult result, TimeSpan duration, int amount)
    {
        if (!options.JobExecutionsTracking)
        {
            return;
        }
        
        var labels = FunctionNameWithStatusLabelsCache.GetOrAdd((functionName, result), _ => [functionName, result.ToString()]);
        var onlyFunctionNameLabels = FunctionNameLabelsCache.GetOrAdd(functionName, _ => [functionName]);
        
        _metrics.JobsInProgressGauge.WithLabels(onlyFunctionNameLabels).Dec(amount);
        _metrics.JobsHandledCounter.WithLabels(labels).Inc(amount);
        _metrics.JobsHandledHistogram.WithLabels(labels).Observe(duration.TotalSeconds, amount);
    }

    public void BatchedJobPreparedWithSize(string functionName, string? key, int size)
    {
        if (!options.BatchSizeTracking)
        {
            return;
        }
        
        var labels = FunctionNameWithBatchKeyLabelsCache.GetOrAdd((functionName, key), _ => [functionName, key ?? "-"]);
        _metrics.BatchSizeHistogram.WithLabels(labels).Observe(size);
    }

    public void HandlerWaitTime(TimeSpan duration)
    {
        if (!options.HandlerWaitTimeTracking)
        {
            return;
        }
        
        _metrics.HandlerWaitTimeHistogram.Observe(duration.TotalSeconds);
    }
}