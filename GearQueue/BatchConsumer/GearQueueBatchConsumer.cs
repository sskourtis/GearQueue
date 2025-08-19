using GearQueue.Consumer;
using GearQueue.Logging;
using GearQueue.Options;
using Microsoft.Extensions.Logging;

namespace GearQueue.BatchConsumer;

public class GearQueueBatchConsumer(
    GearQueueConsumerOptions options,
    IGearQueueHandlerExecutor handlerExecutor,
    Dictionary<string, Type> handlers,
    ILoggerFactory loggerFactory) : IGearQueueConsumer
{
    /// <summary>
    /// Starts consuming gearman jobs. The returned task will not complete until cancellation is request 
    /// </summary>
    /// <param name="cancellationToken"></param>
    public async Task StartConsuming(CancellationToken cancellationToken)
    {
        foreach (var (_, type) in handlers)
        {
            if (!type.IsAssignableTo(typeof(IGearQueueHandler)))
            {
                throw new ApplicationException($"Handler {type.FullName} does not implement IGearQueueBatchHandler");
            }
        }
        
        var logger = loggerFactory.CreateLogger<GearQueueConsumer>();

        var batchCoordinator = new BatchHandlerExecutionCoordinator(loggerFactory, handlerExecutor, options, handlers.First().Value);
        
        var instances = options.Hosts
            .Select(serverOptions =>
            {
                logger.LogStartingBatchConsumer(
                    serverOptions.Connections,
                    serverOptions.Host.Hostname,
                    serverOptions.Host.Port,
                    string.Join(',', handlers.Keys));

                return Enumerable.Repeat(0, serverOptions.Connections)
                    .Select(_ =>
                    {
                        var instance =
                            new GearQueueBatchConsumerInstance(serverOptions, batchCoordinator, handlers.First().Key,
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