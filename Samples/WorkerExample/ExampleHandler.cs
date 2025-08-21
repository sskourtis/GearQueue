using System.Text;
using GearQueue.Consumer;

namespace WorkerExample;

public class ExampleHandler(ILogger<ExampleHandler> logger) : IGearQueueHandler
{
    public async Task<JobResult> Consume(JobContext job)
    {
        logger.LogInformation(message: Encoding.UTF8.GetString(job.Data));
            
        return JobResult.Success;
    }
}