using System.Net.Sockets;
using GearQueue.Consumer;
using GearQueue.Logging;
using GearQueue.Network;
using GearQueue.Options;
using GearQueue.Protocol;
using GearQueue.Protocol.Request;
using GearQueue.Protocol.Response;
using Microsoft.Extensions.Logging;

namespace GearQueue.BatchConsumer;

internal class GearQueueBatchConsumerInstance(
    GearQueueConsumerHostsOptions options,
    IBatchHandlerExecutionCoordinator batchHandlerExecutionCoordinator,
    string function,
    ILoggerFactory loggerFactory)
{
    private readonly ILogger<GearQueueBatchConsumerInstance> _logger = loggerFactory.CreateLogger<GearQueueBatchConsumerInstance>();
    private readonly Connection _connection = new(loggerFactory, options.Host);

    internal void RegisterResultCallback()
    {
        batchHandlerExecutionCoordinator.RegisterAsyncResultCallback(_connection.Id, async (jobHandle, status) =>
        {
            if (status == JobStatus.Success)
            {
                await _connection.SendPacket(RequestFactory.WorkComplete(jobHandle),
                    CancellationToken.None).ConfigureAwait(false);
            }
            else
            {
                await _connection.SendPacket(RequestFactory.WorkFail(jobHandle),
                    CancellationToken.None).ConfigureAwait(false);
            }
        });
    }
    
    internal async Task Start(CancellationToken cancellationToken = default)
    {
        await Task.Yield();
        
        await Connect(cancellationToken).ConfigureAwait(false);
        
        while(!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var job = await CheckForJob(cancellationToken).ConfigureAwait(false);
                
                var batchTimeout = await batchHandlerExecutionCoordinator.Notify(_connection.Id, job, cancellationToken)
                    .ConfigureAwait(false);

                if (job is not null)
                {
                    continue;
                }

                if (!options.UsePreSleep)
                {
                    /*
                     * Polling is also offered as an option instead of using the PRE_SLEEP packet
                     */
                    await Task.Delay(options.PollingDelay <= batchTimeout ? options.PollingDelay : batchTimeout,
                        cancellationToken).ConfigureAwait(false);
                    continue;
                }
                
                await _connection.SendPacket(RequestFactory.PreSleep(), cancellationToken).ConfigureAwait(false);
                
                using var cancellationTokenSource = new CancellationTokenSource(batchTimeout);
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationTokenSource.Token, cancellationToken);
                
                var noopResponse = await _connection.GetPacket(cancellationTokenSource.Token).ConfigureAwait(false);

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
                await _connection.Connect(cancellationToken).ConfigureAwait(false);
                
                await _connection.SendPacket(RequestFactory.CanDo(function), cancellationToken).ConfigureAwait(false);

                break;
            }
            catch (SocketException e)
            {
                _logger.LogSocketError(options.Host.Hostname,
                    options.Host.Port, 
                    options.ReconnectTimeout,
                    e);
                await Task.Delay(options.ReconnectTimeout, cancellationToken).ConfigureAwait(false);
            }
        }
    }
    
    private static readonly RequestPacket GrabJobPacket = RequestFactory.GrabJob();
    
    private async Task<JobAssign?> CheckForJob(CancellationToken cancellationToken = default)
    {
        await _connection.SendPacket(GrabJobPacket, cancellationToken).ConfigureAwait(false);
			
        var response = await _connection.GetPacket(cancellationToken).ConfigureAwait(false);

        if (response is null)
        {
            return null;
        }

        switch (response.Value.Type)
        {
            case PacketType.JobAssign:
                return JobAssign.Create(response.Value.Data);
            case PacketType.JobAssignUniq:
                return JobAssignUniq.Create(response.Value.Data);
            case PacketType.JobAssignAll:
                return JobAssignAll.Create(response.Value.Data);
            default:
                return null;
        }
    }
}