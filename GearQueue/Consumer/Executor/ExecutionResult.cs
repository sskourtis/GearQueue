namespace GearQueue.Consumer.Executor;

public readonly struct ExecutionResult
{
    public JobResult? ResultingStatus { get; private init; }
    public TimeSpan? MaximumSleepDelay { get; private init; }

    public static implicit operator ExecutionResult(JobResult result)
    {
        return new ExecutionResult
        {
            ResultingStatus = result,
        };
    }
    
    public static implicit operator ExecutionResult(TimeSpan delay)
    {
        return new ExecutionResult
        {
            MaximumSleepDelay = delay,
        };
    }
}