using System.Net.Sockets;
using GearQueue.Protocol;
using Microsoft.Extensions.Logging;

namespace GearQueue.Logging;

internal static partial class LogMessages
{
    [LoggerMessage(
        Message = "Got exception while consuming jobs from gearman",
        Level = LogLevel.Error)]
    internal static partial void LogWorkerException(
        this ILogger logger,
        Exception exception);
    
    [LoggerMessage(
        Message = "Couldn't not find handler type for {Function}",
        Level = LogLevel.Error)]
    internal static partial void LogMissingHandlerType(
        this ILogger logger,
        string function);
    
    [LoggerMessage(
        Message = "Couldn't not activate handler {Type} for {Function}",
        Level = LogLevel.Error)]
    internal static partial void LogHandlerTypeCreationFailure(
        this ILogger logger,
        Type type,
        string function);
    
    [LoggerMessage(
        Message = "Received gearman response type {ReceivedType} while expecting {ExpectedType}",
        Level = LogLevel.Warning)]
    internal static partial void LogUnexpectedResponseType(
        this ILogger logger,
        PacketType? receivedType,
        PacketType expectedType);
    
    [LoggerMessage(
        Message = "Connection with {Id} established to gearman server {Host} and {Port}",
        Level = LogLevel.Information)]
    internal static partial void LogConnectionEstablished(
        this ILogger logger,
        string host,
        int port,
        int id);
    
    [LoggerMessage(
        Message = "Connection with {Id} was closed",
        Level = LogLevel.Information)]
    internal static partial void LogConnectionClosed(
        this ILogger logger,
        int id);
    
    [LoggerMessage(
        Message = "Encountered socket error to gearman server {Host} and {Port}",
        Level = LogLevel.Error)]
    internal static partial void LogSocketError(
        this ILogger logger,
        string host,
        int port,
        SocketException exception);
    
    [LoggerMessage(
        Message = "Encountered socket error to gearman server {Host} and {Port}. Retry in {RetryDelay}",
        Level = LogLevel.Error)]
    internal static partial void LogSocketError(
        this ILogger logger,
        string host,
        int port,
        TimeSpan retryDelay,
        SocketException exception);

    [LoggerMessage(
        Message = "Starting worker to {Host} and {Port} with {Concurrency} for {Function}",
        Level = LogLevel.Information)]
    internal static partial void LogStartingWorker(
        this ILogger logger,
        string host,
        int port,
        int concurrency,
        string function
    );

    [LoggerMessage(
        Message = "Server {Host} and {Port} is marked as unhealthy with {FailureCount}",
        Level = LogLevel.Error)]
    internal static partial void LogUnhealthyServer(
        this ILogger logger,
        string host,
        int port,
        int failureCount
    ); 
    
    [LoggerMessage(
        Message = "Server {Host} and {Port} has become healthy",
        Level = LogLevel.Information)]
    internal static partial void LogHealthyServer(
        this ILogger logger,
        string host,
        int port
    ); 
    
    [LoggerMessage(
        Message = "Attempt new connection to unhealthy server {Host} and {Port} after {TimeSinceLastCheck}",
        Level = LogLevel.Information)]
    internal static partial void LogUnhealthyServerAttempt(
        this ILogger logger,
        string host,
        int port,
        TimeSpan timeSinceLastCheck
    ); 
    
    [LoggerMessage(
        Message = "Server {Host} and {Port} has become the new primary server",
        Level = LogLevel.Information)]
    internal static partial void LogNewPrimaryServer(
        this ILogger logger,
        string host,
        int port
    ); 
}