using GearQueue.Extensions.Microsoft.DependencyInjection;
using GearQueue.Options;
using GearQueue.Serialization;
using WorkerExample.Handlers;
using WorkerExample.Middlewares;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddGearQueue(g =>
{
    g.SetDefaultSerializer(new GearQueueJsonJobSerializer());
    
    g.AddConsumer(builder.Configuration.GetConnectionString("Consumer"))
        .SetHandler<ExampleHandler>("test-function", ServiceLifetime.Singleton)
        .SetHandler<ExampleHandler>("test-function-2", ServiceLifetime.Singleton)
        .SetBatchHandler<ExampleBatchHandler>("test-batch-function", new BatchOptions
        {
            Size = 10,
            TimeLimit = TimeSpan.FromSeconds(5),
            ByKey = true
        })
        .SetPipeline(b =>
        {
            b.Use<ThroughputTrackingMiddleware>();
        });
});

var host = builder.Build();
host.Run();