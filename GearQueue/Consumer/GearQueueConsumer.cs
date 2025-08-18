using GearQueue.Consumer.Coordinators;
using GearQueue.Logging;
using GearQueue.Options;
using Microsoft.Extensions.Logging;

namespace GearQueue.Consumer;

public class GearQueueConsumer(
    GearQueueConsumerOptions options,
    IGearQueueHandlerExecutor handlerExecutor,
    Dictionary<string, Type> handlers,
    ILoggerFactory loggerFactory) : IGearQueueConsumer
{
    /// <summary>
    /// Starts consuming gearman jobs. The returned task will not complete until cancellation is request 
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <exception cref="ArgumentException">Unsupported concurrency strategy</exception>
    public async Task StartConsuming(CancellationToken cancellationToken)
    {
        foreach (var (_, type) in handlers)
        {
            if (!type.IsAssignableTo(typeof(IGearQueueHandler)))
            {
                throw new ApplicationException($"Handler {type.FullName} does not implement IGearQueueHandler");
            }
        }
        
        var logger = loggerFactory.CreateLogger<GearQueueConsumer>();
        
        var coordinators = new List<IHandlerExecutionCoordinator>();

        if (options.ConcurrencyStrategy == ConcurrencyStrategy.AcrossServers)
        {
            coordinators.Add(
                new AsynchronousHandlerExecutionCoordinator(handlerExecutor, handlers, options, loggerFactory));
        }

        var instances = options.Hosts
            .Select(serverOptions =>
            {
                logger.LogStartingConsumer(serverOptions.Host.Hostname,
                    serverOptions.Host.Port,
                    serverOptions.Connections, 
                    string.Join(',', handlers.Keys));
                
                IHandlerExecutionCoordinator? sharedCoordinator = null;

                switch (options.ConcurrencyStrategy)
                {
                    case ConcurrencyStrategy.AcrossServers:
                        // Use shared coordinator
                        sharedCoordinator = coordinators.Single();
                        break;
                    case ConcurrencyStrategy.PerServer:
                        // PerServer, all the connections to this server share the same coordinator
                        sharedCoordinator = new AsynchronousHandlerExecutionCoordinator(handlerExecutor, handlers, options, loggerFactory);
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
                                coordinator = new SynchronousHandlerExecutionCoordinator(handlerExecutor, handlers, loggerFactory);
                                break;
                            case null:
                                coordinator = new AsynchronousHandlerExecutionCoordinator(handlerExecutor, handlers, options, loggerFactory);
                                coordinators.Add(coordinator);
                                break;
                        }
                        
                        return new GearQueueConsumerInstance(serverOptions, handlers.Keys, coordinator, loggerFactory);
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