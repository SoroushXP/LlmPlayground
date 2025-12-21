using System.ClientModel;
using System.Diagnostics;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using OpenAI;
using OpenAI.Chat;

namespace LlmPlayground.Core;

/// <summary>
/// Base class for providers that use OpenAI-compatible APIs.
/// </summary>
public abstract class OpenAiCompatibleProviderBase : ILlmProvider, IModelListingProvider
{
    private readonly HttpClient _httpClient;
    private readonly bool _ownsHttpClient;
    protected readonly ILogger Logger;
    private ChatClient? _client;
    private bool _disposed;

    /// <summary>
    /// Gets the current configuration for the provider.
    /// </summary>
    protected abstract OpenAiCompatibleConfiguration Configuration { get; }

    /// <summary>
    /// Gets the API credential to use for requests.
    /// </summary>
    protected abstract ApiKeyCredential GetCredential();

    /// <summary>
    /// Initializes a new instance with an injected HttpClient.
    /// </summary>
    /// <param name="httpClient">The HttpClient to use for API calls.</param>
    /// <param name="logger">Optional logger instance.</param>
    protected OpenAiCompatibleProviderBase(HttpClient httpClient, ILogger? logger = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _ownsHttpClient = false;
        Logger = logger ?? NullLogger.Instance;
    }

    /// <summary>
    /// Initializes a new instance creating its own HttpClient.
    /// </summary>
    /// <param name="baseUrl">The base URL for the API.</param>
    /// <param name="timeoutSeconds">Request timeout in seconds.</param>
    /// <param name="logger">Optional logger instance.</param>
    protected OpenAiCompatibleProviderBase(string baseUrl, int timeoutSeconds, ILogger? logger = null)
    {
        var url = baseUrl.EndsWith('/') ? baseUrl : baseUrl + "/";
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(url),
            Timeout = TimeSpan.FromSeconds(timeoutSeconds)
        };
        _ownsHttpClient = true;
        Logger = logger ?? NullLogger.Instance;
    }

    /// <inheritdoc />
    public abstract string ProviderName { get; }

    /// <inheritdoc />
    public bool IsReady => _client != null && !_disposed;

    /// <inheritdoc />
    public abstract string CurrentModel { get; }

    /// <summary>
    /// Called when the model is changed to reset the client.
    /// </summary>
    protected void ResetClient()
    {
        _client = null;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<LlmModelInfo>> GetAvailableModelsAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            Logger.LogDebug("Fetching available models from {Provider}", ProviderName);
            var response = await _httpClient.GetFromJsonAsync<ModelsResponse>("models", cancellationToken);

            if (response?.Data == null)
            {
                Logger.LogWarning("No models returned from {Provider}", ProviderName);
                return [];
            }

            var models = response.Data
                .Select(m => new LlmModelInfo(
                    m.Id,
                    m.OwnedBy,
                    m.Created.HasValue ? DateTimeOffset.FromUnixTimeSeconds(m.Created.Value).DateTime : null))
                .ToList();

            Logger.LogDebug("Found {Count} models from {Provider}", models.Count, ProviderName);
            return models;
        }
        catch (HttpRequestException ex)
        {
            Logger.LogWarning(ex, "Failed to fetch models from {Provider}", ProviderName);
            return [];
        }
    }

    /// <inheritdoc />
    public abstract void SetModel(string modelId);

    /// <inheritdoc />
    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        if (_client != null)
            return Task.CompletedTask;

        var config = Configuration;
        if (string.IsNullOrWhiteSpace(config.Model))
            throw new InvalidOperationException("Model name is not set. Call SetModel or configure a model first.");

        Logger.LogInformation("Initializing {Provider} with model {Model}", ProviderName, config.Model);

        var options = new OpenAIClientOptions
        {
            Endpoint = new Uri(config.BaseUrl)
        };

        var openAiClient = new OpenAIClient(GetCredential(), options);
        _client = openAiClient.GetChatClient(config.Model);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<LlmCompletionResult> CompleteAsync(
        string prompt,
        LlmInferenceOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ThrowIfNotInitialized();

        options ??= new LlmInferenceOptions();
        var messages = BuildMessages(prompt);
        var chatOptions = BuildChatOptions(options);

        Logger.LogDebug("Completing prompt with {Provider}, MaxTokens={MaxTokens}", ProviderName, options.MaxTokens);

        var stopwatch = Stopwatch.StartNew();
        var response = await _client!.CompleteChatAsync(messages, chatOptions, cancellationToken);
        stopwatch.Stop();

        var completion = response.Value;
        var text = completion.Content[0].Text ?? string.Empty;
        var tokensGenerated = completion.Usage?.OutputTokenCount ?? 0;

        Logger.LogDebug("Completed in {Duration}ms, {Tokens} tokens", stopwatch.ElapsedMilliseconds, tokensGenerated);

        return new LlmCompletionResult(text, tokensGenerated, stopwatch.Elapsed);
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<string> CompleteStreamingAsync(
        string prompt,
        LlmInferenceOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ThrowIfNotInitialized();

        options ??= new LlmInferenceOptions();
        var messages = BuildMessages(prompt);
        var chatOptions = BuildChatOptions(options);

        Logger.LogDebug("Starting streaming completion with {Provider}", ProviderName);

        var streamingUpdates = _client!.CompleteChatStreamingAsync(messages, chatOptions, cancellationToken);

        await foreach (var update in streamingUpdates.WithCancellation(cancellationToken))
        {
            foreach (var contentPart in update.ContentUpdate)
            {
                if (!string.IsNullOrEmpty(contentPart.Text))
                {
                    yield return contentPart.Text;
                }
            }
        }
    }

    /// <inheritdoc />
    public async Task<LlmCompletionResult> ChatAsync(
        IReadOnlyList<ChatMessage> messages,
        LlmInferenceOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ThrowIfNotInitialized();

        options ??= new LlmInferenceOptions();
        var chatMessages = ConvertMessages(messages);
        var chatOptions = BuildChatOptions(options);

        Logger.LogDebug("Chat with {Provider}, {MessageCount} messages", ProviderName, messages.Count);

        var stopwatch = Stopwatch.StartNew();
        var response = await _client!.CompleteChatAsync(chatMessages, chatOptions, cancellationToken);
        stopwatch.Stop();

        var completion = response.Value;
        var text = completion.Content[0].Text ?? string.Empty;
        var tokensGenerated = completion.Usage?.OutputTokenCount ?? 0;

        Logger.LogDebug("Chat completed in {Duration}ms, {Tokens} tokens", stopwatch.ElapsedMilliseconds, tokensGenerated);

        return new LlmCompletionResult(text, tokensGenerated, stopwatch.Elapsed);
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<string> ChatStreamingAsync(
        IReadOnlyList<ChatMessage> messages,
        LlmInferenceOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ThrowIfNotInitialized();

        options ??= new LlmInferenceOptions();
        var chatMessages = ConvertMessages(messages);
        var chatOptions = BuildChatOptions(options);

        Logger.LogDebug("Starting streaming chat with {Provider}, {MessageCount} messages", ProviderName, messages.Count);

        var streamingUpdates = _client!.CompleteChatStreamingAsync(chatMessages, chatOptions, cancellationToken);

        await foreach (var update in streamingUpdates.WithCancellation(cancellationToken))
        {
            foreach (var contentPart in update.ContentUpdate)
            {
                if (!string.IsNullOrEmpty(contentPart.Text))
                {
                    yield return contentPart.Text;
                }
            }
        }
    }

    private List<OpenAI.Chat.ChatMessage> BuildMessages(string prompt)
    {
        var messages = new List<OpenAI.Chat.ChatMessage>();
        var config = Configuration;

        if (!string.IsNullOrWhiteSpace(config.SystemPrompt))
        {
            messages.Add(new SystemChatMessage(config.SystemPrompt));
        }

        messages.Add(new UserChatMessage(prompt));
        return messages;
    }

    private List<OpenAI.Chat.ChatMessage> ConvertMessages(IReadOnlyList<ChatMessage> messages)
    {
        var result = new List<OpenAI.Chat.ChatMessage>();
        var config = Configuration;

        // Add system prompt if configured and not already in messages
        if (!string.IsNullOrWhiteSpace(config.SystemPrompt) &&
            !messages.Any(m => m.Role == ChatRole.System))
        {
            result.Add(new SystemChatMessage(config.SystemPrompt));
        }

        foreach (var msg in messages)
        {
            result.Add(msg.Role switch
            {
                ChatRole.System => new SystemChatMessage(msg.Content),
                ChatRole.User => new UserChatMessage(msg.Content),
                ChatRole.Assistant => new AssistantChatMessage(msg.Content),
                _ => new UserChatMessage(msg.Content)
            });
        }

        return result;
    }

    private static ChatCompletionOptions BuildChatOptions(LlmInferenceOptions options)
    {
        return new ChatCompletionOptions
        {
            MaxOutputTokenCount = options.MaxTokens,
            Temperature = options.Temperature,
            TopP = options.TopP,
            FrequencyPenalty = options.RepeatPenalty - 1.0f
        };
    }

    protected void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    private void ThrowIfNotInitialized()
    {
        if (_client == null)
            throw new InvalidOperationException("Provider is not initialized. Call InitializeAsync first.");
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
            return;

        if (_ownsHttpClient)
        {
            _httpClient.Dispose();
        }

        _client = null;
        _disposed = true;

        Logger.LogDebug("{Provider} disposed", ProviderName);
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        Dispose();
        return ValueTask.CompletedTask;
    }

    // Shared JSON response models for /v1/models endpoint
    private sealed class ModelsResponse
    {
        [JsonPropertyName("data")]
        public List<ModelData>? Data { get; set; }
    }

    private sealed class ModelData
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("owned_by")]
        public string? OwnedBy { get; set; }

        [JsonPropertyName("created")]
        public long? Created { get; set; }
    }
}

/// <summary>
/// Common configuration for OpenAI-compatible providers.
/// </summary>
public abstract record OpenAiCompatibleConfiguration
{
    /// <summary>
    /// Model to use for requests.
    /// </summary>
    public abstract string Model { get; init; }

    /// <summary>
    /// Optional system prompt to set the assistant's behavior.
    /// </summary>
    public abstract string? SystemPrompt { get; init; }

    /// <summary>
    /// Gets the full base URL for the API.
    /// </summary>
    public abstract string BaseUrl { get; }

    /// <summary>
    /// Request timeout in seconds.
    /// </summary>
    public abstract int TimeoutSeconds { get; init; }
}

