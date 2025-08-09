using GearQueue.Network;

namespace GearQueue.Options.Builders;

public class GearQueueConsumerServiceOptionsBuilder(GearQueueConsumerServerOptions options)
{
    public GearQueueConsumerServiceOptionsBuilder WithConnection(int connections)
    {
        options.Connections = connections;
        return this;
    }
    
    public GearQueueConsumerServiceOptionsBuilder WithReconnectTimeout(TimeSpan timeout)
    {
        options.ReconnectTimeout = timeout;
        return this;
    }
    
    public GearQueueConsumerServiceOptionsBuilder WithConnectionTimeout(TimeSpan timeout)
    {
        options.ConnectionTimeout = timeout;
        return this;
    }
    
    public GearQueueConsumerServiceOptionsBuilder WithReceiveTimeout(TimeSpan timeout)
    {
        options.ReceiveTimeout = timeout;
        return this;
    }
    
    public GearQueueConsumerServiceOptionsBuilder WithSendTimeout(TimeSpan timeout)
    {
        options.SendTimeout = timeout;
        return this;
    }
    
    public GearQueueConsumerServiceOptionsBuilder UsePresleep()
    {
        options.UsePreSleep = true;
        return this;
    }
    
    public GearQueueConsumerServiceOptionsBuilder UsePolling(TimeSpan pollingDelay)
    {
        options.UsePreSleep = false;
        options.PollingDelay = pollingDelay;
        return this;
    }
}