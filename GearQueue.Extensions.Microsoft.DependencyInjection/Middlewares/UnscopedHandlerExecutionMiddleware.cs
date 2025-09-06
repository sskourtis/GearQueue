using GearQueue.Worker;
using GearQueue.Worker.Pipeline;
using Microsoft.Extensions.DependencyInjection;

namespace GearQueue.Extensions.Microsoft.DependencyInjection.Middlewares;

internal class UnscopedHandlerExecutionMiddleware(IServiceProvider serviceProvider) : IGearQueueMiddleware
{
    public async Task InvokeAsync(JobContext context, WorkerDelegate? next = null)
    {
        var handler = serviceProvider.GetRequiredService(context.HandlerType!) as IHandler;

        if (handler is null)
        {
            context.SetResult(JobResult.PermanentFailure);
            return;
        }

        var result = await handler.Consume(context);
        context.SetResult(result);
    }
}