using GearQueue.Logging;
using Microsoft.Extensions.Logging;

namespace GearQueue.Worker.Pipeline;

/// <summary>
/// Final middleware that will execute the appropriate handler.
/// 
/// The Microsoft dependency injection extension library doesn't use the current file.
/// It defines its own middlewares that create the new handler instance using the framework's tools.
/// </summary>
/// <param name="loggerFactory"></param>
public class HandlerExecutionMiddleware(ILoggerFactory loggerFactory) : IGearQueueMiddleware
{
    private readonly ILogger<HandlerExecutionMiddleware> _logger = loggerFactory.CreateLogger<HandlerExecutionMiddleware>();
    
    public async Task InvokeAsync(JobContext context, WorkerDelegate? next = null)
    {
        if (Activator.CreateInstance(context.HandlerType!) is not IHandler handler)
        {
            _logger.LogHandlerTypeCreationFailure(context.HandlerType!, context.FunctionName);
            context.SetResult(JobResult.PermanentFailure);
            return;
        }

        var result = await handler.Handle(context);
        context.SetResult(result);
    }
}