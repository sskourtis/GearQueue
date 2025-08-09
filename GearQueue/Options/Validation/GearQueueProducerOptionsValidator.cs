using GearQueue.Producer;

namespace GearQueue.Options.Validation;

public class GearQueueProducerOptionsValidator
{
    public ValidationResult Validate(GearQueueProducerOptions options)
    {
        var errors = new List<string>();
        
        if (!Enum.GetValues<DistributionStrategy>().Contains(options.DistributionStrategy))
        {
            errors.Add($"Distribution strategy {options.DistributionStrategy} not supported");
        }
        
        if (options.Servers.Count == 0)
        {
            errors.Add("No gearman servers configured");
            
            return new ValidationResult(errors);
        }

        foreach (var connectionPoolSettings in options.Servers)
        {
            if (Uri.CheckHostName(connectionPoolSettings.ServerInfo.Hostname) == UriHostNameType.Unknown)
            {
                errors.Add($"Server {connectionPoolSettings.ServerInfo.Hostname} does not appear to be a valid hostname"); 
            }

            if (connectionPoolSettings.ServerInfo.Port is <= 0 or > 65535)
            {
                errors.Add($"Server {connectionPoolSettings.ServerInfo.Port} is not a valid port number");
            }
            
            if (connectionPoolSettings.ConnectionMaxAge <= TimeSpan.Zero)
            {
                errors.Add("connection pool max age must be greater than zero");
            }

            if (connectionPoolSettings.MaxConnections <= 0)
            {
                errors.Add("max connections must be greater than zero");
            }

            if (connectionPoolSettings.HealthErrorThreshold <= 0)
            {
                errors.Add("health error threshold must be greater than zero");
            }

            if (connectionPoolSettings.HealthCheckInterval <= TimeSpan.Zero)
            {
                errors.Add("health check interval must be greater than zero");
            }

            if (connectionPoolSettings.ConnectionTimeout <= TimeSpan.Zero)
            {
                errors.Add("connection timeout must be greater than zero");
            }
                
            if (connectionPoolSettings.ReceiveTimeout <= TimeSpan.Zero)
            {
                errors.Add("receive timeout must be greater than zero");
            }
                
            if (connectionPoolSettings.SendTimeout <= TimeSpan.Zero)
            {
                errors.Add("send timeout must be greater than zero");
            }
        }

        return new ValidationResult(errors);
    }
}