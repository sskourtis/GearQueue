# GearQueue

GearQueue is a high-performance C# client library for the [Gearman Job Server](http://gearman.org/).  
It provides fully asynchronous APIs, efficient connection pooling, and flexible worker configuration for building scalable distributed systems.

[Documentation](https://github.com/StavrosSkourtis/GearQueue/wiki)

---

## HighLights

- Support for multiple Gearman hosts with health tracking
- High-performance connection pooling for job submissions
- Fully asynchronous design (no synchronous job support)
- Built-in JSON serialization, with the option to define custom serializers
- Multiple function handler registrations per worker
- Configurable concurrency settings per worker
- Reusable middleware support for workers
- Metrics collection support (Prometheus integration available as a separate library)
- Batch job processing, configurable by batch size and timeout
- Key-based batching support

---

##  Installation

Install from NuGet:

```bash
dotnet add package GearQueue
dotnet add package GearQueue.DependencyInjection
```

## Quick Start

**Define the Job contact**

```csharp
[GearQueueJob("test-function")]
public class JobContract
{
    public string TestField { get; set; }
}
```

**Create a Job Handler**

```csharp
public class SampleHandler : AbstractHandler<JobContract>
{
    public override Task<JobResult> Handle(JobContract job, JobContext context)
    {       
        // process the job
        
        return Task.FromResult(JobResult.Success);
    }
}
```

**Register GearQueue in dependency injection**

```csharp
builder.Services.AddGearQueue(g =>
{
    g.SetDefaultSerializer(new GearQueueJsonJobSerializer());
    
    g.AddProducer("Hosts=localhost:4730; MaxConnections=100");
    
    g.AddWorker("Hosts=localhost:4730; MaxConcurrency=10; Connections=1")
        .SetHandler<SampleHandler>("test-function", ServiceLifetime.Singleton);
});
```

**Submit jobs**
```csharp
// Get default producer from IoC
var producer = serviceProvider.GetRequriedService<IProducer>();

await producer.Produce(new JobContract
{
    TestField = "dummy data"
});
```
