using System.ClientModel;
using System.Diagnostics;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using OpenAI;
using OpenAI.Chat;

namespace LlmPlayground.Core;

/// <summary>
/// Configuration for the Ollama provider.
/// </summary>
public record OllamaConfiguration
{
    /// <summary>
    /// Ollama server host (default: localhost).
    /// </summary>
    public string Host { get; init; } = "localhost";

    /// <summary>
    /// Ollama server port (default: 11434).
    /// </summary>
    public int Port { get; init; } = 11434;

    /// <summary>
    /// URL scheme - "http" or "https" (default: http).
    /// </summary>
    public string Scheme { get; init; } = "http";

    /// <summary>
    /// API base path (default: /v1).
    /// </summary>
    public string ApiPath { get; init; } = "/v1";

    /// <summary>
    /// Model to use (e.g., "llama3", "mistral", "codellama", "phi3").
    /// </summary>
    public string Model { get; init; } = "llama3";

    /// <summary>
    /// Optional system prompt to set the assistant's behavior.
    /// </summary>
    public string? SystemPrompt { get; init; }

    /// <summary>
    /// Request timeout in seconds.
    /// </summary>
    public int TimeoutSeconds { get; init; } = 300;

    /// <summary>
    /// Optional full base URL override. If set, Host/Port/Scheme/ApiPath are ignored.
    /// </summary>
    public string? BaseUrlOverride { get; init; }

    /// <summary>
    /// Gets the full base URL for the Ollama API.
    /// </summary>
    public string BaseUrl => !string.IsNullOrWhiteSpace(BaseUrlOverride)
        ? BaseUrlOverride
        : $"{Scheme}://{Host}:{Port}{ApiPath}";
}

/// <summary>
/// LLM provider implementation for Ollama.
/// Uses the OpenAI-compatible API endpoint.
/// </summary>
public sealed class OllamaProvider : ILlmProvider, IModelListingProvider
{
    private OllamaConfiguration _configuration;
    private readonly HttpClient _httpClient;
    private ChatClient? _client;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="OllamaProvider"/> class.
    /// </summary>
    /// <param name="configuration">Ollama configuration.</param>
    public OllamaProvider(OllamaConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        _configuration = configuration;

        // Ensure base URL ends with "/" for proper relative URI resolution
        var baseUrl = _configuration.BaseUrl;
        if (!baseUrl.EndsWith('/'))
            baseUrl += "/";

        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(baseUrl),
            Timeout = TimeSpan.FromSeconds(_configuration.TimeoutSeconds)
        };
    }

    /// <inheritdoc />
    public string ProviderName => $"Ollama ({_configuration.Model})";

    /// <inheritdoc />
    public bool IsReady => _client != null && !_disposed;

    /// <inheritdoc />
    public string CurrentModel => _configuration.Model;

    /// <inheritdoc />
    public async Task<IReadOnlyList<LlmModelInfo>> GetAvailableModelsAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            var response = await _httpClient.GetFromJsonAsync<ModelsResponse>("models", cancellationToken);
            if (response?.Data == null)
                return Array.Empty<LlmModelInfo>();

            return response.Data
                .Select(m => new LlmModelInfo(
                    m.Id,
                    m.OwnedBy,
                    m.Created.HasValue ? DateTimeOffset.FromUnixTimeSeconds(m.Created.Value).DateTime : null))
                .ToList();
        }
        catch (HttpRequestException)
        {
            return Array.Empty<LlmModelInfo>();
        }
    }

    /// <inheritdoc />
    public void SetModel(string modelId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(modelId);
        _configuration = _configuration with { Model = modelId };
        _client = null; // Force re-initialization with new model
    }

    /// <inheritdoc />
    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        if (_client != null)
            return Task.CompletedTask;

        if (string.IsNullOrWhiteSpace(_configuration.Model))
            throw new InvalidOperationException("Model name is not set. Call SetModel or configure a model first.");

        var options = new OpenAIClientOptions
        {
            Endpoint = new Uri(_configuration.BaseUrl)
        };

        // Ollama doesn't require an API key, but the client needs one
        var credential = new ApiKeyCredential("ollama");
        var openAiClient = new OpenAIClient(credential, options);
        _client = openAiClient.GetChatClient(_configuration.Model);

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

        var stopwatch = Stopwatch.StartNew();
        var response = await _client!.CompleteChatAsync(messages, chatOptions, cancellationToken);
        stopwatch.Stop();

        var completion = response.Value;
        var text = completion.Content[0].Text ?? string.Empty;
        var tokensGenerated = completion.Usage?.OutputTokenCount ?? 0;

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
        IReadOnlyList<Core.ChatMessage> messages,
        LlmInferenceOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ThrowIfNotInitialized();

        options ??= new LlmInferenceOptions();
        var chatMessages = ConvertMessages(messages);
        var chatOptions = BuildChatOptions(options);

        var stopwatch = Stopwatch.StartNew();
        var response = await _client!.CompleteChatAsync(chatMessages, chatOptions, cancellationToken);
        stopwatch.Stop();

        var completion = response.Value;
        var text = completion.Content[0].Text ?? string.Empty;
        var tokensGenerated = completion.Usage?.OutputTokenCount ?? 0;

        return new LlmCompletionResult(text, tokensGenerated, stopwatch.Elapsed);
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<string> ChatStreamingAsync(
        IReadOnlyList<Core.ChatMessage> messages,
        LlmInferenceOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ThrowIfNotInitialized();

        options ??= new LlmInferenceOptions();
        var chatMessages = ConvertMessages(messages);
        var chatOptions = BuildChatOptions(options);

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

        if (!string.IsNullOrWhiteSpace(_configuration.SystemPrompt))
        {
            messages.Add(new SystemChatMessage(_configuration.SystemPrompt));
        }

        messages.Add(new UserChatMessage(prompt));
        return messages;
    }

    private List<OpenAI.Chat.ChatMessage> ConvertMessages(IReadOnlyList<Core.ChatMessage> messages)
    {
        var result = new List<OpenAI.Chat.ChatMessage>();

        // Add system prompt if configured and not already in messages
        if (!string.IsNullOrWhiteSpace(_configuration.SystemPrompt) &&
            !messages.Any(m => m.Role == ChatRole.System))
        {
            result.Add(new SystemChatMessage(_configuration.SystemPrompt));
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

    private void ThrowIfDisposed()
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

        _httpClient.Dispose();
        _client = null;
        _disposed = true;
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        Dispose();
        return ValueTask.CompletedTask;
    }

    // JSON response models for /v1/models endpoint
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

