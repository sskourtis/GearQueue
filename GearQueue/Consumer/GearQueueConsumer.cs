using GearQueue.Logging;
using Microsoft.Extensions.Logging;

namespace GearQueue.Consumer;

public class GearQueueConsumer(
    GearQueueConsumerOptions options,
    IGearQueueHandlerExecutor handlerExecutor,
    IReadOnlyDictionary<string, Type> functions,
    ILoggerFactory loggerFactory)
{
    public async Task StartConsuming(CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger<GearQueueConsumer>();

        var instances = options.Servers
            .Select(serverOptions =>
            {
                logger.LogStaringConsumer(serverOptions.ServerInfo.Hostname,
                    serverOptions.ServerInfo.Port,
                    serverOptions.Concurrency,
                    functions.Keys);

                return Enumerable.Repeat(0, serverOptions.Concurrency)
                    .Select(_ =>
                        new GearQueueConsumerInstance(serverOptions, functions, handlerExecutor, loggerFactory));
            })
            .SelectMany(x => x);

        var instanceTasks = instances.Select(i => i.Start(cancellationToken));

        /*
        var instances = options.Servers
            .Select(serverOptions =>
            {
                logger.LogStaringConsumer(serverOptions.ServerInfo.Hostname,
                    serverOptions.ServerInfo.Port,
                    serverOptions.Concurrency,
                    functions.Keys);

                return new GearQueueConcurrentConsumerInstance(serverOptions, functions, handlerExecutor,
                    loggerFactory);
            });

        var instanceTasks = instances.Select(i => i.Start(cancellationToken));
        */

        await Task.WhenAll(instanceTasks).ConfigureAwait(false);
    }
}