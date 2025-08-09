using GearQueue.Extensions.Microsoft.DependencyInjection;
using WorkerExample;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddGearQueueConsumer<ExampleHandler>(
    builder.Configuration.GetSection("GearQueue:TestFunction"));

builder.Services.AddGearQueueConsumer<ExampleBatchHandler>(
    builder.Configuration.GetSection("GearQueue:TestBatchFunction"));

var host = builder.Build();
host.Run();