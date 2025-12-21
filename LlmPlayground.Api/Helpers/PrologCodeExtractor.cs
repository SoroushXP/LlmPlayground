using System.Text.RegularExpressions;

namespace LlmPlayground.Api.Helpers;

/// <summary>
/// Helper for extracting Prolog code from LLM responses.
/// </summary>
public static partial class PrologCodeExtractor
{
    /// <summary>
    /// Extracts Prolog code from an LLM response that may contain markdown code blocks.
    /// </summary>
    /// <param name="response">The LLM response containing Prolog code.</param>
    /// <returns>The extracted Prolog code, or the original response if no code block is found.</returns>
    public static string ExtractPrologCode(string? response)
    {
        if (string.IsNullOrWhiteSpace(response))
            return string.Empty;

        // Try to extract code from ```prolog blocks first
        var prologMatch = PrologCodeBlockPattern().Match(response);
        if (prologMatch.Success)
        {
            return prologMatch.Groups[1].Value.Trim();
        }

        // Try generic code blocks
        var genericMatch = GenericCodeBlockPattern().Match(response);
        if (genericMatch.Success)
        {
            return genericMatch.Groups[1].Value.Trim();
        }

        // If no code blocks found, try to identify Prolog code patterns
        // Look for common Prolog constructs like facts and rules
        if (ContainsPrologCode(response))
        {
            return ExtractPrologStatements(response);
        }

        // Return the original response as-is
        return response.Trim();
    }

    /// <summary>
    /// Validates that extracted code appears to be valid Prolog.
    /// </summary>
    /// <param name="code">The code to validate.</param>
    /// <returns>True if the code appears to be Prolog, false otherwise.</returns>
    public static bool IsPrologCode(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return false;

        // Check for common Prolog patterns
        // - Facts: name(arg1, arg2).
        // - Rules: head :- body.
        // - Comments: % comment or /* comment */
        return PrologFactPattern().IsMatch(code) || 
               PrologRulePattern().IsMatch(code) ||
               PrologCommentPattern().IsMatch(code);
    }

    /// <summary>
    /// Removes any unsafe predicates from Prolog code.
    /// </summary>
    /// <param name="code">The Prolog code to sanitize.</param>
    /// <returns>The sanitized code with unsafe predicates commented out.</returns>
    public static string SanitizePrologCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return string.Empty;

        // Comment out lines containing unsafe predicates
        var lines = code.Split('\n');
        var sanitizedLines = lines.Select(line =>
        {
            if (UnsafePredicatePattern().IsMatch(line))
            {
                return $"% UNSAFE - commented out: {line}";
            }
            return line;
        });

        return string.Join('\n', sanitizedLines);
    }

    private static bool ContainsPrologCode(string text)
    {
        return PrologFactPattern().IsMatch(text) || PrologRulePattern().IsMatch(text);
    }

    private static string ExtractPrologStatements(string text)
    {
        // Extract lines that look like Prolog statements
        var lines = text.Split('\n');
        var prologLines = new List<string>();

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            
            // Skip empty lines and obvious prose
            if (string.IsNullOrWhiteSpace(trimmed))
            {
                prologLines.Add(string.Empty);
                continue;
            }

            // Include Prolog comments
            if (trimmed.StartsWith('%') || trimmed.StartsWith("/*"))
            {
                prologLines.Add(trimmed);
                continue;
            }

            // Include lines that look like Prolog statements
            if (PrologFactPattern().IsMatch(trimmed) || 
                PrologRulePattern().IsMatch(trimmed) ||
                trimmed.EndsWith('.'))
            {
                prologLines.Add(trimmed);
            }
        }

        return string.Join('\n', prologLines).Trim();
    }

    // Regex patterns
    [GeneratedRegex(@"```(?:prolog|pl)\s*\n([\s\S]*?)\n?```", RegexOptions.IgnoreCase)]
    private static partial Regex PrologCodeBlockPattern();

    [GeneratedRegex(@"```\s*\n([\s\S]*?)\n?```")]
    private static partial Regex GenericCodeBlockPattern();

    [GeneratedRegex(@"^\s*[a-z_][a-z0-9_]*\s*\([^)]*\)\s*\.", RegexOptions.Multiline | RegexOptions.IgnoreCase)]
    private static partial Regex PrologFactPattern();

    [GeneratedRegex(@"^\s*[a-z_][a-z0-9_]*\s*(?:\([^)]*\))?\s*:-", RegexOptions.Multiline | RegexOptions.IgnoreCase)]
    private static partial Regex PrologRulePattern();

    [GeneratedRegex(@"(?:^\s*%|/\*)", RegexOptions.Multiline)]
    private static partial Regex PrologCommentPattern();

    [GeneratedRegex(@"\b(?:shell|system|exec|popen|process_create|halt|abort)\s*\(", RegexOptions.IgnoreCase)]
    private static partial Regex UnsafePredicatePattern();
}

