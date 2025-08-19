namespace GearQueue.Consumer.Coordinators;

public readonly struct ExecutionResult
{
    public JobStatus? ResultingStatus { get; private init; }
    public TimeSpan? MaximumSleepDelay { get; private init; }

    public static implicit operator ExecutionResult(JobStatus status)
    {
        return new ExecutionResult
        {
            ResultingStatus = status,
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