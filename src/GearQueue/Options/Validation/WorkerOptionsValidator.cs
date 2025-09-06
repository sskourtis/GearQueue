namespace GearQueue.Options.Validation;

public class WorkerOptionsValidator : IValidator<WorkerOptions>
{
    public ValidationResult Validate(WorkerOptions options)
    {
        var serverValidator = new HostOptionsValidator();
        
        var errors = new List<string>();

        if (options.MaxConcurrency <= 0)
        {
            errors.Add("Max concurrency must be greater than zero");
        }
        
        if (options.ConcurrencyStrategy is not 
            (ConcurrencyStrategy.AcrossServers or 
            ConcurrencyStrategy.PerServer or 
            ConcurrencyStrategy.PerConnection))
        {
            errors.Add($"Concurrency strategy {options.ConcurrencyStrategy} not supported");       
        }
        
        if (options.Hosts.Count == 0)
        {
            errors.Add("No gearman servers configured");
            
            return new ValidationResult(errors);
        }

        foreach (var servers in options.Hosts)
        {
            var serverValidationResult = serverValidator.Validate(servers.Host);

            if (servers.Connections <= 0)
            {
                errors.Add("Connections must be greater than zero");
            }
            
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