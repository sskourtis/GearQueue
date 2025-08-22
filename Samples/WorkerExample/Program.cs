using GearQueue.Extensions.Microsoft.DependencyInjection;
using GearQueue.Serialization;
using WorkerExample.Handlers;
using WorkerExample.Middlewares;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddGearQueue(g =>
{
    g.SetDefaultSerializer(new GearQueueJsonJobSerializer());
    
    g.AddConsumer(builder.Configuration.GetConnectionString("Consumer"))
        .SetHandler<ExampleHandler>("test-function", ServiceLifetime.Singleton)
        .SetPipeline(b =>
        {
            b.Use<ThroughputTrackingMiddleware>();
        });

    g.AddConsumer(builder.Configuration.GetConnectionString("BatchConsumer"))
        .SetHandler<ExampleBatchHandler>("test-batch-function");
});

var host = builder.Build();
host.Run();