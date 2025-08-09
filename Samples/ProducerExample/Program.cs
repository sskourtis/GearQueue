using System.Text;
using GearQueue.Extensions.Microsoft.DependencyInjection;
using GearQueue.Producer;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddGearQueueProducer(builder.Configuration);

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

app.MapGet("/produce", async (GearQueueProducer producer, JobCounter counter) =>
    {
        var result = await producer.Produce("test-function", Encoding.UTF8.GetBytes($"Test {Guid.NewGuid()}"));

        if (result)
        {
            counter.Increment();
            
            return Results.Ok("Ok");
        }

        return Results.InternalServerError();
    })
    .WithName("Produce");

app.MapGet("/status", (GearQueueProducer producer, JobCounter counter) =>
    {
        var metrics = producer.Metrics;
        
        return Task.FromResult(new
        {
            Coutner = counter.Get(),
            metrics.ExpiredConnections,
            metrics.CleanedConnections,
            metrics.CleanupErrors,
            metrics.CleanupRuns,
            metrics.ConnectionErrors,
            metrics.ConnectionsCreated,
            metrics.ConnectionsDiscarded,
            metrics.ConnectionsReturned,
            metrics.ConnectionsReused
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