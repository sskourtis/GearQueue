using GearQueue.Consumer.Executor;
using GearQueue.Consumer.Pipeline;
using GearQueue.Logging;
using GearQueue.Metrics;
using GearQueue.Options;
using Microsoft.Extensions.Logging;

namespace GearQueue.Consumer;

public class Consumer(
    ConsumerOptions options,
    ConsumerPipeline consumerPipeline,
    Dictionary<string, HandlerOptions> handlers,
    ILoggerFactory loggerFactory,
    IMetricsCollector? metricsCollector = null) : IConsumer
{
    /// <summary>
    /// Starts consuming gearman jobs. The returned task will not complete until cancellation is request 
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <exception cref="ArgumentException">Unsupported concurrency strategy</exception>
    public async Task Start(CancellationToken cancellationToken)
    {
        foreach (var (_, handlerOptions) in handlers)
        {
            if (!handlerOptions.Type.IsAssignableTo(typeof(IHandler)))
            {
                throw new ApplicationException($"Handler {handlerOptions.Type.FullName} does not implement IGearQueueHandler");
            }
        }

        var logger = loggerFactory.CreateLogger<Consumer>();
        
        var jobExecutors = new List<IJobExecutor>();
        JobManager? globalManager = null;

        if (options.ConcurrencyStrategy == ConcurrencyStrategy.AcrossServers &&
            (options.Hosts.Count > 1 || options.MaxConcurrency > 1))
        {
            logger.LogInformation("Using shared job manager with asynchronous job executor");
            var executor = new AsynchronousJobExecutor(options, consumerPipeline, loggerFactory, metricsCollector);
            jobExecutors.Add(executor);
            
            globalManager = new JobManager(executor, loggerFactory, handlers, metricsCollector);
        }

        var instances = options.Hosts
            .Select(serverOptions =>
            {
                logger.LogStartingConsumer(serverOptions.Host.Hostname,
                    serverOptions.Host.Port,
                    serverOptions.Connections, 
                    string.Join(',', handlers.Keys));
                
                JobManager? sharedJobManager = null;

                switch (options.ConcurrencyStrategy)
                {
                    case ConcurrencyStrategy.AcrossServers when globalManager is not null:
                        // Use shared coordinator
                        sharedJobManager = globalManager;
                        break;
                    case ConcurrencyStrategy.PerServer when serverOptions.Connections > 1:
                        // PerServer, all the connections to this server share the same coordinator
                        var executor = new AsynchronousJobExecutor(options, consumerPipeline, loggerFactory, metricsCollector);
                        jobExecutors.Add(executor);
                        
                        sharedJobManager = new JobManager(executor, loggerFactory, handlers, metricsCollector);
                        
                        logger.LogInformation("Using shared job manager for {Host}:{Port} with asynchronous job executor", 
                            serverOptions.Host.Hostname, 
                            serverOptions.Host.Port);;
                        break;
                    default:
                        // leave it null and handle it per connection
                        break;
                }

                return Enumerable.Range(0, serverOptions.Connections)
                    .Select(index =>
                    {
                        var jobManager = sharedJobManager;

                        switch (jobManager)
                        {
                            case null when options.MaxConcurrency == 1:
                                logger.LogInformation("Using dedicated job manager for {ConnectionNumber} of {Host}:{Port} with synchronous job executor",
                                    index,
                                    serverOptions.Host.Hostname, 
                                    serverOptions.Host.Port);
                                jobManager =  new JobManager(
                                    new SynchronousJobExecutor(consumerPipeline, loggerFactory, metricsCollector),
                                    loggerFactory, 
                                    handlers,
                                    metricsCollector);
                                break;
                            case null:
                                logger.LogInformation("Using dedicated job manager for {ConnectionNumber} of {Host}:{Port} with asynchronous job executor",
                                    index,
                                    serverOptions.Host.Hostname, 
                                    serverOptions.Host.Port);
                                var executor = new AsynchronousJobExecutor(options, consumerPipeline, loggerFactory, metricsCollector);
                                jobExecutors.Add(executor);
                                jobManager =  new JobManager(executor, loggerFactory, handlers, metricsCollector);
                                break;
                        }
                        
                        var connection = new ConsumerConnection(serverOptions, handlers.Keys, jobManager, loggerFactory);

                        connection.RegisterResultCallback();
                        
                        return connection;
                    });
            })
            .SelectMany(x => x);

        var instanceTasks = instances.Select(i => i.Start(cancellationToken));

        await Task.WhenAll(instanceTasks).ConfigureAwait(false);

        if (jobExecutors.Count != 0)
        {
            // Wait for all executions to complete
            await Task.WhenAll(jobExecutors.Select(c => c.WaitAllExecutions()))
                .ConfigureAwait(false);
        }
    }
}