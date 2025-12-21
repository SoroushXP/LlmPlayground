using System.Text.RegularExpressions;

namespace LlmPlayground.PromptLab;

/// <summary>
/// Represents a reusable prompt template with variable substitution.
/// </summary>
public partial class PromptTemplate
{
    private readonly string _template;
    private readonly HashSet<string> _variables;

    /// <summary>
    /// Gets the raw template string.
    /// </summary>
    public string Template => _template;

    /// <summary>
    /// Gets the variable names found in the template.
    /// </summary>
    public IReadOnlySet<string> Variables => _variables;

    /// <summary>
    /// Creates a new prompt template.
    /// Variables are defined using {{variableName}} syntax.
    /// </summary>
    /// <param name="template">The template string with {{variable}} placeholders.</param>
    public PromptTemplate(string template)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(template);
        _template = template;
        _variables = ExtractVariables(template);
    }

    /// <summary>
    /// Renders the template with the provided variables.
    /// </summary>
    /// <param name="variables">Dictionary of variable names and values.</param>
    /// <returns>The rendered prompt string.</returns>
    public string Render(IDictionary<string, string> variables)
    {
        ArgumentNullException.ThrowIfNull(variables);

        var result = _template;
        foreach (var (name, value) in variables)
        {
            result = result.Replace($"{{{{{name}}}}}", value);
        }

        return result;
    }

    /// <summary>
    /// Renders the template with the provided variables using anonymous object.
    /// </summary>
    /// <param name="variables">Anonymous object with properties matching variable names.</param>
    /// <returns>The rendered prompt string.</returns>
    public string Render(object variables)
    {
        ArgumentNullException.ThrowIfNull(variables);

        var dict = variables.GetType()
            .GetProperties()
            .ToDictionary(
                p => p.Name,
                p => p.GetValue(variables)?.ToString() ?? string.Empty);

        return Render(dict);
    }

    /// <summary>
    /// Validates that all required variables are provided.
    /// </summary>
    /// <param name="variables">The variables to validate.</param>
    /// <returns>List of missing variable names.</returns>
    public IReadOnlyList<string> GetMissingVariables(IDictionary<string, string> variables)
    {
        return _variables.Where(v => !variables.ContainsKey(v)).ToList();
    }

    /// <summary>
    /// Creates a template from a file.
    /// </summary>
    /// <param name="filePath">Path to the template file.</param>
    /// <returns>The loaded prompt template.</returns>
    public static async Task<PromptTemplate> FromFileAsync(string filePath)
    {
        var content = await File.ReadAllTextAsync(filePath);
        return new PromptTemplate(content);
    }

    /// <summary>
    /// Creates a template from a file.
    /// </summary>
    /// <param name="filePath">Path to the template file.</param>
    /// <returns>The loaded prompt template.</returns>
    public static PromptTemplate FromFile(string filePath)
    {
        var content = File.ReadAllText(filePath);
        return new PromptTemplate(content);
    }

    private static HashSet<string> ExtractVariables(string template)
    {
        var matches = VariablePattern().Matches(template);
        return matches.Select(m => m.Groups[1].Value).ToHashSet();
    }

    [GeneratedRegex(@"\{\{(\w+)\}\}")]
    private static partial Regex VariablePattern();

    /// <summary>
    /// Implicitly converts a string to a PromptTemplate.
    /// </summary>
    public static implicit operator PromptTemplate(string template) => new(template);

    /// <inheritdoc />
    public override string ToString() => _template;
}

/// <summary>
/// Builder for constructing prompts with a fluent API.
/// </summary>
public class PromptBuilder
{
    private readonly List<string> _parts = [];
    private string? _systemPrompt;

    /// <summary>
    /// Sets the system prompt.
    /// </summary>
    public PromptBuilder WithSystem(string systemPrompt)
    {
        _systemPrompt = systemPrompt;
        return this;
    }

    /// <summary>
    /// Appends text to the prompt.
    /// </summary>
    public PromptBuilder Append(string text)
    {
        _parts.Add(text);
        return this;
    }

    /// <summary>
    /// Appends a line to the prompt.
    /// </summary>
    public PromptBuilder AppendLine(string text)
    {
        _parts.Add(text + Environment.NewLine);
        return this;
    }

    /// <summary>
    /// Appends an empty line.
    /// </summary>
    public PromptBuilder AppendLine()
    {
        _parts.Add(Environment.NewLine);
        return this;
    }

    /// <summary>
    /// Appends a code block.
    /// </summary>
    public PromptBuilder AppendCodeBlock(string code, string language = "")
    {
        _parts.Add($"```{language}{Environment.NewLine}{code}{Environment.NewLine}```{Environment.NewLine}");
        return this;
    }

    /// <summary>
    /// Appends a list of items.
    /// </summary>
    public PromptBuilder AppendList(IEnumerable<string> items, string prefix = "- ")
    {
        foreach (var item in items)
        {
            _parts.Add($"{prefix}{item}{Environment.NewLine}");
        }
        return this;
    }

    /// <summary>
    /// Appends a numbered list.
    /// </summary>
    public PromptBuilder AppendNumberedList(IEnumerable<string> items)
    {
        var i = 1;
        foreach (var item in items)
        {
            _parts.Add($"{i}. {item}{Environment.NewLine}");
            i++;
        }
        return this;
    }

    /// <summary>
    /// Conditionally appends content.
    /// </summary>
    public PromptBuilder AppendIf(bool condition, string text)
    {
        if (condition)
            _parts.Add(text);
        return this;
    }

    /// <summary>
    /// Conditionally appends content using a factory function.
    /// </summary>
    public PromptBuilder AppendIf(bool condition, Func<string> textFactory)
    {
        if (condition)
            _parts.Add(textFactory());
        return this;
    }

    /// <summary>
    /// Gets the system prompt, if set.
    /// </summary>
    public string? SystemPrompt => _systemPrompt;

    /// <summary>
    /// Builds the final prompt string.
    /// </summary>
    public string Build() => string.Concat(_parts);

    /// <inheritdoc />
    public override string ToString() => Build();
}

