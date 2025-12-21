namespace LlmPlayground.Services.Models;

/// <summary>
/// Request model for LLM chat completion.
/// </summary>
public record ChatRequest
{
    /// <summary>
    /// The messages in the conversation.
    /// </summary>
    public required IReadOnlyList<ChatMessageDto> Messages { get; init; }

    /// <summary>
    /// Optional inference options.
    /// </summary>
    public InferenceOptionsDto? Options { get; init; }

    /// <summary>
    /// Whether to stream the response.
    /// </summary>
    public bool Stream { get; init; }
}

/// <summary>
/// Request model for simple prompt completion.
/// </summary>
public record CompletionRequest
{
    /// <summary>
    /// The prompt to complete.
    /// </summary>
    public required string Prompt { get; init; }

    /// <summary>
    /// Optional inference options.
    /// </summary>
    public InferenceOptionsDto? Options { get; init; }

    /// <summary>
    /// Whether to stream the response.
    /// </summary>
    public bool Stream { get; init; }
}

/// <summary>
/// DTO for chat messages.
/// </summary>
public record ChatMessageDto
{
    /// <summary>
    /// The role of the message sender (system, user, assistant).
    /// </summary>
    public required string Role { get; init; }

    /// <summary>
    /// The content of the message.
    /// </summary>
    public required string Content { get; init; }
}

/// <summary>
/// DTO for inference options.
/// </summary>
public record InferenceOptionsDto
{
    /// <summary>
    /// Maximum number of tokens to generate.
    /// </summary>
    public int? MaxTokens { get; init; }

    /// <summary>
    /// Temperature for sampling (0.0 = deterministic, higher = more random).
    /// </summary>
    public float? Temperature { get; init; }

    /// <summary>
    /// Top-p (nucleus) sampling threshold.
    /// </summary>
    public float? TopP { get; init; }

    /// <summary>
    /// Penalty for repeating tokens.
    /// </summary>
    public float? RepeatPenalty { get; init; }
}

/// <summary>
/// Response model for LLM completion.
/// </summary>
public record CompletionResponse
{
    /// <summary>
    /// The generated text.
    /// </summary>
    public required string Text { get; init; }

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
    public double TokensPerSecond => Duration.TotalSeconds > 0
        ? TokensGenerated / Duration.TotalSeconds
        : 0;
}

/// <summary>
/// DTO for model information.
/// </summary>
public record ModelInfoDto
{
    /// <summary>
    /// The model identifier.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// The owner/creator of the model.
    /// </summary>
    public string? OwnedBy { get; init; }

    /// <summary>
    /// When the model was created.
    /// </summary>
    public DateTime? Created { get; init; }
}

/// <summary>
/// Available LLM provider types.
/// </summary>
public enum LlmProviderType
{
    Ollama,
    LmStudio,
    OpenAI
}

