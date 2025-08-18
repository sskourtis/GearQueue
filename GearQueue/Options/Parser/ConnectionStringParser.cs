using GearQueue.Options.Validation;

namespace GearQueue.Options.Parser;

public static class ConnectionStringParser
{
    private const string HostsPropertyName = "Hosts";
    
    private static readonly string[] ConsumerHostProperties = typeof(GearQueueConsumerHostsOptions)
        .GetProperties()
        .Where(p => p.Name != "Host")
        .Select(p => p.Name)
        .ToArray();

    private static readonly string[] ConnectionPoolProperties = typeof(ConnectionPoolOptions)
        .GetProperties()
        .Where(p => p.Name != "Host")
        .Select(p => p.Name)
        .ToArray();

    private static readonly string[] HostProperties = typeof(GearQueueHostOptions)
        .GetProperties()
        .Where(p => p.Name != "Hostname" || p.Name != "Port")
        .Select(p => p.Name)
        .ToArray();

    public static GearQueueProducerOptions ParseToProducerOptions(string connectionString)
    {
        var configOptions = GetConfiguration(connectionString);

        var options = new GearQueueProducerOptions();

        foreach (var option in configOptions)
        {
            if (option!.Name == HostsPropertyName)
            {
                options.ConnectionPools = ParseHosts(option.Value)
                    .Select(h => new ConnectionPoolOptions
                    {
                        Host = h
                    }).ToList();
            }
            else if (ConnectionPoolProperties.Contains(option.Name))
            {
                foreach (var pool in options.ConnectionPools)
                {
                    TrySetPropertyValue(pool, option.Name ,option.Value);    
                }
            }
            else if (HostProperties.Contains(option.Name))
            {
                foreach (var pool in options.ConnectionPools)
                {
                    TrySetPropertyValue(pool.Host, option.Name ,option.Value);    
                }
            }
            else
            {
                TrySetPropertyValue(options, option.Name, option.Value);
            }
        }

        var validator = new GearQueueProducerOptionsValidator();
        
        var result = validator.Validate(options);
        
        result.ThrowIfInvalid();
        
        return options;
    }
    
    /// <summary>
    /// Converts a GearQueue connection string to consumer options
    /// </summary>
    /// <param name="connectionString"></param>
    /// <returns></returns>
    public static GearQueueConsumerOptions ParseToConsumerOptions(string connectionString)
    {
        var configOptions = GetConfiguration(connectionString);

        var options = new GearQueueConsumerOptions();

        foreach (var option in configOptions)
        {
            if (option!.Name == HostsPropertyName)
            {
                options.Hosts = ParseHosts(option.Value)
                    .Select(h => new GearQueueConsumerHostsOptions
                    {
                        Host = h
                    }).ToList();
            }
            else if (ConsumerHostProperties.Contains(option.Name))
            {
                foreach (var optionsHost in options.Hosts)
                {
                    TrySetPropertyValue(optionsHost, option.Name ,option.Value);    
                }
            }
            else if (HostProperties.Contains(option.Name))
            {
                foreach (var optionsHost in options.Hosts)
                {
                    TrySetPropertyValue(optionsHost.Host, option.Name ,option.Value);    
                }
            }
            else if (option.Name.StartsWith("Batch"))
            {
                options.Batch ??= new BatchOptions();
                
                TrySetPropertyValue(options.Batch, option.Name[5..] ,option.Value);
            }
            else
            {
                TrySetPropertyValue(options, option.Name, option.Value);
            }
        }

        var validator = new GearQueueConsumerOptionsValidator();
        
        var result = validator.Validate(options);
        
        result.ThrowIfInvalid();
        
        return options;
    }

    private static IEnumerable<GearQueueHostOptions> ParseHosts(string hostsString)
    {
        return hostsString
            .Split(',')
            .Select(s => s.Trim())
            .Select(s => s.Split(':'))
            .Select(s => new GearQueueHostOptions
            {
                Hostname = s[0],
                Port = s.Length > 1 
                    ? int.TryParse(s[1], out var port)
                        ? port
                        : 4730
                    : 4730,
            });
    }

    private static IEnumerable<(string Name, string Value)> GetConfiguration(string connectionString)
    {
        return connectionString.Split(";")
            .Select(c =>
            {
                var parts = c.Split("=");

                if (parts.Length != 2)
                {
                    return default;
                }

                return (parts[0].Trim(), parts[1].Trim());
            })
            .Where(o => o != default)
            .GroupBy(o => o.Item1, o => o)
            .Select(g => g.First())
            // Order by "Hosts" to create the lists before handling the rest of the properties
            .OrderBy(o => o.Item1 == "Hosts"
                ? 0
                : 1);
    }

    private static void TrySetPropertyValue<T>(T obj, string name, string value)
    {
        var property = typeof(T).GetProperty(name);
        
        if (property == null)
        {
            return;
        }
        
        if (property.PropertyType == typeof(int))
        {
            if (int.TryParse(value, out var intValue))
            {
                property.SetValue(obj, intValue);
            }
        }
        else if (property.PropertyType == typeof(DistributionStrategy))
        {
            if (Enum.TryParse(typeof(DistributionStrategy), value, out var enumValue))
            {
                property.SetValue(obj, enumValue);
            }
        }
        else if (property.PropertyType == typeof(ConcurrencyStrategy))
        {
            if (Enum.TryParse(typeof(ConcurrencyStrategy), value, out var enumValue))
            {
                property.SetValue(obj, enumValue);
            }
        }
        else if (property.PropertyType == typeof(bool))
        {
            if (bool.TryParse(value, out var boolValue))
            {
                property.SetValue(obj, boolValue);
            }
        }
        else if (property.PropertyType == typeof(TimeSpan))
        {
            if (TimeSpan.TryParse(value, out var timeSpanValue))
            {
                property.SetValue(obj, timeSpanValue);
            }
        }
        else if (property.PropertyType == typeof(string))
        {
            property.SetValue(obj, value);
        }
    }
}