namespace GearQueue.Options.Validation;

public class GearQueueConsumerOptionsValidator
{
    public ValidationResult Validate(GearQueueConsumerOptions options)
    {
        var errors = new List<string>();
        
        if (options.Servers.Count == 0)
        {
            errors.Add("No gearman servers configured");
            
            return new ValidationResult(errors);
        }
        
        if (string.IsNullOrWhiteSpace(options.Function))
        {
            errors.Add("Function property is required");
        }

        if (options.Batch is not null)
        {
            if (options.Batch.Size <= 1)
            {
                errors.Add("Batch size must be greater than 1");
            }

            if (options.Batch.TimeLimit <= TimeSpan.Zero)
            {
                errors.Add("Batch time limit must be greater than zero");
            }

            if (options.ConcurrencyStrategy is not ConcurrencyStrategy.AcrossServers)
            {
                errors.Add("Concurrency strategy must be AcrossServers when batching is enabled");
            }
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
            
            if (!connectionPoolSettings.UsePreSleep && connectionPoolSettings.PollingDelay <= TimeSpan.Zero)
            {
                errors.Add($"Server {connectionPoolSettings.PollingDelay} should be greater than zero when presleep is disabled");
            }

            if (connectionPoolSettings.ReconnectTimeout <=  TimeSpan.Zero)
            {
                errors.Add("Reconnect timeout must be greater than zero");
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