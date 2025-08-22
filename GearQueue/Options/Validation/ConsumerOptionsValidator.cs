namespace GearQueue.Options.Validation;

public class ConsumerOptionsValidator : IValidator<ConsumerOptions>
{
    public ValidationResult Validate(ConsumerOptions options)
    {
        var serverValidator = new HostOptionsValidator();
        
        var errors = new List<string>();
        
        if (options.Hosts.Count == 0)
        {
            errors.Add("No gearman servers configured");
            
            return new ValidationResult(errors);
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