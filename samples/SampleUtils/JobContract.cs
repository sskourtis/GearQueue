using GearQueue.Producer;

namespace SampleUtils;

[GearQueueJob("test-function")]
public class JobContract
{
    public string? TestValue { get; set; }
}