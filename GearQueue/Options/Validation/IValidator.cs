namespace GearQueue.Options.Validation;

public interface IValidator<T> where T : class
{
    ValidationResult Validate(T options);
}