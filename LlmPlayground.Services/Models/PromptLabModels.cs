namespace LlmPlayground.Services.Models;

/// <summary>
/// Request model for creating a prompt session.
/// </summary>
public record CreateSessionRequest
{
    /// <summary>
    /// The LLM provider to use.
    /// </summary>
    public LlmProviderType Provider { get; init; } = LlmProviderType.Ollama;

    /// <summary>
    /// Optional system prompt for the session.
    /// </summary>
    public string? SystemPrompt { get; init; }

    /// <summary>
    /// Optional inference options.
    /// </summary>
    public InferenceOptionsDto? Options { get; init; }
}

/// <summary>
/// Response after creating a session.
/// </summary>
public record SessionCreatedResponse
{
    /// <summary>
    /// The session identifier.
    /// </summary>
    public required string SessionId { get; init; }

    /// <summary>
    /// The provider being used.
    /// </summary>
    public required string Provider { get; init; }
}

/// <summary>
/// Request model for sending a prompt in a session.
/// </summary>
public record SendPromptRequest
{
    /// <summary>
    /// The prompt text to send.
    /// </summary>
    public required string Prompt { get; init; }

    /// <summary>
    /// Whether to stream the response.
    /// </summary>
    public bool Stream { get; init; }
}

/// <summary>
/// Response model for a prompt result.
/// </summary>
public record PromptResponse
{
    /// <summary>
    /// The original prompt.
    /// </summary>
    public required string Prompt { get; init; }

    /// <summary>
    /// The LLM response.
    /// </summary>
    public required string Response { get; init; }

    /// <summary>
    /// Number of tokens generated.
    /// </summary>
    public int TokensGenerated { get; init; }

    /// <summary>
    /// Time taken to generate the response.
    /// </summary>
    public TimeSpan Duration { get; init; }

    /// <summary>
    /// Tokens per second.
    /// </summary>
    public double TokensPerSecond { get; init; }

    /// <summary>
    /// Whether the prompt executed successfully.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Error message if the prompt failed.
    /// </summary>
    public string? Error { get; init; }
}

/// <summary>
/// Represents a prompt/response exchange in session history.
/// </summary>
public record PromptExchangeDto
{
    /// <summary>
    /// The user's prompt.
    /// </summary>
    public required string Prompt { get; init; }

    /// <summary>
    /// The assistant's response.
    /// </summary>
    public required string Response { get; init; }

    /// <summary>
    /// When the exchange occurred.
    /// </summary>
    public DateTime Timestamp { get; init; }
}

/// <summary>
/// Session information response.
/// </summary>
public record SessionInfoResponse
{
    /// <summary>
    /// The session identifier.
    /// </summary>
    public required string SessionId { get; init; }

    /// <summary>
    /// The provider being used.
    /// </summary>
    public required string Provider { get; init; }

    /// <summary>
    /// The system prompt, if set.
    /// </summary>
    public string? SystemPrompt { get; init; }

    /// <summary>
    /// Number of exchanges in history.
    /// </summary>
    public int HistoryCount { get; init; }

    /// <summary>
    /// The conversation history.
    /// </summary>
    public IReadOnlyList<PromptExchangeDto> History { get; init; } = [];
}

/// <summary>
/// Request for rendering a prompt template.
/// </summary>
public record RenderTemplateRequest
{
    /// <summary>
    /// The template string with {{variable}} placeholders.
    /// </summary>
    public required string Template { get; init; }

    /// <summary>
    /// Dictionary of variable names and values.
    /// </summary>
    public required IDictionary<string, string> Variables { get; init; }
}

/// <summary>
/// Response after rendering a template.
/// </summary>
public record RenderTemplateResponse
{
    /// <summary>
    /// The rendered prompt.
    /// </summary>
    public required string RenderedPrompt { get; init; }

    /// <summary>
    /// Variables that were found in the template.
    /// </summary>
    public required IReadOnlyList<string> Variables { get; init; }

    /// <summary>
    /// Variables that were missing from the input.
    /// </summary>
    public required IReadOnlyList<string> MissingVariables { get; init; }
}

