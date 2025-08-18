using GearQueue.Extensions.Microsoft.DependencyInjection;
using WorkerExample;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddGearQueue(g =>
{
    g.AddConsumer(builder.Configuration.GetConnectionString("Consumer"))
        .SetHandler<ExampleHandler>("test-function");

    g.AddConsumer(builder.Configuration.GetConnectionString("BatchConsumer"))
        .SetHandler<ExampleBatchHandler>("test-batch-function");
});

var host = builder.Build();
host.Run();