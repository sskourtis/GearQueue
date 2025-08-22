using System.Reflection;
using System.Text;
using GearQueue.Consumer;
using GearQueue.Extensions.Microsoft.DependencyInjection;
using GearQueue.Json;
using GearQueue.Producer;
using Microsoft.AspNetCore.Mvc;
using SampleUtils;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddGearQueue(g =>
{
    g.SetDefaultSerializer(new GearQueueJsonSerializer());
    
    g.AddProducer(builder.Configuration.GetConnectionString("Producer")!);
    g.AddNamedProducer("primary", builder.Configuration.GetConnectionString("ProducerA")!);
    g.AddNamedProducer("secondary", builder.Configuration.GetConnectionString("ProducerB")!);

    g.RegisterTypedProducerFromAssembly(Assembly.GetAssembly(typeof(Program))!);
    g.RegisterTypedProducerFromAssembly(Assembly.GetAssembly(typeof(JobContract))!);
});

builder.Services.AddSingleton<JobCounter>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapGet("/test", () => Encoding.UTF8.GetBytes($"Test {Guid.NewGuid()}") )
    .WithName("Test");

app.MapGet("/produce/{name}", async (IGearQueueProducerFactory factory, JobCounter counter, string name) =>
    {
        var producer = factory.Get(name);

        if (producer == null)
        {
            return Results.BadRequest($"Invalid name {name}");
        }
        
        var result = await producer.Produce("test-function", Encoding.UTF8.GetBytes($"Test {Guid.NewGuid()}"));

        if (result)
        {
            counter.Increment();
            
            return Results.Ok("Ok");
        }

        return Results.InternalServerError();
    })
    .WithName("Produce from factory");

app.MapGet("/produce", async (IGearQueueProducer<JobContract> producer, JobCounter counter) =>
    {
        var result = await producer.Produce(new JobContract { TestValue = $"Test {Guid.NewGuid():N}" });

        if (result)
        {
            counter.Increment();
            
            return Results.Ok("Ok");
        }

        return Results.InternalServerError();
    })
    .WithName("Produce");

app.MapGet("/produce-batch", async (IGearQueueProducer producer, JobCounter counter, [FromQuery] string? key) =>
    {
        var result = await producer.Produce("test-batch-function", 
            new JobContract
            {
                TestValue = $"Test {Guid.NewGuid():N}", 
            },
            new ProducerOptions
            {
                BatchKey = key,
            });

        if (result)
        {
            counter.Increment();
            
            return Results.Ok("Ok");
        }

        return Results.InternalServerError();
    })
    .WithName("Produce Batch");

app.MapGet("/produce-random", async (IGearQueueProducer producer, JobCounter counter) =>
    {
        var result = await producer.Produce(Random.Shared.Next(10) > 5 
            ? "test-function"
            : "test-secondary-function", Encoding.UTF8.GetBytes($"Test {Guid.NewGuid()}"));

        if (result)
        {
            counter.Increment();
            
            return Results.Ok("Ok");
        }

        return Results.InternalServerError();
    })
    .WithName("Produce Random");

app.MapGet("/status", (IGearQueueProducer producer, JobCounter counter) =>
    {
        return Task.FromResult(new
        {
            Coutner = counter.Get(),
        });
    })
    .WithName("Status");

app.Run();

public class JobCounter
{
    private int _counter = 0;

    public void Increment()
    {
        Interlocked.Increment(ref _counter);
    }

    public int Get()
    {
        return Volatile.Read(ref _counter);
    }
}