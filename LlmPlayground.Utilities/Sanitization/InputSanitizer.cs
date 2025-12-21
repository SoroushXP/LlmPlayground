using System.Text;
using System.Text.RegularExpressions;

namespace LlmPlayground.Utilities.Sanitization;

/// <summary>
/// Provides utilities for sanitizing user input before processing.
/// </summary>
public static partial class InputSanitizer
{
    /// <summary>
    /// Sanitizes a text input by removing or replacing potentially harmful content.
    /// </summary>
    /// <param name="input">The input to sanitize.</param>
    /// <param name="options">Sanitization options.</param>
    /// <returns>The sanitized input.</returns>
    public static string Sanitize(string? input, SanitizationOptions? options = null)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        options ??= SanitizationOptions.Default;

        var result = input;

        // Remove null bytes
        if (options.RemoveNullBytes)
        {
            result = result.Replace("\0", string.Empty);
        }

        // Normalize line endings
        if (options.NormalizeLineEndings)
        {
            result = result.Replace("\r\n", "\n").Replace("\r", "\n");
        }

        // Remove control characters (except common whitespace)
        if (options.RemoveControlCharacters)
        {
            result = ControlCharactersPattern().Replace(result, string.Empty);
        }

        // Trim whitespace
        if (options.TrimWhitespace)
        {
            result = result.Trim();
        }

        // Collapse multiple spaces
        if (options.CollapseWhitespace)
        {
            result = MultipleSpacesPattern().Replace(result, " ");
        }

        // Limit length
        if (options.MaxLength.HasValue && result.Length > options.MaxLength.Value)
        {
            result = result[..options.MaxLength.Value];
        }

        return result;
    }

    /// <summary>
    /// Sanitizes a file path by removing potentially dangerous elements.
    /// </summary>
    /// <param name="path">The file path to sanitize.</param>
    /// <returns>The sanitized path.</returns>
    public static string SanitizeFilePath(string? path)
    {
        if (string.IsNullOrEmpty(path))
            return string.Empty;

        var result = path;

        // Remove null bytes
        result = result.Replace("\0", string.Empty);

        // Remove path traversal sequences
        result = PathTraversalPattern().Replace(result, string.Empty);

        // Remove leading/trailing whitespace
        result = result.Trim();

        // Normalize path separators
        result = result.Replace('/', Path.DirectorySeparatorChar)
                       .Replace('\\', Path.DirectorySeparatorChar);

        // Remove consecutive separators
        var separator = Path.DirectorySeparatorChar.ToString();
        while (result.Contains(separator + separator))
        {
            result = result.Replace(separator + separator, separator);
        }

        return result;
    }

    /// <summary>
    /// Sanitizes a string for safe logging (masks sensitive patterns).
    /// </summary>
    /// <param name="input">The input to sanitize for logging.</param>
    /// <returns>The log-safe string.</returns>
    public static string SanitizeForLogging(string? input)
    {
        if (string.IsNullOrEmpty(input))
            return "[empty]";

        var result = input;

        // Mask potential API keys (long alphanumeric strings)
        result = ApiKeyPattern().Replace(result, "[MASKED_KEY]");

        // Mask potential passwords in common formats
        result = PasswordPattern().Replace(result, "$1[MASKED]");

        // Mask email addresses
        result = EmailPattern().Replace(result, "[MASKED_EMAIL]");

        // Truncate very long strings for logging
        if (result.Length > 500)
        {
            result = result[..500] + $"... [truncated, {input.Length} chars total]";
        }

        return result;
    }

    /// <summary>
    /// Encodes a string to be safely included in a URL.
    /// </summary>
    /// <param name="input">The input to encode.</param>
    /// <returns>The URL-encoded string.</returns>
    public static string UrlEncode(string? input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        return Uri.EscapeDataString(input);
    }

    /// <summary>
    /// Escapes special characters for safe inclusion in JSON strings.
    /// </summary>
    /// <param name="input">The input to escape.</param>
    /// <returns>The JSON-safe string.</returns>
    public static string EscapeForJson(string? input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        var sb = new StringBuilder(input.Length);
        foreach (var c in input)
        {
            sb.Append(c switch
            {
                '"' => "\\\"",
                '\\' => "\\\\",
                '\b' => "\\b",
                '\f' => "\\f",
                '\n' => "\\n",
                '\r' => "\\r",
                '\t' => "\\t",
                _ when c < ' ' => $"\\u{(int)c:X4}",
                _ => c.ToString()
            });
        }

        return sb.ToString();
    }

    /// <summary>
    /// Removes HTML tags from a string.
    /// </summary>
    /// <param name="input">The input containing HTML.</param>
    /// <returns>The string with HTML tags removed.</returns>
    public static string StripHtml(string? input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        return HtmlTagPattern().Replace(input, string.Empty);
    }

    // Compiled regex patterns
    [GeneratedRegex(@"[\x00-\x08\x0B\x0C\x0E-\x1F\x7F]")]
    private static partial Regex ControlCharactersPattern();

    [GeneratedRegex(@" {2,}")]
    private static partial Regex MultipleSpacesPattern();

    [GeneratedRegex(@"\.\.[\\/]|[\\/]\.\.")]
    private static partial Regex PathTraversalPattern();

    [GeneratedRegex(@"(?:sk-|pk_|api[_-]?key[_-]?)[a-zA-Z0-9]{20,}", RegexOptions.IgnoreCase)]
    private static partial Regex ApiKeyPattern();

    [GeneratedRegex(@"(password|pwd|secret|token)\s*[:=]\s*\S+", RegexOptions.IgnoreCase)]
    private static partial Regex PasswordPattern();

    [GeneratedRegex(@"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}")]
    private static partial Regex EmailPattern();

    [GeneratedRegex(@"<[^>]+>")]
    private static partial Regex HtmlTagPattern();
}

/// <summary>
/// Options for input sanitization.
/// </summary>
public sealed record SanitizationOptions
{
    /// <summary>
    /// Gets the default sanitization options.
    /// </summary>
    public static SanitizationOptions Default { get; } = new();

    /// <summary>
    /// Gets minimal sanitization options (only removes null bytes).
    /// </summary>
    public static SanitizationOptions Minimal { get; } = new()
    {
        RemoveNullBytes = true,
        RemoveControlCharacters = false,
        NormalizeLineEndings = false,
        TrimWhitespace = false,
        CollapseWhitespace = false
    };

    /// <summary>
    /// Gets strict sanitization options.
    /// </summary>
    public static SanitizationOptions Strict { get; } = new()
    {
        RemoveNullBytes = true,
        RemoveControlCharacters = true,
        NormalizeLineEndings = true,
        TrimWhitespace = true,
        CollapseWhitespace = true,
        MaxLength = 50_000
    };

    /// <summary>
    /// Whether to remove null bytes from input.
    /// </summary>
    public bool RemoveNullBytes { get; init; } = true;

    /// <summary>
    /// Whether to remove control characters (except newlines, tabs).
    /// </summary>
    public bool RemoveControlCharacters { get; init; } = true;

    /// <summary>
    /// Whether to normalize line endings to \n.
    /// </summary>
    public bool NormalizeLineEndings { get; init; } = true;

    /// <summary>
    /// Whether to trim leading and trailing whitespace.
    /// </summary>
    public bool TrimWhitespace { get; init; } = true;

    /// <summary>
    /// Whether to collapse multiple consecutive spaces into one.
    /// </summary>
    public bool CollapseWhitespace { get; init; }

    /// <summary>
    /// Maximum allowed length (null for no limit).
    /// </summary>
    public int? MaxLength { get; init; }
}

