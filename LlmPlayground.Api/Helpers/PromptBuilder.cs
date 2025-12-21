using LlmPlayground.Api.Configuration;
using LlmPlayground.Api.Models;

namespace LlmPlayground.Api.Helpers;

/// <summary>
/// Helper for building prompts from templates and request parameters.
/// </summary>
public static class PromptBuilder
{
    /// <summary>
    /// Builds the user prompt for game idea generation.
    /// </summary>
    /// <param name="template">The prompt template with placeholders.</param>
    /// <param name="request">The game generation request.</param>
    /// <returns>The rendered prompt.</returns>
    public static string BuildGameIdeaPrompt(string template, GameGenerationRequest request)
    {
        var themeSection = string.IsNullOrWhiteSpace(request.Theme)
            ? string.Empty
            : $"The game should have a \"{request.Theme}\" theme.";

        var descriptionSection = string.IsNullOrWhiteSpace(request.Description)
            ? string.Empty
            : $"Additional requirements: {request.Description}";

        return template
            .Replace("{ThemeSection}", themeSection)
            .Replace("{DescriptionSection}", descriptionSection)
            .Trim();
    }

    /// <summary>
    /// Builds the user prompt for Prolog code generation.
    /// </summary>
    /// <param name="template">The prompt template with placeholders.</param>
    /// <param name="gameIdea">The game idea/concept to implement.</param>
    /// <returns>The rendered prompt.</returns>
    public static string BuildPrologCodePrompt(string template, string gameIdea)
    {
        return template
            .Replace("{GameIdea}", gameIdea)
            .Trim();
    }

    /// <summary>
    /// Validates a template contains expected placeholders.
    /// </summary>
    /// <param name="template">The template to validate.</param>
    /// <param name="expectedPlaceholders">The placeholders that should be present.</param>
    /// <returns>List of missing placeholders.</returns>
    public static IReadOnlyList<string> ValidateTemplate(string template, params string[] expectedPlaceholders)
    {
        var missing = new List<string>();

        foreach (var placeholder in expectedPlaceholders)
        {
            var pattern = $"{{{placeholder}}}";
            if (!template.Contains(pattern, StringComparison.OrdinalIgnoreCase))
            {
                missing.Add(placeholder);
            }
        }

        return missing;
    }
}

