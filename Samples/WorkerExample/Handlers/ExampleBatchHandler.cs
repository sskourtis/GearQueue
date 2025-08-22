using GearQueue.Consumer;
using SampleUtils;

namespace WorkerExample.Handlers;

public class ExampleBatchHandler(ILogger<ExampleBatchHandler> logger) : AbstractBatchHandler<JobContract>
{
    public override Task<JobResult> Consume(IEnumerable<JobContract> jobs, JobContext context)
    {
        //logger.LogInformation("Consuming batch job {Job} for {Key}", count, context.BatchKey ?? "-");

        return Task.FromResult(JobResult.Success);
    }
}