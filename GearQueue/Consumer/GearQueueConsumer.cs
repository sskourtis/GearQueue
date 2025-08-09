using GearQueue.Logging;
using Microsoft.Extensions.Logging;

namespace GearQueue.Consumer;

public class GearQueueConsumer(
    GearQueueConsumerOptions options,
    IGearQueueHandlerExecutor handlerExecutor,
    IReadOnlyDictionary<string, Type> functions,
    ILoggerFactory loggerFactory)
{
    
    /// <summary>
    /// Starts consuming gearman jobs. The returned task will not complete until cancellation is request 
    /// </summary>
    /// <param name="cancellationToken"></param>
    public async Task StartConsuming(CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger<GearQueueConsumer>();

        var instances = options.Servers
            .Select(serverOptions =>
            {
                logger.LogStartingConsumer(serverOptions.ServerInfo.Hostname,
                    serverOptions.ServerInfo.Port,
                    serverOptions.Concurrency,
                    functions.Keys);

                return Enumerable.Repeat(0, serverOptions.Concurrency)
                    .Select(_ =>
                        new GearQueueConsumerInstance(serverOptions, functions, handlerExecutor, loggerFactory));
            })
            .SelectMany(x => x);

        var instanceTasks = instances.Select(i => i.Start(cancellationToken));

        await Task.WhenAll(instanceTasks).ConfigureAwait(false);
    }
}