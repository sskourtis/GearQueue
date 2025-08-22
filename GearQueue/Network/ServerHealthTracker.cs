using GearQueue.Logging;
using GearQueue.Options;
using Microsoft.Extensions.Logging;

namespace GearQueue.Network;

internal sealed class ServerHealthTracker
{
    private enum HealthStatus { Healthy, Unhealthy }

    // State
    private HealthStatus _state = HealthStatus.Healthy;
    private int _failureCount = 0;
    private readonly Lock _stateLock = new();
    
    // Configuration
    private readonly HostOptions _hostOptions;
    private readonly int _failureThreshold;
    private readonly TimeSpan _healthCheckInterval;
    private readonly ILogger<ServerHealthTracker> _logger;
    
    internal bool IsHealthy => _state == HealthStatus.Healthy;
    internal DateTimeOffset LastFailureTime { get; private set; } = DateTimeOffset.MinValue;

    internal ServerHealthTracker(HostOptions hostOptions, 
        int failureThreshold, 
        TimeSpan healthCheckInterval,
        ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<ServerHealthTracker>();
        _hostOptions = hostOptions;
        _failureThreshold = failureThreshold;
        _healthCheckInterval = healthCheckInterval;
    }
    
    internal void ReportSuccess()
    {
        lock (_stateLock)
        {
            if (_state == HealthStatus.Unhealthy)
            {
                _logger.LogHealthyServer(_hostOptions.Hostname, _hostOptions.Port);
            }
            
            _state = HealthStatus.Healthy;
            _failureCount = 0;
        }
    }

    internal void ReportFailure()
    {
        lock (_stateLock)
        {
            LastFailureTime = DateTimeOffset.UtcNow;
            
            if (_state == HealthStatus.Healthy)
            {
                _failureCount++;
                
                if (_failureCount >= _failureThreshold)
                {
                    _logger.LogUnhealthyServer(_hostOptions.Hostname, _hostOptions.Port, _failureCount);
                    // Too many failures, mark server as unhealthy
                    _state = HealthStatus.Unhealthy;
                }
            }
        }
    }
    
    internal bool ShouldTryConnection()
    {
        lock (_stateLock)
        {
            switch (_state)
            {
                case HealthStatus.Healthy:
                    return true;
                    
                case HealthStatus.Unhealthy:
                    // In unhealthy, only allow periodic health check requests
                    var timeSinceLastCheck = DateTimeOffset.UtcNow - LastFailureTime;
                    if (timeSinceLastCheck >= _healthCheckInterval)
                    {
                        _logger.LogUnhealthyServerAttempt(_hostOptions.Hostname, _hostOptions.Port, timeSinceLastCheck);
                        return true;
                    }

                    return false;
                default:
                    return false;
            }
        }
    }
}