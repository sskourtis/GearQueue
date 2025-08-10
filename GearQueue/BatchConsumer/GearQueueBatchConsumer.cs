using GearQueue.Consumer;
using GearQueue.Logging;
using GearQueue.Options;
using Microsoft.Extensions.Logging;

namespace GearQueue.BatchConsumer;

public class GearQueueBatchConsumer(
    GearQueueConsumerOptions options,
    IGearQueueBatchHandlerExecutor handlerExecutor,
    Type handlerType,
    ILoggerFactory loggerFactory) : IGearQueueConsumer
{
    /// <summary>
    /// Starts consuming gearman jobs. The returned task will not complete until cancellation is request 
    /// </summary>
    /// <param name="cancellationToken"></param>
    public async Task StartConsuming(CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger<GearQueueConsumer>();

        var batchCoordinator = new BatchHandlerExecutionCoordinator(loggerFactory, handlerExecutor, options, handlerType);
        
        var instances = options.Servers
            .Select(serverOptions =>
            {
                logger.LogStartingBatchConsumer(
                    serverOptions.Connections,
                    serverOptions.ServerInfo.Hostname,
                    serverOptions.ServerInfo.Port,
                    options.Function);

                return Enumerable.Repeat(0, serverOptions.Connections)
                    .Select(_ =>
                    {
                        var instance =
                            new GearQueueBatchConsumerInstance(serverOptions, batchCoordinator, options.Function,
                                loggerFactory);

                        instance.RegisterResultCallback();

                        return instance;
                    });
            }).SelectMany(instance => instance);

        var instanceTasks = instances.Select(i => i.Start(cancellationToken));

        await Task.WhenAll(instanceTasks).ConfigureAwait(false);
        
        await batchCoordinator.WaitAllHandlers();
    }
}