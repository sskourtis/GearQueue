namespace GearQueue.Options.Validation;

public interface IGearQueueValidator<T> where T : class
{
    ValidationResult Validate(T options);
}