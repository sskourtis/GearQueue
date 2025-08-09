using System.Net.Sockets;
using GearQueue.Logging;
using GearQueue.Network;
using GearQueue.Protocol;
using GearQueue.Protocol.Request;
using GearQueue.Protocol.Response;
using Microsoft.Extensions.Logging;

namespace GearQueue.Consumer;

internal class GearQueueConsumerInstance(
    GearQueueConsumerServerOptions options,
    IReadOnlyDictionary<string, Type> functions,
    IGearQueueHandlerExecutor handlerExecutor,
    ILoggerFactory loggerFactory
    ) : IDisposable
{
    private readonly ILogger<GearQueueConsumerInstance> _logger = loggerFactory.CreateLogger<GearQueueConsumerInstance>();
    private readonly Connection _connection = new(loggerFactory, 
        options.ConnectionTimeout,  
        options.SendTimeout, 
        options.ReceiveTimeout);
    
    internal async Task Start(CancellationToken cancellationToken = default)
    {
        await Task.Yield();
        
        await Connect(cancellationToken).ConfigureAwait(false);
        
        while(!cancellationToken.IsCancellationRequested)
        {
            try
            {
                if (await CheckForJob(cancellationToken).ConfigureAwait(false))
                {
                    continue;
                }

                if (!options.UsePreSleep)
                {
                    /*
                     * Polling is also offered as an option instead of using the PRE_SLEEP packet
                     */
                    await Task.Delay(options.PollingDelay, cancellationToken).ConfigureAwait(false);
                    continue;
                }
                
                await _connection.SendPacket(RequestFactory.PreSleep(), cancellationToken).ConfigureAwait(false);
                
                var noopResponse = await _connection.GetPacket(cancellationToken).ConfigureAwait(false);

                if (noopResponse is not null && noopResponse.Value.Type is not PacketType.Noop)
                {
                    _logger.LogUnexpectedResponseType(noopResponse?.Type, PacketType.Noop);
                }
            }
            catch (SocketException e)
            {
                _logger.LogInformation(e, "Got Exception");
                // Reconnect to gearman server
                await Connect(cancellationToken).ConfigureAwait(false);
            }
        }
    }
    
    private async Task Connect(CancellationToken cancellationToken)
    {
        while (true)
        {
            try
            {
                await _connection.Connect(options.ServerInfo.Hostname, options.ServerInfo.Port, cancellationToken).ConfigureAwait(false);
                
                // Register functions again
                foreach (var (function, _) in functions)
                {
                    await _connection.SendPacket(RequestFactory.CanDo(function), cancellationToken).ConfigureAwait(false);
                }

                break;
            }
            catch (SocketException e)
            {
                _logger.LogSocketError(options.ServerInfo.Hostname,
                    options.ServerInfo.Port, 
                    options.ReconnectTimeout,
                    e);
                await Task.Delay(options.ReconnectTimeout, cancellationToken).ConfigureAwait(false);
            }
        }
    }
    
    private async Task<bool> CheckForJob(CancellationToken cancellationToken = default)
    {
        await _connection.SendPacket(RequestFactory.GrabJob(), cancellationToken).ConfigureAwait(false);
			
        var response = await _connection.GetPacket(cancellationToken).ConfigureAwait(false);

        if (response is null)
        {
            return false;
        }

        if (response.Value.Type != PacketType.JobAssign)
            return false;

        var job = JobAssign.Create(response.Value.Data);
        
        await CallHandler(job, cancellationToken).ConfigureAwait(false);

        return true;
    }

    private async Task CallHandler(JobAssign job, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!functions.TryGetValue(job.FunctionName, out var handlerType))
            {
                _logger.LogMissingHandlerType(job.FunctionName);
                await _connection.SendPacket(RequestFactory.WorkFail(job.JobHandle), CancellationToken.None).ConfigureAwait(false);
                return;
            }
            
            var jobContext = new JobContext(job, cancellationToken);
            
            var (success, jobStatus) = await handlerExecutor.TryExecute(handlerType, jobContext).ConfigureAwait(false);

            if (!success)
            {
                _logger.LogHandlerTypeCreationFailure(handlerType, job.FunctionName);
                await _connection.SendPacket(RequestFactory.WorkFail(job.JobHandle),
                    CancellationToken.None).ConfigureAwait(false);
                return;
            }

            if (jobStatus == JobStatus.Success)
            {
                await _connection.SendPacket(RequestFactory.WorkComplete(job.JobHandle),
                    CancellationToken.None).ConfigureAwait(false);
            }
            else
            {
                await _connection.SendPacket(RequestFactory.WorkFail(job.JobHandle),
                    CancellationToken.None).ConfigureAwait(false);
            }
        }
        catch (Exception e)
        {
            _logger.LogConsumerException(e);
            await _connection.SendPacket(RequestFactory.WorkFail(job.JobHandle),
                CancellationToken.None).ConfigureAwait(false);
        }
    }

    public void Dispose()
    {
        _connection?.Dispose();
    }
}