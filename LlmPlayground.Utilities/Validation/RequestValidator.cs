using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace LlmPlayground.Utilities.Validation;

/// <summary>
/// Validates requests for safety before processing by core services.
/// </summary>
public sealed partial class RequestValidator : IRequestValidator
{
    private readonly ILogger<RequestValidator>? _logger;

    // Valid chat roles
    private static readonly HashSet<string> ValidRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        "system", "user", "assistant"
    };

    // Dangerous Prolog predicates that could affect the system
    private static readonly HashSet<string> DangerousPrologPredicates = new(StringComparer.OrdinalIgnoreCase)
    {
        "shell", "system", "exec", "popen", "process_create",
        "open", "close", "read", "write", "delete_file", "rename_file",
        "make_directory", "delete_directory", "working_directory",
        "getenv", "setenv", "halt", "abort"
    };

    public RequestValidator(ILogger<RequestValidator>? logger = null)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public ValidationResult ValidatePrompt(string? prompt, int maxLength = 100_000)
    {
        var result = new ValidationResult();

        if (string.IsNullOrWhiteSpace(prompt))
        {
            result.AddError("Prompt", "Prompt cannot be null or empty.");
            return result;
        }

        if (prompt.Length > maxLength)
        {
            result.AddError("Prompt", $"Prompt exceeds maximum length of {maxLength} characters.");
            return result;
        }

        // Check for null bytes which could indicate binary content or injection attempts
        if (prompt.Contains('\0'))
        {
            result.AddError("Prompt", "Prompt contains invalid null characters.", ValidationSeverity.Critical);
            _logger?.LogWarning("Blocked prompt containing null bytes");
        }

        // Check for excessive control characters (excluding normal whitespace)
        var controlCharCount = prompt.Count(c => char.IsControl(c) && c != '\n' && c != '\r' && c != '\t');
        if (controlCharCount > 10)
        {
            result.AddError("Prompt", "Prompt contains excessive control characters.", ValidationSeverity.Warning);
        }

        return result;
    }

    /// <inheritdoc />
    public ValidationResult ValidateFilePath(
        string? filePath,
        string? allowedBasePath = null,
        IEnumerable<string>? allowedExtensions = null)
    {
        var result = new ValidationResult();

        if (string.IsNullOrWhiteSpace(filePath))
        {
            result.AddError("FilePath", "File path cannot be null or empty.");
            return result;
        }

        // Check for null bytes
        if (filePath.Contains('\0'))
        {
            result.AddError("FilePath", "File path contains invalid null characters.", ValidationSeverity.Critical);
            return result;
        }

        // Check for path traversal patterns
        if (PathTraversalPattern().IsMatch(filePath))
        {
            result.AddError("FilePath", "File path contains path traversal sequences.", ValidationSeverity.Critical);
            _logger?.LogWarning("Blocked path traversal attempt: {Path}", filePath);
            return result;
        }

        // Check for dangerous path patterns on Windows
        if (OperatingSystem.IsWindows())
        {
            if (WindowsDangerousPathPattern().IsMatch(filePath))
            {
                result.AddError("FilePath", "File path contains potentially dangerous Windows path.", ValidationSeverity.Critical);
                return result;
            }
        }

        // Validate against allowed base path
        if (!string.IsNullOrEmpty(allowedBasePath))
        {
            try
            {
                var fullPath = Path.GetFullPath(filePath);
                var basePath = Path.GetFullPath(allowedBasePath);

                if (!fullPath.StartsWith(basePath, StringComparison.OrdinalIgnoreCase))
                {
                    result.AddError("FilePath", $"File path must be within: {allowedBasePath}", ValidationSeverity.Critical);
                    return result;
                }
            }
            catch (Exception ex)
            {
                result.AddError("FilePath", $"Invalid file path: {ex.Message}");
                return result;
            }
        }

        // Validate extension
        if (allowedExtensions is not null)
        {
            var extension = Path.GetExtension(filePath);
            var allowedList = allowedExtensions.ToList();

            if (!allowedList.Any(ext =>
                    ext.Equals(extension, StringComparison.OrdinalIgnoreCase) ||
                    ext.Equals("." + extension.TrimStart('.'), StringComparison.OrdinalIgnoreCase)))
            {
                result.AddError("FilePath", $"File extension '{extension}' is not allowed. Allowed: {string.Join(", ", allowedList)}");
            }
        }

        return result;
    }

    /// <inheritdoc />
    public ValidationResult ValidatePrologQuery(string? query)
    {
        var result = new ValidationResult();

        if (string.IsNullOrWhiteSpace(query))
        {
            result.AddError("Query", "Prolog query cannot be null or empty.");
            return result;
        }

        // Check for null bytes
        if (query.Contains('\0'))
        {
            result.AddError("Query", "Query contains invalid null characters.", ValidationSeverity.Critical);
            return result;
        }

        // Check for dangerous predicates
        foreach (var predicate in DangerousPrologPredicates)
        {
            // Look for predicate calls: predicate( or predicate/
            var pattern = $@"\b{Regex.Escape(predicate)}\s*[\(/]";
            if (Regex.IsMatch(query, pattern, RegexOptions.IgnoreCase))
            {
                result.AddError("Query", $"Query contains potentially dangerous predicate: {predicate}", ValidationSeverity.Critical);
                _logger?.LogWarning("Blocked Prolog query with dangerous predicate: {Predicate}", predicate);
            }
        }

        // Check for shell command execution patterns
        if (ShellExecutionPattern().IsMatch(query))
        {
            result.AddError("Query", "Query contains potential shell execution.", ValidationSeverity.Critical);
        }

        // Check for file operations
        if (FileOperationPattern().IsMatch(query))
        {
            result.AddError("Query", "Query contains potential file system operations.", ValidationSeverity.Warning);
        }

        return result;
    }

    /// <inheritdoc />
    public ValidationResult ValidateChatMessage(string? role, string? content)
    {
        var result = new ValidationResult();

        if (string.IsNullOrWhiteSpace(role))
        {
            result.AddError("Role", "Message role cannot be null or empty.");
        }
        else if (!ValidRoles.Contains(role))
        {
            result.AddError("Role", $"Invalid role '{role}'. Must be one of: {string.Join(", ", ValidRoles)}");
        }

        // Validate content using prompt validation
        var contentResult = ValidatePrompt(content);
        if (!contentResult.IsValid)
        {
            foreach (var error in contentResult.Errors)
            {
                result.AddError($"Content.{error.Field}", error.Message, error.Severity);
            }
        }

        return result;
    }

    /// <inheritdoc />
    public ValidationResult ValidateInferenceOptions(
        int? maxTokens,
        float? temperature,
        float? topP,
        float? repeatPenalty)
    {
        var result = new ValidationResult();

        if (maxTokens.HasValue)
        {
            if (maxTokens.Value <= 0)
            {
                result.AddError("MaxTokens", "MaxTokens must be greater than 0.");
            }
            else if (maxTokens.Value > 100_000)
            {
                result.AddError("MaxTokens", "MaxTokens cannot exceed 100,000.");
            }
        }

        if (temperature.HasValue)
        {
            if (temperature.Value < 0)
            {
                result.AddError("Temperature", "Temperature cannot be negative.");
            }
            else if (temperature.Value > 2)
            {
                result.AddError("Temperature", "Temperature should not exceed 2.0 for reasonable outputs.", ValidationSeverity.Warning);
            }
        }

        if (topP.HasValue)
        {
            if (topP.Value <= 0 || topP.Value > 1)
            {
                result.AddError("TopP", "TopP must be between 0 (exclusive) and 1 (inclusive).");
            }
        }

        if (repeatPenalty.HasValue)
        {
            if (repeatPenalty.Value < 0)
            {
                result.AddError("RepeatPenalty", "RepeatPenalty cannot be negative.");
            }
            else if (repeatPenalty.Value > 5)
            {
                result.AddError("RepeatPenalty", "RepeatPenalty should not exceed 5.0.", ValidationSeverity.Warning);
            }
        }

        return result;
    }

    // Compiled regex patterns for performance
    [GeneratedRegex(@"\.\.[\\/]|[\\/]\.\.")]
    private static partial Regex PathTraversalPattern();

    [GeneratedRegex(@"^\\\\|^[a-zA-Z]:\\(?:windows|system32|program files)", RegexOptions.IgnoreCase)]
    private static partial Regex WindowsDangerousPathPattern();

    [GeneratedRegex(@"(?:shell|system|exec|popen)\s*\(", RegexOptions.IgnoreCase)]
    private static partial Regex ShellExecutionPattern();

    [GeneratedRegex(@"(?:see|tell|told|seen|open|close|read|write|append)\s*\(", RegexOptions.IgnoreCase)]
    private static partial Regex FileOperationPattern();
}

