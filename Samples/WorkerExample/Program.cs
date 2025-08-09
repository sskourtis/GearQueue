using GearQueue.Consumer;
using GearQueue.Extensions.Microsoft.DependencyInjection;
using GearQueue.Network;
using WorkerExample;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddGearQueueConsumer<ExampleHandler>(new GearQueueConsumerOptions
{
    Servers =
    [
        new GearQueueConsumerServerOptions
        {
            ServerInfo = new ServerInfo
            {
                Hostname = "127.0.0.1",
                Port = 4731
            },
            Concurrency = 10,
            UsePreSleep = true
        },
        new GearQueueConsumerServerOptions
        {
            ServerInfo = new ServerInfo
            {
                Hostname = "127.0.0.1",
                Port = 4732
            },
            Concurrency = 10,
            UsePreSleep = true
        },
    ]
}, "test-function", ServiceLifetime.Singleton);

var host = builder.Build();
host.Run();