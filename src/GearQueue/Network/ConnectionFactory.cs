using GearQueue.Options;
using Microsoft.Extensions.Logging;

namespace GearQueue.Network;

internal interface IConnectionFactory
{
    IConnection CreateConnection(ILoggerFactory loggerFactory, HostOptions options);
}

internal class ConnectionFactory : IConnectionFactory
{
    public IConnection CreateConnection(ILoggerFactory loggerFactory, HostOptions options)
    {
        return new Connection(loggerFactory, options);
    }
}