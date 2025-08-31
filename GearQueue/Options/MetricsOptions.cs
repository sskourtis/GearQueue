namespace GearQueue.Options;

public class MetricsOptions
{
    public bool PoolHealthTracking { get; set; } = true;
    public bool PoolConnectionsTracking { get; set; } = true;
    public bool JobSubmissionsTracking { get; set; } = true;
    public bool JobExecutionsTracking { get; set; } = true;
    public bool BatchSizeTracking { get; set; } = true;
    public bool HandlerWaitTimeTracking { get; set; } = true;
}