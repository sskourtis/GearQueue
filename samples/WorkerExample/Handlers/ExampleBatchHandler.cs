using GearQueue.Worker;
using SampleUtils;

namespace WorkerExample.Handlers;

public class ExampleBatchHandler(ILogger<ExampleBatchHandler> logger) : AbstractBatchHandler<JobContract>
{
    public override Task<JobResult> Handle(IEnumerable<JobContract> jobs, JobContext context)
    {
        var jobList = jobs.ToList();
        
        logger.LogInformation("Consuming batch job {Size} for {Key}", jobList.Count, context.BatchKey ?? "-");

        foreach (var job in jobList)
        {
            logger.LogInformation("Got Job {data}", job.TestValue);
        }

        return Task.FromResult(JobResult.Success);
    }
}