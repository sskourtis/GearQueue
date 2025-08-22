using System.Text;
using GearQueue.Consumer;

namespace WorkerExample;

public class ExampleHandler(ILogger<ExampleHandler> logger) : GearQueueTypedHandler<string>
{
    public override async Task<JobResult> Consume(JobContext<string> context)
    {
        logger.LogInformation(message: Encoding.UTF8.GetString(context.Data));
            
        return JobResult.Success;
    }
}