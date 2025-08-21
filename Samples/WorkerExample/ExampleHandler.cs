using System.Text;
using GearQueue.Consumer;

namespace WorkerExample;

public class ExampleHandler(ILogger<ExampleHandler> logger) : IGearQueueHandler
{
    public async Task<JobResult> Consume(JobContext context)
    {
        logger.LogInformation(message: Encoding.UTF8.GetString(context.Data));
            
        return JobResult.Success;
    }
}