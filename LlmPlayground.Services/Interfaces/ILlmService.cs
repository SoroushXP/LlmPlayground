using LlmPlayground.Services.Models;

namespace LlmPlayground.Services.Interfaces;

/// <summary>
/// Service for interacting with LLM providers.
/// </summary>
public interface ILlmService
{
    /// <summary>
    /// Gets a list of available providers.
    /// </summary>
    IReadOnlyList<string> GetAvailableProviders();

    /// <summary>
    /// Gets the current active provider name.
    /// </summary>
    string CurrentProvider { get; }

    /// <summary>
    /// Sets the active provider.
    /// </summary>
    /// <param name="providerType">The provider type to use.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SetProviderAsync(LlmProviderType providerType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a list of available models from the current provider.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of available models, or empty if provider doesn't support model listing.</returns>
    Task<IReadOnlyList<ModelInfoDto>> GetAvailableModelsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the model to use for subsequent requests.
    /// </summary>
    /// <param name="modelId">The model identifier.</param>
    void SetModel(string modelId);

    /// <summary>
    /// Gets the currently configured model identifier.
    /// </summary>
    string? CurrentModel { get; }

    /// <summary>
    /// Generates a completion for the given prompt.
    /// </summary>
    /// <param name="request">The completion request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The completion response.</returns>
    Task<CompletionResponse> CompleteAsync(CompletionRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a streaming completion for the given prompt.
    /// </summary>
    /// <param name="request">The completion request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An async enumerable of generated tokens.</returns>
    IAsyncEnumerable<string> CompleteStreamingAsync(CompletionRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a chat completion for the given messages.
    /// </summary>
    /// <param name="request">The chat request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The completion response.</returns>
    Task<CompletionResponse> ChatAsync(ChatRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a streaming chat completion.
    /// </summary>
    /// <param name="request">The chat request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An async enumerable of generated tokens.</returns>
    IAsyncEnumerable<string> ChatStreamingAsync(ChatRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the current provider is ready.
    /// </summary>
    bool IsReady { get; }
}

