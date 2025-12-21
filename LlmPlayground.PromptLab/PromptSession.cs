using System.Diagnostics;
using System.Text;
using LlmPlayground.Core;

namespace LlmPlayground.PromptLab;

/// <summary>
/// Manages a prompt engineering session with conversation history and streaming support.
/// </summary>
public class PromptSession : IDisposable
{
    private readonly ILlmProvider _provider;
    private readonly List<PromptExchange> _history = [];
    private bool _disposed;

    /// <summary>
    /// Gets or sets the system prompt for this session.
    /// </summary>
    public string? SystemPrompt { get; set; }

    /// <summary>
    /// Gets or sets the inference options for this session.
    /// </summary>
    public LlmInferenceOptions Options { get; set; } = new();

    /// <summary>
    /// Gets the conversation history.
    /// </summary>
    public IReadOnlyList<PromptExchange> History => _history.AsReadOnly();

    /// <summary>
    /// Gets the provider being used.
    /// </summary>
    public ILlmProvider Provider => _provider;

    /// <summary>
    /// Creates a new prompt session.
    /// </summary>
    /// <param name="provider">The LLM provider to use.</param>
    /// <param name="systemPrompt">Optional system prompt.</param>
    /// <param name="options">Optional inference options.</param>
    public PromptSession(ILlmProvider provider, string? systemPrompt = null, LlmInferenceOptions? options = null)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        SystemPrompt = systemPrompt;
        Options = options ?? new LlmInferenceOptions();
    }

    /// <summary>
    /// Sends a prompt and gets a complete response.
    /// </summary>
    /// <param name="prompt">The user prompt.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The prompt result with response and metadata.</returns>
    public async Task<PromptResult> SendAsync(string prompt, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrWhiteSpace(prompt);

        var messages = BuildMessages(prompt);
        var stopwatch = Stopwatch.StartNew();

        var result = await _provider.ChatAsync(messages, Options, cancellationToken);
        stopwatch.Stop();

        var promptResult = new PromptResult
        {
            Prompt = prompt,
            Response = result.Text,
            TokensGenerated = result.TokensGenerated,
            Duration = stopwatch.Elapsed,
            Success = true
        };

        _history.Add(new PromptExchange(prompt, result.Text, DateTime.UtcNow));

        return promptResult;
    }

    /// <summary>
    /// Sends a prompt and streams the response token by token.
    /// </summary>
    /// <param name="prompt">The user prompt.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An async enumerable of response tokens.</returns>
    public async IAsyncEnumerable<string> SendStreamingAsync(
        string prompt, 
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrWhiteSpace(prompt);

        var messages = BuildMessages(prompt);
        var responseBuilder = new StringBuilder();

        await foreach (var token in _provider.ChatStreamingAsync(messages, Options, cancellationToken))
        {
            responseBuilder.Append(token);
            yield return token;
        }

        _history.Add(new PromptExchange(prompt, responseBuilder.ToString(), DateTime.UtcNow));
    }

    /// <summary>
    /// Sends a prompt with streaming and collects the full result.
    /// </summary>
    /// <param name="prompt">The user prompt.</param>
    /// <param name="onToken">Callback invoked for each token received.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The complete prompt result.</returns>
    public async Task<PromptResult> SendWithCallbackAsync(
        string prompt,
        Action<string> onToken,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrWhiteSpace(prompt);

        var messages = BuildMessages(prompt);
        var responseBuilder = new StringBuilder();
        var tokenCount = 0;
        var stopwatch = Stopwatch.StartNew();

        await foreach (var token in _provider.ChatStreamingAsync(messages, Options, cancellationToken))
        {
            responseBuilder.Append(token);
            tokenCount++;
            onToken(token);
        }

        stopwatch.Stop();

        var response = responseBuilder.ToString();
        _history.Add(new PromptExchange(prompt, response, DateTime.UtcNow));

        return new PromptResult
        {
            Prompt = prompt,
            Response = response,
            TokensGenerated = tokenCount,
            Duration = stopwatch.Elapsed,
            Success = true
        };
    }

    /// <summary>
    /// Retries the last prompt.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The new prompt result.</returns>
    public Task<PromptResult> RetryLastAsync(CancellationToken cancellationToken = default)
    {
        if (_history.Count == 0)
            throw new InvalidOperationException("No previous prompt to retry.");

        return SendAsync(_history[^1].Prompt, cancellationToken);
    }

    /// <summary>
    /// Clears the conversation history.
    /// </summary>
    public void ClearHistory()
    {
        _history.Clear();
    }

    /// <summary>
    /// Gets the last exchange, if any.
    /// </summary>
    public PromptExchange? LastExchange => _history.Count > 0 ? _history[^1] : null;

    private List<ChatMessage> BuildMessages(string prompt)
    {
        var messages = new List<ChatMessage>();

        if (!string.IsNullOrWhiteSpace(SystemPrompt))
        {
            messages.Add(new ChatMessage(ChatRole.System, SystemPrompt));
        }

        // Include conversation history for context
        foreach (var exchange in _history)
        {
            messages.Add(new ChatMessage(ChatRole.User, exchange.Prompt));
            messages.Add(new ChatMessage(ChatRole.Assistant, exchange.Response));
        }

        messages.Add(new ChatMessage(ChatRole.User, prompt));
        return messages;
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Represents the result of a prompt execution.
/// </summary>
public class PromptResult
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
    /// Whether the prompt executed successfully.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Error message if the prompt failed.
    /// </summary>
    public string? Error { get; init; }

    /// <summary>
    /// Tokens per second.
    /// </summary>
    public double TokensPerSecond => Duration.TotalSeconds > 0 
        ? TokensGenerated / Duration.TotalSeconds 
        : 0;
}

/// <summary>
/// Represents a prompt and response exchange.
/// </summary>
public record PromptExchange(string Prompt, string Response, DateTime Timestamp);
