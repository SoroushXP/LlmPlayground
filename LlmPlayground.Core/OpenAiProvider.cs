using System.ClientModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using OpenAI;
using OpenAI.Chat;

namespace LlmPlayground.Core;

/// <summary>
/// Configuration for the OpenAI provider.
/// </summary>
public record OpenAiConfiguration
{
    /// <summary>
    /// OpenAI API key.
    /// </summary>
    public required string ApiKey { get; init; }

    /// <summary>
    /// Model to use (e.g., "gpt-4", "gpt-4o", "gpt-3.5-turbo").
    /// </summary>
    public string Model { get; init; } = "gpt-4o-mini";

    /// <summary>
    /// Optional system prompt to set the assistant's behavior.
    /// </summary>
    public string? SystemPrompt { get; init; }

    /// <summary>
    /// Optional base URL for API endpoint (for Azure OpenAI or proxies).
    /// </summary>
    public string? BaseUrl { get; init; }

    /// <summary>
    /// Request timeout in seconds.
    /// </summary>
    public int TimeoutSeconds { get; init; } = 120;
}

/// <summary>
/// LLM provider implementation for OpenAI (ChatGPT) API.
/// </summary>
public sealed class OpenAiProvider : ILlmProvider
{
    private readonly OpenAiConfiguration _configuration;
    private ChatClient? _client;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="OpenAiProvider"/> class.
    /// </summary>
    /// <param name="configuration">OpenAI configuration.</param>
    public OpenAiProvider(OpenAiConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        if (string.IsNullOrWhiteSpace(configuration.ApiKey))
            throw new ArgumentException("API key cannot be empty.", nameof(configuration));

        _configuration = configuration;
    }

    /// <inheritdoc />
    public string ProviderName => $"OpenAI ({_configuration.Model})";

    /// <inheritdoc />
    public bool IsReady => _client != null && !_disposed;

    /// <inheritdoc />
    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        if (_client != null)
            return Task.CompletedTask;

        var options = new OpenAIClientOptions();

        if (!string.IsNullOrWhiteSpace(_configuration.BaseUrl))
        {
            options.Endpoint = new Uri(_configuration.BaseUrl);
        }

        var credential = new ApiKeyCredential(_configuration.ApiKey);
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

    private ChatCompletionOptions BuildChatOptions(LlmInferenceOptions options)
    {
        return new ChatCompletionOptions
        {
            MaxOutputTokenCount = options.MaxTokens,
            Temperature = options.Temperature,
            TopP = options.TopP,
            FrequencyPenalty = options.RepeatPenalty - 1.0f // Convert from repeat penalty to frequency penalty
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

        _client = null;
        _disposed = true;
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        Dispose();
        return ValueTask.CompletedTask;
    }
}

