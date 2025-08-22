using GearQueue.Consumer;
using GearQueue.Consumer.Pipeline;
using Microsoft.Extensions.DependencyInjection;

namespace GearQueue.Extensions.Microsoft.DependencyInjection.Middlewares;

internal class ScopedHandlerExecutionMiddleware(IServiceScopeFactory serviceScopeFactory) : IGearQueueMiddleware
{
    public async Task InvokeAsync(JobContext context, ConsumerDelegate? next = null)
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