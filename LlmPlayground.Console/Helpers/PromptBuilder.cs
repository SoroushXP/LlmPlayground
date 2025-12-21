namespace LlmPlayground.Console.Helpers;

/// <summary>
/// Helper for building prompts from templates and request parameters.
/// </summary>
public static class PromptBuilder
{
    /// <summary>
    /// Builds the user prompt for game idea generation.
    /// </summary>
    /// <param name="template">The prompt template with placeholders.</param>
    /// <param name="theme">Optional game theme.</param>
    /// <param name="description">Optional additional requirements.</param>
    /// <returns>The rendered prompt.</returns>
    public static string BuildGameIdeaPrompt(string template, string? theme, string? description)
    {
        var themeSection = string.IsNullOrWhiteSpace(theme)
            ? string.Empty
            : $"The game should have a \"{theme}\" theme.";

        var descriptionSection = string.IsNullOrWhiteSpace(description)
            ? string.Empty
            : $"Additional requirements: {description}";

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
    /// Builds the user prompt for fixing Prolog code errors.
    /// </summary>
    /// <param name="template">The prompt template with placeholders.</param>
    /// <param name="prologCode">The Prolog code to fix.</param>
    /// <param name="errors">The errors encountered.</param>
    /// <returns>The rendered prompt.</returns>
    public static string BuildPrologFixPrompt(string template, string prologCode, string errors)
    {
        return template
            .Replace("{PrologCode}", prologCode)
            .Replace("{Errors}", errors)
            .Trim();
    }
}

