namespace LlmPlayground.Core;

/// <summary>
/// Represents information about an available model.
/// </summary>
public record LlmModelInfo(
    string Id,
    string? OwnedBy = null,
    DateTime? Created = null
);

/// <summary>
/// Represents the result of an LLM completion request.
/// </summary>
public record LlmCompletionResult(
    string Text,
    int TokensGenerated,
    TimeSpan Duration
);

/// <summary>
/// Represents the role of a message in a conversation.
/// </summary>
public enum ChatRole
{
    System,
    User,
    Assistant
}

/// <summary>
/// Represents a message in a conversation.
/// </summary>
public record ChatMessage(ChatRole Role, string Content);

/// <summary>
/// Configuration options for LLM inference.
/// </summary>
public record LlmInferenceOptions
{
    /// <summary>
    /// Maximum number of tokens to generate.
    /// </summary>
    public int MaxTokens { get; init; } = 256;

    /// <summary>
    /// Temperature for sampling (0.0 = deterministic, higher = more random).
    /// </summary>
    public float Temperature { get; init; } = 0.7f;

    /// <summary>
    /// Top-p (nucleus) sampling threshold.
    /// </summary>
    public float TopP { get; init; } = 0.9f;

    /// <summary>
    /// Penalty for repeating tokens.
    /// </summary>
    public float RepeatPenalty { get; init; } = 1.1f;
}

/// <summary>
/// Base interface for all LLM providers.
/// Each provider implementation handles communication with a specific LLM backend.
/// </summary>
public interface ILlmProvider : IAsyncDisposable, IDisposable
{
    /// <summary>
    /// Gets the name of this LLM provider.
    /// </summary>
    string ProviderName { get; }

    /// <summary>
    /// Gets a value indicating whether the provider is ready to accept requests.
    /// </summary>
    bool IsReady { get; }

    /// <summary>
    /// Initializes the provider and loads any required resources.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task InitializeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a completion for the given prompt.
    /// </summary>
    /// <param name="prompt">The input prompt.</param>
    /// <param name="options">Inference options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The completion result.</returns>
    Task<LlmCompletionResult> CompleteAsync(
        string prompt,
        LlmInferenceOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a streaming completion for the given prompt.
    /// </summary>
    /// <param name="prompt">The input prompt.</param>
    /// <param name="options">Inference options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An async enumerable of generated tokens.</returns>
    IAsyncEnumerable<string> CompleteStreamingAsync(
        string prompt,
        LlmInferenceOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a completion for the given conversation history.
    /// </summary>
    /// <param name="messages">The conversation history.</param>
    /// <param name="options">Inference options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The completion result.</returns>
    Task<LlmCompletionResult> ChatAsync(
        IReadOnlyList<ChatMessage> messages,
        LlmInferenceOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a streaming completion for the given conversation history.
    /// </summary>
    /// <param name="messages">The conversation history.</param>
    /// <param name="options">Inference options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An async enumerable of generated tokens.</returns>
    IAsyncEnumerable<string> ChatStreamingAsync(
        IReadOnlyList<ChatMessage> messages,
        LlmInferenceOptions? options = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for providers that support listing available models.
/// </summary>
public interface IModelListingProvider
{
    /// <summary>
    /// Gets a list of available models from the server.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of available models.</returns>
    Task<IReadOnlyList<LlmModelInfo>> GetAvailableModelsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the model to use for subsequent requests.
    /// </summary>
    /// <param name="modelId">The model identifier.</param>
    void SetModel(string modelId);

    /// <summary>
    /// Gets the currently configured model identifier.
    /// </summary>
    string CurrentModel { get; }
}

