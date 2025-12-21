namespace LlmPlayground.Utilities.Validation;

/// <summary>
/// Represents the result of a validation operation.
/// </summary>
public sealed class ValidationResult
{
    private readonly List<ValidationError> _errors = [];

    /// <summary>
    /// Gets whether the validation passed with no errors.
    /// </summary>
    public bool IsValid => _errors.Count == 0;

    /// <summary>
    /// Gets the collection of validation errors.
    /// </summary>
    public IReadOnlyList<ValidationError> Errors => _errors.AsReadOnly();

    /// <summary>
    /// Creates a successful validation result.
    /// </summary>
    public static ValidationResult Success() => new();

    /// <summary>
    /// Creates a failed validation result with a single error.
    /// </summary>
    public static ValidationResult Failure(string field, string message, ValidationSeverity severity = ValidationSeverity.Error)
    {
        var result = new ValidationResult();
        result.AddError(field, message, severity);
        return result;
    }

    /// <summary>
    /// Creates a failed validation result with a single error.
    /// </summary>
    public static ValidationResult Failure(ValidationError error)
    {
        var result = new ValidationResult();
        result._errors.Add(error);
        return result;
    }

    /// <summary>
    /// Adds an error to the validation result.
    /// </summary>
    public void AddError(string field, string message, ValidationSeverity severity = ValidationSeverity.Error)
    {
        _errors.Add(new ValidationError(field, message, severity));
    }

    /// <summary>
    /// Adds an error to the validation result.
    /// </summary>
    public void AddError(ValidationError error)
    {
        _errors.Add(error);
    }

    /// <summary>
    /// Merges another validation result into this one.
    /// </summary>
    public void Merge(ValidationResult other)
    {
        _errors.AddRange(other._errors);
    }

    /// <summary>
    /// Gets a summary of all error messages.
    /// </summary>
    public string GetErrorSummary(string separator = "; ")
    {
        return string.Join(separator, _errors.Select(e => $"{e.Field}: {e.Message}"));
    }
}

/// <summary>
/// Represents a single validation error.
/// </summary>
public sealed record ValidationError(
    string Field,
    string Message,
    ValidationSeverity Severity = ValidationSeverity.Error
);

/// <summary>
/// Severity level of a validation error.
/// </summary>
public enum ValidationSeverity
{
    /// <summary>
    /// A warning that doesn't prevent processing but should be noted.
    /// </summary>
    Warning,

    /// <summary>
    /// An error that prevents safe processing.
    /// </summary>
    Error,

    /// <summary>
    /// A critical security issue that must be blocked.
    /// </summary>
    Critical
}

