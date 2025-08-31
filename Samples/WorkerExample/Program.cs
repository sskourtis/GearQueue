using System.Text;
using GearQueue.Extensions.Microsoft.DependencyInjection;
using GearQueue.Options;
using GearQueue.PrometheusNet;
using GearQueue.Serialization;
using Prometheus;
using WorkerExample.Handlers;
using WorkerExample.Middlewares;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddGearQueue(g =>
{
    g.SetDefaultSerializer(new GearQueueJsonJobSerializer());
    g.SetMetricsCollector<PrometheusMetricsCollector>();
    
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

var app = builder.Build();

app.UseWhen(
    context => !context.Request.Path.StartsWithSegments(other: "/health"),
    config =>
    {
        config.UseHttpMetrics();
    });

app.MapMetrics();

app.MapGet("/test", () => Encoding.UTF8.GetBytes($"Test {Guid.NewGuid()}") )
    .WithName("Test");

app.Run();