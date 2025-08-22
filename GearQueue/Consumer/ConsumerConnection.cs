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
    ConsumerHostsOptions options,
    ICollection<string> functions,
    AbstractHandlerExecutionCoordinator abstractHandlerExecutionCoordinator,
    ILoggerFactory loggerFactory) : IDisposable
{
    private readonly ILogger<IConsumer> _logger = loggerFactory.CreateLogger<IConsumer>();
    private readonly Connection _connection = new(loggerFactory, options.Host);
    
    internal void RegisterResultCallback()
    {
        abstractHandlerExecutionCoordinator.RegisterAsyncResultCallback(_connection.Id, async (jobHandle, status) =>
        {
            await SendResult(jobHandle, status).ConfigureAwait(false);
        });
    }
    
    internal async Task Start(CancellationToken cancellationToken = default)
    {
        await Task.Yield();
        
        await Connect(cancellationToken).ConfigureAwait(false);
        
        ResponsePacket? outOfOrderResponsePacket = null;
        
        while(!cancellationToken.IsCancellationRequested)
        {
            try
            {
                // Make sure to first handle any out of order pending response packet
                var job = outOfOrderResponsePacket?.Type switch
                {
                    PacketType.JobAssign => JobAssign.Create(outOfOrderResponsePacket.Value.Data),
                    PacketType.JobAssignUniq => JobAssign.CreateUniq(outOfOrderResponsePacket.Value.Data),
                    PacketType.JobAssignAll => JobAssign.CreateAll(outOfOrderResponsePacket.Value.Data),
                    _ => await CheckForJob(cancellationToken).ConfigureAwait(false),
                };

                outOfOrderResponsePacket = null;
                
                var executionResult = await abstractHandlerExecutionCoordinator.ArrangeExecution(_connection.Id, job, cancellationToken).ConfigureAwait(false);
                    
                if (job != null)
                {
                    if (executionResult.ResultingStatus.HasValue)
                    {
                        // Send the result right away when it completes synchronously
                        await SendResult(job.JobHandle, executionResult.ResultingStatus.Value).ConfigureAwait(false);   
                    }
                    
                    continue;
                }

                if (!options.UsePreSleep || 
                    // Avoid using presleep if we have to wake up less than 200ms from now.
                    // This is because the "GetPacket" after presleep is not stable if the cancellation token is extremely short.
                    // The instability is that we won't read the response packets after presleep in the correct order.
                    (executionResult.MaximumSleepDelay.HasValue && executionResult.MaximumSleepDelay <= TimeSpan.FromMilliseconds(200)))
                {
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

                    noopResponse = await _connection.GetPacket(linkedCts.Token).ConfigureAwait(false);
                }
                else
                {
                    noopResponse = await _connection.GetPacket(cancellationToken).ConfigureAwait(false);    
                }

                if (noopResponse is not null && noopResponse.Value.Type is not PacketType.Noop)
                {
                    _logger.LogUnexpectedResponseType(noopResponse?.Type, PacketType.Noop);
                    
                    // If we receive a packet out of order after PRE_SLEEP, we need to store it and check if it contains a new job.
                    // This should never happen under normal conditions. The mechanism is in place mostly for peace of mind. 
                    outOfOrderResponsePacket = noopResponse;
                }
            }
            catch (SocketException e)
            {
                _logger.LogSocketError(options.Host.Hostname, options.Host.Port, e);;
                // Reconnect to gearman server
                await Connect(cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private async Task SendResult(string jobHandle, JobResult jobResult)
    {
        try
        {
            if (jobResult == JobResult.Success)
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

    private async Task<JobAssign?> CheckForJob(CancellationToken cancellationToken = default)
    {
        await _connection.SendPacket(abstractHandlerExecutionCoordinator.GrabJobPacket, cancellationToken).ConfigureAwait(false);
			
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
                return JobAssign.CreateUniq(response.Value.Data);
            case PacketType.JobAssignAll:
                return JobAssign.CreateAll(response.Value.Data);
            default:
                return null;
        }
    }

    public void Dispose()
    {
        _connection.Dispose();
    }
}