using GearQueue.Consumer;
using SampleUtils;

namespace WorkerExample.Handlers;

public class ExampleHandler(ILogger<ExampleHandler> logger) : AbstractHandler<JobContract>
{
    public override Task<JobResult> Consume(JobContract job, JobContext context)
    {        
        //logger.LogInformation("Got Job {data}", job.TestValue);
            
        return Task.FromResult(JobResult.Success);
    }
}