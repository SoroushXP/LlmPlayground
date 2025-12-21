namespace LlmPlayground.Console.Configuration;

/// <summary>
/// Settings for game generation, loaded from configuration file.
/// Prompts can be modified without recompiling the application.
/// </summary>
public sealed class GameGenerationSettings
{
    /// <summary>
    /// System prompt for generating game ideas.
    /// </summary>
    public string GameIdeaSystemPrompt { get; set; } = DefaultPrompts.GameIdeaSystemPrompt;

    /// <summary>
    /// User prompt template for generating game ideas.
    /// Supports placeholders: {ThemeSection}, {DescriptionSection}
    /// </summary>
    public string GameIdeaUserPromptTemplate { get; set; } = DefaultPrompts.GameIdeaUserPromptTemplate;

    /// <summary>
    /// System prompt for generating Prolog code.
    /// </summary>
    public string PrologCodeSystemPrompt { get; set; } = DefaultPrompts.PrologCodeSystemPrompt;

    /// <summary>
    /// User prompt template for generating Prolog code.
    /// Supports placeholder: {GameIdea}
    /// </summary>
    public string PrologCodeUserPromptTemplate { get; set; } = DefaultPrompts.PrologCodeUserPromptTemplate;

    /// <summary>
    /// Default maximum tokens for game idea generation.
    /// </summary>
    public int DefaultGameIdeaMaxTokens { get; set; } = 1024;

    /// <summary>
    /// Default temperature for game idea generation (higher = more creative).
    /// </summary>
    public float DefaultGameIdeaTemperature { get; set; } = 0.8f;

    /// <summary>
    /// Default maximum tokens for Prolog code generation.
    /// </summary>
    public int DefaultPrologCodeMaxTokens { get; set; } = 2048;

    /// <summary>
    /// Default temperature for Prolog code generation (lower = more precise).
    /// </summary>
    public float DefaultPrologCodeTemperature { get; set; } = 0.5f;

    /// <summary>
    /// Default Prolog goal to execute when running the game.
    /// </summary>
    public string DefaultPrologGoal { get; set; } = "main";

    /// <summary>
    /// Directory to save generated Prolog files. If empty, uses temp directory.
    /// </summary>
    public string PrologOutputDirectory { get; set; } = string.Empty;

    /// <summary>
    /// Whether to keep generated Prolog files after execution.
    /// </summary>
    public bool KeepGeneratedFiles { get; set; } = true;

    /// <summary>
    /// Maximum number of retry attempts to fix Prolog code errors.
    /// </summary>
    public int MaxFixRetries { get; set; } = 3;

    /// <summary>
    /// System prompt for fixing Prolog code errors.
    /// </summary>
    public string PrologFixSystemPrompt { get; set; } = DefaultPrompts.PrologFixSystemPrompt;

    /// <summary>
    /// User prompt template for fixing Prolog code errors.
    /// Supports placeholders: {PrologCode}, {Errors}
    /// </summary>
    public string PrologFixUserPromptTemplate { get; set; } = DefaultPrompts.PrologFixUserPromptTemplate;
}

