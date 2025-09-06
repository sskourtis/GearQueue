using GearQueue.Worker;
using GearQueue.Worker.Pipeline;
using Microsoft.Extensions.DependencyInjection;

namespace GearQueue.DependencyInjection.Middlewares;

internal class ScopedHandlerExecutionMiddleware(IServiceScopeFactory serviceScopeFactory) : IGearQueueMiddleware
{
    public async Task InvokeAsync(JobContext context, WorkerDelegate? next = null)
    {
        using var scope = serviceScopeFactory.CreateScope();

        if (scope.ServiceProvider.GetRequiredService(context.HandlerType!) is not IHandler handler)
        {
            context.SetResult(JobResult.PermanentFailure);
            return;
        }

        var result = await handler.Consume(context);
        context.SetResult(result);
    }
}