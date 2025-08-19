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

internal class ConsumerConnection(
    GearQueueConsumerHostsOptions options,
    ICollection<string> functions,
    IHandlerExecutionCoordinator handlerExecutionCoordinator,
    ILoggerFactory loggerFactory) : IDisposable
{
    private readonly ILogger<IGearQueueConsumer> _logger = loggerFactory.CreateLogger<IGearQueueConsumer>();

    private readonly Connection _connection = new(loggerFactory, options.Host);
    
    internal void RegisterResultCallback()
    {
        handlerExecutionCoordinator.RegisterAsyncResultCallback(_connection.Id, async (jobHandle, status) =>
        {
            await SendResult(jobHandle, status).ConfigureAwait(false);
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

                var executionResult = await handlerExecutionCoordinator.ArrangeExecution(_connection.Id, job, cancellationToken).ConfigureAwait(false);
                    
                if (job != null && executionResult.ResultingStatus.HasValue)
                {
                    // Send the result right away when it completes synchronously
                    await SendResult(job.JobHandle, executionResult.ResultingStatus.Value).ConfigureAwait(false);
                    
                    continue;
                }

                if (!options.UsePreSleep)
                {
                    /*
                     * Polling is also offered as an option instead of using the PRE_SLEEP packet
                     */
                    await Task.Delay(executionResult.MaximumSleepDelay.HasValue && executionResult.MaximumSleepDelay.Value < options.PollingDelay 
                            ? executionResult.MaximumSleepDelay.Value 
                            : options.PollingDelay,
                        cancellationToken).ConfigureAwait(false);
                    continue;
                }
                
                await _connection.SendPacket(RequestFactory.PreSleep(), cancellationToken).ConfigureAwait(false);

                ResponsePacket? noopResponse;

                if (executionResult.MaximumSleepDelay.HasValue)
                {
                    using var cancellationTokenSource = new CancellationTokenSource(executionResult.MaximumSleepDelay.Value);
                    using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationTokenSource.Token, cancellationToken);
                
                    noopResponse = await _connection.GetPacket(cancellationTokenSource.Token).ConfigureAwait(false);
                }
                else
                {
                    noopResponse = await _connection.GetPacket(cancellationToken).ConfigureAwait(false);    
                }

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
            _logger.LogSocketError(_connection.Options.Hostname, _connection.Options.Port, e);
        }
    }

    private async Task Connect(CancellationToken cancellationToken)
    {
        while (true)
        {
            try
            {
                await _connection.Connect(cancellationToken).ConfigureAwait(false);

                foreach (var function in functions)
                {
                    await _connection.SendPacket(RequestFactory.CanDo(function), cancellationToken).ConfigureAwait(false);    
                }

                break;
            }
            catch (SocketException e)
            {
                _logger.LogSocketError(_connection.Options.Hostname,
                    _connection.Options.Port, 
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

    public void Dispose()
    {
        _connection.Dispose();
    }
}