using GearQueue.Utils;
using Microsoft.Extensions.Logging;

namespace GearQueue.Network;

internal interface IConnectionPoolFactory
{
    IConnectionPool Create(ConnectionPoolOptions options);
}

internal class ConnectionPoolFactory(
    ILoggerFactory loggerFactory,
    IConnectionFactory connectionFactory,
    ITimeProvider timeProvider)
    : IConnectionPoolFactory
{
    public IConnectionPool Create(ConnectionPoolOptions options)
    {
        return new ConnectionPool(options, loggerFactory, connectionFactory, timeProvider);
    }
}