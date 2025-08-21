namespace GearQueue.Consumer.Pipeline;

public class ConsumerPipeline
{
    private readonly IGearQueueMiddleware[] _middlewares;
    private readonly ConsumerDelegate _pipeline;
    public ConsumerPipeline(IGearQueueMiddleware[] middlewares)
    {
        _middlewares = middlewares;
        _pipeline = BuildPipeline();
    }

    public Task InvokeAsync(JobContext context)
    {
        return _pipeline(context);
    }
    
    private ConsumerDelegate BuildPipeline()
    {
        if (_middlewares.Length == 0)
        {
            return _ => Task.CompletedTask;
        }

        if (_middlewares.Length == 1)
        {
            return context => _middlewares[0].InvokeAsync(context);
        }

        ConsumerDelegate pipeline = context => _middlewares[^1].InvokeAsync(context);
        
        for (var i = _middlewares.Length - 2; i >= 0; i--)
        {
            var middleware = _middlewares[i];
            var next = pipeline;
            pipeline = context => middleware.InvokeAsync(context, next);
        }
        
        return pipeline;
    }
}