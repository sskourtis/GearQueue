namespace GearQueue.Options.Validation;

public class HostOptionsValidator
{
    public ValidationResult Validate(HostOptions options)
    {
        var errors = new List<string>();
        
        if (Uri.CheckHostName(options.Hostname) == UriHostNameType.Unknown)
        {
            errors.Add($"Server {options.Hostname} does not appear to be a valid hostname"); 
        }

        if (options.Port is <= 0 or > 65535)
        {
            errors.Add($"Server {options.Port} is not a valid port number");
        }
        
        if (options.ConnectionTimeout <= TimeSpan.Zero)
        {
            errors.Add("connection timeout must be greater than zero");
        }

        if (options.ReceiveTimeout <= TimeSpan.Zero)
        {
            errors.Add("receive timeout must be greater than zero");
        }
                
        if (options.SendTimeout <= TimeSpan.Zero)
        {
            errors.Add("send timeout must be greater than zero");
        }

        return new ValidationResult(errors);
    }
}