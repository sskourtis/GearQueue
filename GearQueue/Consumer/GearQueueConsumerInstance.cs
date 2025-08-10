using System.Net.Sockets;
using GearQueue.Consumer.Coordinators;
using GearQueue.Logging;
using GearQueue.Network;
using GearQueue.Options;
using GearQueue.Protocol;
using GearQueue.Protocol.Request;
using GearQueue.Protocol.Response;
using Microsoft.Extensions.Logging;

namespace GearQueue.Consumer;

internal class GearQueueConsumerInstance(
    GearQueueConsumerServerOptions options,
    string function,
    IHandlerExecutionCoordinator handlerExecutionCoordinator,
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

        handlerExecutionCoordinator.RegisterAsyncResultCallback(_connection.Id, async (jobHandle, status) =>
        {
            await SendResult(jobHandle, status).ConfigureAwait(false);
        });
        
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
                
                await _connection.SendPacket(RequestFactory.CanDo(function), cancellationToken).ConfigureAwait(false);

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
        
        var result = await handlerExecutionCoordinator.ArrangeExecution(_connection.Id, job, cancellationToken).ConfigureAwait(false);

        if (result is not null)
        {
            // Send the result right away when it completes synchronously
            await SendResult(job.JobHandle, result.Value).ConfigureAwait(false);
        }

        return true;
    }

    private async Task SendResult(string jobHandle, JobStatus jobStatus)
    {
        try
        {
            if (jobStatus == JobStatus.Success)
            {
                await _connection.SendPacket(RequestFactory.WorkComplete(jobHandle),
                    CancellationToken.None).ConfigureAwait(false);
            }
            else
            {
                await _connection.SendPacket(RequestFactory.WorkFail(jobHandle),
                    CancellationToken.None).ConfigureAwait(false);
            }
        }
        catch (SocketException e)
        {
            _logger.LogSocketError(options.ServerInfo.Hostname, options.ServerInfo.Port, e);
        }
    }

    public void Dispose()
    {
        _connection?.Dispose();
    }
}