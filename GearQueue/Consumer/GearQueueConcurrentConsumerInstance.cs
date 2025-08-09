using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.Sockets;
using GearQueue.Logging;
using GearQueue.Network;
using GearQueue.Protocol;
using GearQueue.Protocol.Request;
using GearQueue.Protocol.Response;
using Microsoft.Extensions.Logging;

namespace GearQueue.Consumer;

internal class GearQueueConcurrentConsumerInstance(
    GearQueueConsumerServerOptions options,
    IReadOnlyDictionary<string, Type> functions,
    IGearQueueHandlerExecutor handlerExecutor,
    ILoggerFactory loggerFactory
    ) : IDisposable
{
    private readonly ILogger<GearQueueConsumerInstance> _logger = loggerFactory.CreateLogger<GearQueueConsumerInstance>();
    private readonly Connection _connection = new(loggerFactory, options.ConnectionTimeout, options.SendTimeout, options.ReceiveTimeout);

    private readonly ConcurrentDictionary<string, TaskCompletionSource<bool>> _activeJobs = new();
    
    private readonly SemaphoreSlim _connectionSemaphore = new(1, 1);
    private readonly SemaphoreSlim _handlerSemaphore = new(options.Concurrency, options.Concurrency);
    
    internal async Task Start(CancellationToken cancellationToken = default)
    {
        await Task.Yield();
        await Connect(cancellationToken);
        
        while(!cancellationToken.IsCancellationRequested)
        {
            try
            {
                if (await CheckForJob(cancellationToken))
                {
                    continue;
                }

                /*
                 * Presleep is only available when the consumer is fully idle
                 */
                if (_activeJobs.IsEmpty && options.UsePreSleep)
                {
                    await _connection.SendPacket(RequestFactory.PreSleep(), cancellationToken);
                    
                    var noopResponse = await _connection.GetPacket(cancellationToken);

                    if (noopResponse is null || noopResponse.Value.Type != PacketType.Noop)
                    {
                        _logger.LogUnexpectedResponseType(noopResponse?.Type, PacketType.Noop);
                    } 

                    continue;
                }
                
                /*
                 * Polling is offered as an option instead of using the PRE_SLEEP packet
                 * because it seemed to hang after some hours and not wake up
                 */
                await Task.Delay(options.PollingDelay, cancellationToken);
            }
            catch (SocketException)
            {
                // Reconnect to gearman server
                await Connect(cancellationToken);
            }
        }

        await Task.WhenAll(_activeJobs.Values.Select(t => t.Task));
    }
    
    private async Task Connect(CancellationToken cancellationToken)
    {
        while (true)
        {
            try
            {
                await _connectionSemaphore.WaitAsync(cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();

                await _connection.Connect(options.ServerInfo.Hostname, options.ServerInfo.Port, cancellationToken);

                // Register functions again
                foreach (var (function, _) in functions)
                {
                    await _connection.SendPacket(RequestFactory.CanDo(function), cancellationToken);
                }

                break;
            }
            catch (SocketException e)
            {
                _logger.LogSocketError(options.ServerInfo.Hostname,
                    options.ServerInfo.Port,
                    options.ReconnectTimeout,
                    e);
                await Task.Delay(options.ReconnectTimeout, cancellationToken);
            }
            finally
            {
                _connectionSemaphore.Release();
            }
        }
    }
    
    private async Task<bool> CheckForJob(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        
        await _connection.SendPacket(RequestFactory.GrabJob(), cancellationToken);
        /*_logger.LogInformation("Sent grab job in {ElapsedMilliseconds}ms", stopwatch.ElapsedMilliseconds);*/
        stopwatch.Restart();

        var response = await _connection.GetPacket(cancellationToken);
        /*_logger.LogInformation("Got response {Type} in {ElapsedMilliseconds}ms",
            response?.Type.ToString() ?? "-",
            stopwatch.ElapsedMilliseconds);*/
        
        if (response is null)
        {
            return false;
        }

        if (response.Value.Type != PacketType.JobAssign)
            return false;

        var job = JobAssign.Create(response.Value.Data);
        
        await _handlerSemaphore.WaitAsync(cancellationToken);
        
        cancellationToken.ThrowIfCancellationRequested();
        
        var taskCompletionSource = new TaskCompletionSource<bool>();

        _activeJobs[job.JobHandle] = taskCompletionSource;

        _ = Task.Run(() => CallHandler(job, taskCompletionSource, cancellationToken), CancellationToken.None);
        
        return true;
    }

    private async Task CallHandler(JobAssign job, TaskCompletionSource<bool> taskCompletionSource, CancellationToken cancellationToken = default)
    {
        var status = JobStatus.Failure;
        
        try
        {
            if (!functions.TryGetValue(job.FunctionName, out var handlerType))
            {
                _logger.LogMissingHandlerType(job.FunctionName);
            }
            else
            {
                var jobContext = new JobContext(job, cancellationToken);

                var (success, jobStatus) = await handlerExecutor.TryExecute(handlerType, jobContext);

                if (!success)
                {
                    _logger.LogHandlerTypeCreationFailure(handlerType, job.FunctionName);
                }
                
                status = jobStatus ?? JobStatus.Failure;
            }
        }
        catch (Exception e)
        {
            _logger.LogConsumerException(e);
        }
        finally
        {
            _handlerSemaphore.Release();
        }

        try
        {
            await SendJobResult(status, job.JobHandle);
        }
        finally
        {
            taskCompletionSource.SetResult(true);
            _activeJobs.Remove(job.JobHandle, out _);
        }
    }

    private async Task SendJobResult(JobStatus jobStatus, string jobHandle)
    {
        await _connection.SendPacket(
            jobStatus == JobStatus.Success
                ? RequestFactory.WorkComplete(jobHandle, "OK"u8.ToArray())
                : RequestFactory.WorkFail(jobHandle), 
            CancellationToken.None);
    }

    public void Dispose()
    {
        _connectionSemaphore.Dispose();
        _handlerSemaphore.Dispose();
        _connection?.Dispose();
    }
}