using Prometheus;

namespace GearQueue.PrometheusNet;

public class GearQueueMetrics
{
    public Counter PoolConnectionsCreatedCounter { get; init; } = 
        Prometheus.Metrics.CreateCounter("gearqueue_pool_connections_created_total",
            "Number of connections created by the pool",
            "host");
    
    public Counter PoolConnectionsReusedCounter { get; init; } = 
        Prometheus.Metrics.CreateCounter("gearqueue_pool_connections_reused_total",
            "Number of connections reused by the pool",
            "host");
    
    public Counter PoolConnectionsDiscardedCounter { get; init; } = 
        Prometheus.Metrics.CreateCounter("gearqueue_pool_connections_discarded_total",
            "Number of connections discarded by the pool",
            "host");
    
    public Counter PoolConnectionsFailedCounter { get; init; } = 
        Prometheus.Metrics.CreateCounter("gearqueue_pool_connections_failed_total",
            "Number of connections failed",
            "host");
    
    public Gauge PoolTotalConnectionsGauge { get; init; } = 
        Prometheus.Metrics.CreateGauge("gearqueue_pool_connections_total",
            "Number of connections in pool",
            "host");
    
    public Gauge PoolHealthGauge { get; init; } =
        Prometheus.Metrics.CreateGauge("gearqueue_pool_healthy",
            "If pool is healthy",
            "host");
    
    public Histogram PoolConnectionsReturnedWaitTime { get; init; } =
        Prometheus.Metrics.CreateHistogram("gearqueue_pool_connections_returned_wait_seconds",
            "Number of connections created by the pool",
            configuration: new HistogramConfiguration
            {
                Buckets = [0.1, 0.2, 0.5, 1, 2, 5, 10],
                LabelNames = ["host"]
            });
    
    public Counter JobsSubmittedCounter { get; init; } = Prometheus.Metrics.CreateCounter(
        name: "gearqueue_jobs_submitted_total",
        help: "Total number of jobs submitted to the queue successfully",
        configuration: new CounterConfiguration
        {
            LabelNames = ["function_name"],
        }
    );
    
    public Counter JobsFailedToSubmitCounter { get; init; } = Prometheus.Metrics.CreateCounter(
        name: "gearqueue_jobs_failed_to_submit_total",
        help: "Total number of jobs that were failed to be submitted to the queue",
        configuration: new CounterConfiguration
        {
            LabelNames = ["function_name"],
        }
    );

    public Gauge JobsInProgressGauge { get; init; } = Prometheus.Metrics
        .CreateGauge("gearqueue_jobs_in_progress",
            "Number of jobs currently being processed", 
            "function_name");
    
    public Counter JobsHandledCounter { get; init; } = Prometheus.Metrics.CreateCounter(
        name: "gearqueue_jobs_handled_total",
        help: "Elapsed time of the query",
        configuration: new CounterConfiguration
        {
            LabelNames = ["function_name", "status"],
        }
    );
    
    public Histogram JobsHandledHistogram { get; init; } = Prometheus.Metrics.CreateHistogram(
        name: "gearqueue_jobs_handled_seconds",
        help: "Elapsed time of the query",
        configuration: new HistogramConfiguration
        {
            Buckets = [10.0, 100, 300, 500, 1000, 2500, 5000, 10000],
            LabelNames = ["function_name", "status"],
        }
    );
    
    public Histogram BatchSizeHistogram = Prometheus.Metrics.CreateHistogram(
        name: "gearqueue_batch_size",
        help: "Elapsed time of the query",
        configuration: new HistogramConfiguration
        {
            Buckets = [10, 20, 50, 100, 500, 750, 1000],
            LabelNames = ["function_name", "key"],
        }
    );
    
    public Histogram HandlerWaitTimeHistogram =  Prometheus.Metrics.CreateHistogram(
        name: "gearqueue_handler_wait_seconds",
        help: "Elapsed time of the query",
        configuration: new HistogramConfiguration
        {
            Buckets = [0.01, 0.05, 0.1, 0.3, 0.7, 1, 3, 5, 10]
        }
    );
}