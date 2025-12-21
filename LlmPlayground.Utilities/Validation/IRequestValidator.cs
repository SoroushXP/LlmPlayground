namespace LlmPlayground.Utilities.Validation;

/// <summary>
/// Interface for validating requests before processing.
/// </summary>
public interface IRequestValidator
{
    /// <summary>
    /// Validates a text prompt for safety.
    /// </summary>
    /// <param name="prompt">The prompt text to validate.</param>
    /// <param name="maxLength">Maximum allowed length.</param>
    /// <returns>Validation result indicating if the prompt is safe.</returns>
    ValidationResult ValidatePrompt(string? prompt, int maxLength = 100_000);

    /// <summary>
    /// Validates a file path for safety (prevents path traversal attacks).
    /// </summary>
    /// <param name="filePath">The file path to validate.</param>
    /// <param name="allowedBasePath">Optional base path that the file must be within.</param>
    /// <param name="allowedExtensions">Optional list of allowed file extensions.</param>
    /// <returns>Validation result indicating if the path is safe.</returns>
    ValidationResult ValidateFilePath(
        string? filePath,
        string? allowedBasePath = null,
        IEnumerable<string>? allowedExtensions = null);

    /// <summary>
    /// Validates Prolog code/query for potentially dangerous constructs.
    /// </summary>
    /// <param name="query">The Prolog query to validate.</param>
    /// <returns>Validation result indicating if the query is safe.</returns>
    ValidationResult ValidatePrologQuery(string? query);

    /// <summary>
    /// Validates a chat message for safety.
    /// </summary>
    /// <param name="role">The message role.</param>
    /// <param name="content">The message content.</param>
    /// <returns>Validation result indicating if the message is safe.</returns>
    ValidationResult ValidateChatMessage(string? role, string? content);

    /// <summary>
    /// Validates inference options for reasonable bounds.
    /// </summary>
    /// <param name="maxTokens">Maximum tokens to generate.</param>
    /// <param name="temperature">Temperature setting.</param>
    /// <param name="topP">Top-P setting.</param>
    /// <param name="repeatPenalty">Repeat penalty setting.</param>
    /// <returns>Validation result indicating if options are valid.</returns>
    ValidationResult ValidateInferenceOptions(
        int? maxTokens,
        float? temperature,
        float? topP,
        float? repeatPenalty);
}

