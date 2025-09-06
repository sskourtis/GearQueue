using System.Reflection;
using System.Text;
using GearQueue.DependencyInjection;
using GearQueue.Producer;
using GearQueue.PrometheusNet;
using GearQueue.Serialization;
using Microsoft.AspNetCore.Mvc;
using Prometheus;
using SampleUtils;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddGearQueue(g =>
{
    g.SetDefaultSerializer(new GearQueueJsonJobSerializer());
    g.SetMetricsCollector<PrometheusMetricsCollector>();
    
    g.AddProducer(builder.Configuration.GetConnectionString("Producer")!);
    g.AddNamedProducer("primary", builder.Configuration.GetConnectionString("ProducerA")!);
    g.AddNamedProducer("secondary", builder.Configuration.GetConnectionString("ProducerB")!);

    g.RegisterTypedProducerFromAssembly(Assembly.GetAssembly(typeof(Program))!);
    g.RegisterTypedProducerFromAssembly(Assembly.GetAssembly(typeof(JobContract))!);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseWhen(
    context => !context.Request.Path.StartsWithSegments(other: "/health"),
    config =>
    {
        config.UseHttpMetrics();
    });

app.MapMetrics();


app.MapGet("/produce/{name}", async (IProducerFactory factory, string name) =>
    {
        var producer = factory.Get(name);

        if (producer == null)
        {
            return Results.BadRequest($"Invalid name {name}");
        }
        
        var result = await producer.Produce("test-function", Encoding.UTF8.GetBytes($"Test {Guid.NewGuid()}"));

        return result ? Results.Ok("Ok") : Results.InternalServerError();
    })
    .WithName("Produce from factory");

app.MapGet("/produce", async (IProducer<JobContract> producer) =>
    {
        var result = await producer.Produce(new JobContract { TestValue = $"Test {Guid.NewGuid():N}" });

        return result ? Results.Ok("Ok") : Results.InternalServerError();
    })
    .WithName("Produce");

app.MapGet("/produce-batch", async (IProducer producer, [FromQuery] string? key) =>
    {
        var result = await producer.Produce("test-batch-function", 
            new JobContract
            {
                TestValue = $"Test {Guid.NewGuid():N}", 
            },
            new JobOptions
            {
                BatchKey = key,
            });

        return result ? Results.Ok("Ok") : Results.InternalServerError();
    })
    .WithName("Produce Batch");

app.MapGet("/produce-random", async (IProducer producer) =>
    {
        var result = await producer.Produce(Random.Shared.Next(0, 3) switch
        {
            0 => "test-function",
            1 => "test-function-2",
            _ => "test-batch-function",
        }, new JobContract { TestValue = $"Test {Guid.NewGuid():N}" });

        return result ? Results.Ok("Ok") : Results.InternalServerError();
    })
    .WithName("Produce Random");

app.Run();
