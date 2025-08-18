namespace GearQueue.Options.Validation;

public class GearQueueConsumerOptionsValidator : IGearQueueValidator<GearQueueConsumerOptions>
{
    public ValidationResult Validate(GearQueueConsumerOptions options)
    {
        var serverValidator = new GearQueueHostOptionsValidator();
        
        var errors = new List<string>();
        
        if (options.Hosts.Count == 0)
        {
            errors.Add("No gearman servers configured");
            
            return new ValidationResult(errors);
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

        foreach (var servers in options.Hosts)
        {
            var serverValidationResult = serverValidator.Validate(servers.Host);

            if (!serverValidationResult.IsValid)
            {
                errors.AddRange(serverValidationResult.Errors);    
            }
            
            if (!servers.UsePreSleep && servers.PollingDelay <= TimeSpan.Zero)
            {
                errors.Add($"Server {servers.PollingDelay} should be greater than zero when presleep is disabled");
            }

            if (servers.ReconnectTimeout <=  TimeSpan.Zero)
            {
                errors.Add("Reconnect timeout must be greater than zero");
            }
        }

        return new ValidationResult(errors);
    }
}