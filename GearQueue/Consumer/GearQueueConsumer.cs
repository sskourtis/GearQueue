using GearQueue.Consumer.Coordinators;
using GearQueue.Logging;
using GearQueue.Options;
using Microsoft.Extensions.Logging;

namespace GearQueue.Consumer;

public class GearQueueConsumer(
    GearQueueConsumerOptions options,
    IGearQueueHandlerExecutor handlerExecutor,
    Type handlerType,
    ILoggerFactory loggerFactory) : IGearQueueConsumer
{
    /// <summary>
    /// Starts consuming gearman jobs. The returned task will not complete until cancellation is request 
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <exception cref="ArgumentException">Unsupported concurrency strategy</exception>
    public async Task StartConsuming(CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger<GearQueueConsumer>();
        
        var coordinators = new List<IHandlerExecutionCoordinator>();

        if (options.ConcurrencyStrategy == ConcurrencyStrategy.AcrossServers)
        {
            coordinators.Add(
                new AsynchronousHandlerExecutionCoordinator(handlerExecutor, handlerType, options, loggerFactory));
        }

        var instances = options.Servers
            .Select(serverOptions =>
            {
                logger.LogStartingConsumer(serverOptions.ServerInfo.Hostname,
                    serverOptions.ServerInfo.Port,
                    serverOptions.Connections,
                    options.Function);
                
                IHandlerExecutionCoordinator? sharedCoordinator = null;

                switch (options.ConcurrencyStrategy)
                {
                    case ConcurrencyStrategy.AcrossServers:
                        // Use shared coordinator
                        sharedCoordinator = coordinators.Single();
                        break;
                    case ConcurrencyStrategy.PerServer:
                        // PerServer, all the connections to this server share the same coordinator
                        sharedCoordinator = new AsynchronousHandlerExecutionCoordinator(handlerExecutor, handlerType, options, loggerFactory);
                        coordinators.Add(sharedCoordinator);
                        break;
                    case ConcurrencyStrategy.PerConnection:
                        // leave it null and handle it per connection
                        break;
                    default:
                        throw new ArgumentException("Unsupported concurrency strategy");
                }

                return Enumerable.Repeat(0, serverOptions.Connections)
                    .Select(_ =>
                    {
                        var coordinator = sharedCoordinator;

                        switch (coordinator)
                        {
                            case null when options.MaxConcurrency == 1:
                                coordinator = new SynchronousHandlerExecutionCoordinator(handlerExecutor, handlerType, loggerFactory);
                                break;
                            case null:
                                coordinator = new AsynchronousHandlerExecutionCoordinator(handlerExecutor, handlerType, options, loggerFactory);
                                coordinators.Add(coordinator);
                                break;
                        }
                        
                        return new GearQueueConsumerInstance(serverOptions, options.Function, coordinator, loggerFactory);
                    });
            })
            .SelectMany(x => x);

        var instanceTasks = instances.Select(i => i.Start(cancellationToken));

        await Task.WhenAll(instanceTasks).ConfigureAwait(false);

        if (coordinators.Count != 0)
        {
            // Wait for all executions to complete
            await Task.WhenAll(coordinators.Select(c => c.WaitAllExecutions()))
                .ConfigureAwait(false);
        }
    }
}