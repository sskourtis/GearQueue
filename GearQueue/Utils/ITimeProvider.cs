namespace GearQueue.Utils;

internal interface ITimeProvider
{
    DateTimeOffset Now { get; }
}

internal class TimeProvider : ITimeProvider
{
    public DateTimeOffset Now => DateTimeOffset.UtcNow;
}