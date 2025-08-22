using GearQueue.Consumer.Provider;
using GearQueue.Logging;
using Microsoft.Extensions.Logging;

namespace GearQueue.Consumer.Pipeline;

internal class HandlerExecutionMiddleware(
    ILoggerFactory loggerFactory,
    IHandlerProviderFactory handlerProviderFactory) : IGearQueueMiddleware
{
    private readonly ILogger<HandlerExecutionMiddleware> _logger = loggerFactory.CreateLogger<HandlerExecutionMiddleware>();
    
    public async Task InvokeAsync(JobContext context, ConsumerDelegate? next = null)
    {
        using var provider = handlerProviderFactory.Create();

        var handler = provider.Get(context.HandlerType!);

        if (handler is null)
        {
            _logger.LogHandlerTypeCreationFailure(context.HandlerType!, context.FunctionName);
            context.SetResult(JobResult.PermanentFailure);
            return;
        }

        var result = await handler.Consume(context);
        context.SetResult(result);
    }
}