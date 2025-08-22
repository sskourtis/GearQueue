namespace GearQueue.Options.Validation;

public class ProducerOptionsValidator : IValidator<ProducerOptions>
{
    public ValidationResult Validate(ProducerOptions options)
    {
        var serverValidator = new HostOptionsValidator();
        
        var errors = new List<string>();
        
        if (!Enum.GetValues<DistributionStrategy>().Contains(options.DistributionStrategy))
        {
            errors.Add($"Distribution strategy {options.DistributionStrategy} not supported");
        }
        
        if (options.ConnectionPools.Count == 0)
        {
            errors.Add("No gearman servers configured");
            
            return new ValidationResult(errors);
        }

        foreach (var connectionPoolSettings in options.ConnectionPools)
        {
            var serverValidationResult = serverValidator.Validate(connectionPoolSettings.Host);

            if (!serverValidationResult.IsValid)
            {
                errors.AddRange(serverValidationResult.Errors);    
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
        }

        return new ValidationResult(errors);
    }
}