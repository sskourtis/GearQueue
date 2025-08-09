namespace GearQueue.Options.Validation;

public class ValidationResult(IReadOnlyList<string> errors)
{
    public bool IsValid { get; } = errors.Count == 0;
    public IReadOnlyList<string> Errors { get; } = errors;

    public void ThrowIfInvalid()
    {
        if (!IsValid)
            throw new InvalidOperationException($"Configuration validation failed: {string.Join(", ", Errors)}");
    }
}

