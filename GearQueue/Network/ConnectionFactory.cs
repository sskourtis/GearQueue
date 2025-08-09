using Microsoft.Extensions.Logging;

namespace GearQueue.Network;

internal interface IConnectionFactory
{
    IConnection CreateConnection(ILoggerFactory loggerFactory,
        TimeSpan connectionTimeout, 
        TimeSpan sendTimeout, 
        TimeSpan receiveTimeout);
}

internal class ConnectionFactory : IConnectionFactory
{
    public IConnection CreateConnection(ILoggerFactory loggerFactory, 
        TimeSpan connectionTimeout, 
        TimeSpan sendTimeout, 
        TimeSpan receiveTimeout)
    {
        return new Connection(loggerFactory, connectionTimeout, sendTimeout, receiveTimeout);
    }
}