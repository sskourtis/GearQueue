using GearQueue.Options;
using Microsoft.Extensions.Logging;

namespace GearQueue.Network;

internal interface IConnectionFactory
{
    IConnection CreateConnection(ILoggerFactory loggerFactory, GearQueueHostOptions options);
}

internal class ConnectionFactory : IConnectionFactory
{
    public IConnection CreateConnection(ILoggerFactory loggerFactory, GearQueueHostOptions options)
    {
        return new Connection(loggerFactory, options);
    }
}