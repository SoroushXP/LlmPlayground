using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using LlmPlayground.Core;
using LlmPlayground.PromptLab;
using LlmPlayground.Services.Interfaces;
using LlmPlayground.Services.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LlmPlayground.Services.Services;

/// <summary>
/// Service implementation for managing prompt engineering sessions.
/// </summary>
public sealed class PromptLabService : IPromptLabService, IDisposable
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<PromptLabService> _logger;
    private readonly ConcurrentDictionary<string, SessionEntry> _sessions = new();
    private bool _disposed;

    private sealed class SessionEntry : IDisposable
    {
        public required PromptSession Session { get; init; }
        public required ILlmProvider Provider { get; init; }
        public required string ProviderName { get; init; }

        public void Dispose()
        {
            Session.Dispose();
            Provider.Dispose();
        }
    }

    public PromptLabService(IConfiguration configuration, ILogger<PromptLabService> logger)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<SessionCreatedResponse> CreateSessionAsync(CreateSessionRequest request, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(request);

        var sessionId = Guid.NewGuid().ToString("N")[..12];
        var providerName = request.Provider.ToString();

        _logger.LogInformation("Creating session {SessionId} with provider {Provider}", sessionId, providerName);

        var provider = CreateProvider(request.Provider);
        await provider.InitializeAsync(cancellationToken);

        var options = MapInferenceOptions(request.Options);
        var session = new PromptSession(provider, request.SystemPrompt, options);

        var entry = new SessionEntry
        {
            Session = session,
            Provider = provider,
            ProviderName = providerName
        };

        if (!_sessions.TryAdd(sessionId, entry))
        {
            entry.Dispose();
            throw new InvalidOperationException("Failed to create session - ID collision");
        }

        return new SessionCreatedResponse
        {
            SessionId = sessionId,
            Provider = providerName
        };
    }

    /// <inheritdoc />
    public SessionInfoResponse? GetSession(string sessionId)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);

        if (!_sessions.TryGetValue(sessionId, out var entry))
        {
            return null;
        }

        return new SessionInfoResponse
        {
            SessionId = sessionId,
            Provider = entry.ProviderName,
            SystemPrompt = entry.Session.SystemPrompt,
            HistoryCount = entry.Session.History.Count,
            History = entry.Session.History.Select(h => new PromptExchangeDto
            {
                Prompt = h.Prompt,
                Response = h.Response,
                Timestamp = h.Timestamp
            }).ToList()
        };
    }

    /// <inheritdoc />
    public IReadOnlyList<string> GetActiveSessions()
    {
        ThrowIfDisposed();
        return _sessions.Keys.ToList();
    }

    /// <inheritdoc />
    public async Task<PromptResponse> SendPromptAsync(string sessionId, SendPromptRequest request, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        ArgumentNullException.ThrowIfNull(request);

        var entry = GetSessionEntry(sessionId);

        _logger.LogDebug("Sending prompt in session {SessionId}", sessionId);

        try
        {
            var result = await entry.Session.SendAsync(request.Prompt, cancellationToken);

            return new PromptResponse
            {
                Prompt = result.Prompt,
                Response = result.Response,
                TokensGenerated = result.TokensGenerated,
                Duration = result.Duration,
                TokensPerSecond = result.TokensPerSecond,
                Success = result.Success,
                Error = result.Error
            };
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Error sending prompt in session {SessionId}", sessionId);
            return new PromptResponse
            {
                Prompt = request.Prompt,
                Response = string.Empty,
                Success = false,
                Error = ex.Message
            };
        }
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<string> SendPromptStreamingAsync(
        string sessionId, 
        SendPromptRequest request, 
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        ArgumentNullException.ThrowIfNull(request);

        var entry = GetSessionEntry(sessionId);

        _logger.LogDebug("Streaming prompt in session {SessionId}", sessionId);

        await foreach (var token in entry.Session.SendStreamingAsync(request.Prompt, cancellationToken))
        {
            yield return token;
        }
    }

    /// <inheritdoc />
    public async Task<PromptResponse> RetryLastAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);

        var entry = GetSessionEntry(sessionId);

        _logger.LogDebug("Retrying last prompt in session {SessionId}", sessionId);

        try
        {
            var result = await entry.Session.RetryLastAsync(cancellationToken);

            return new PromptResponse
            {
                Prompt = result.Prompt,
                Response = result.Response,
                TokensGenerated = result.TokensGenerated,
                Duration = result.Duration,
                TokensPerSecond = result.TokensPerSecond,
                Success = result.Success,
                Error = result.Error
            };
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("No previous prompt to retry in session {SessionId}", sessionId);
            return new PromptResponse
            {
                Prompt = string.Empty,
                Response = string.Empty,
                Success = false,
                Error = ex.Message
            };
        }
    }

    /// <inheritdoc />
    public bool ClearSessionHistory(string sessionId)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);

        if (!_sessions.TryGetValue(sessionId, out var entry))
        {
            return false;
        }

        entry.Session.ClearHistory();
        _logger.LogDebug("Cleared history for session {SessionId}", sessionId);
        return true;
    }

    /// <inheritdoc />
    public bool CloseSession(string sessionId)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);

        if (!_sessions.TryRemove(sessionId, out var entry))
        {
            return false;
        }

        entry.Dispose();
        _logger.LogInformation("Closed session {SessionId}", sessionId);
        return true;
    }

    /// <inheritdoc />
    public RenderTemplateResponse RenderTemplate(RenderTemplateRequest request)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Template);
        ArgumentNullException.ThrowIfNull(request.Variables);

        var template = new PromptTemplate(request.Template);
        var missing = template.GetMissingVariables(request.Variables);
        var rendered = template.Render(request.Variables);

        return new RenderTemplateResponse
        {
            RenderedPrompt = rendered,
            Variables = template.Variables.ToList(),
            MissingVariables = missing
        };
    }

    /// <inheritdoc />
    public IReadOnlyList<string> GetTemplateVariables(string template)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrWhiteSpace(template);

        var promptTemplate = new PromptTemplate(template);
        return promptTemplate.Variables.ToList();
    }

    private SessionEntry GetSessionEntry(string sessionId)
    {
        if (!_sessions.TryGetValue(sessionId, out var entry))
        {
            throw new KeyNotFoundException($"Session not found: {sessionId}");
        }
        return entry;
    }

    private ILlmProvider CreateProvider(LlmProviderType providerType)
    {
        return providerType switch
        {
            LlmProviderType.Ollama => PromptLabFactory.CreateOllamaProvider(_configuration),
            LlmProviderType.LmStudio => PromptLabFactory.CreateLmStudioProvider(_configuration),
            LlmProviderType.OpenAI => PromptLabFactory.CreateOpenAiProvider(_configuration),
            _ => throw new ArgumentException($"Unknown provider type: {providerType}")
        };
    }

    private static LlmInferenceOptions? MapInferenceOptions(InferenceOptionsDto? dto)
    {
        if (dto == null) return null;

        return new LlmInferenceOptions
        {
            MaxTokens = dto.MaxTokens ?? 256,
            Temperature = dto.Temperature ?? 0.7f,
            TopP = dto.TopP ?? 0.9f,
            RepeatPenalty = dto.RepeatPenalty ?? 1.1f
        };
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

        foreach (var entry in _sessions.Values)
        {
            entry.Dispose();
        }
        _sessions.Clear();
    }
}

