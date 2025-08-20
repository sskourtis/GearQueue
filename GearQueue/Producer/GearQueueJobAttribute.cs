namespace GearQueue.Producer;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public class GearQueueJobAttribute(string functionName) : Attribute
{
    public string FunctionName { get; init; } = functionName;
}