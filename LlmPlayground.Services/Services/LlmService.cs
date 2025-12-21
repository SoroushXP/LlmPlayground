using System.Diagnostics;
using System.Runtime.CompilerServices;
using LlmPlayground.Core;
using LlmPlayground.Services.Interfaces;
using LlmPlayground.Services.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LlmPlayground.Services.Services;

/// <summary>
/// Service implementation for interacting with LLM providers.
/// </summary>
public sealed class LlmService : ILlmService, IAsyncDisposable, IDisposable
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<LlmService> _logger;
    private ILlmProvider? _currentProvider;
    private LlmProviderType _currentProviderType = LlmProviderType.Ollama;
    private bool _disposed;

    private static readonly IReadOnlyList<string> AvailableProviderNames = 
        ["Ollama", "LmStudio", "OpenAI"];

    public LlmService(IConfiguration configuration, ILogger<LlmService> logger)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public IReadOnlyList<string> GetAvailableProviders() => AvailableProviderNames;

    /// <inheritdoc />
    public string CurrentProvider => _currentProviderType.ToString();

    /// <inheritdoc />
    public bool IsReady => _currentProvider?.IsReady ?? false;

    /// <inheritdoc />
    public string? CurrentModel => (_currentProvider as IModelListingProvider)?.CurrentModel;

    /// <inheritdoc />
    public async Task SetProviderAsync(LlmProviderType providerType, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        if (_currentProvider != null)
        {
            await _currentProvider.DisposeAsync();
            _currentProvider = null;
        }

        _currentProvider = CreateProvider(providerType);
        _currentProviderType = providerType;

        await _currentProvider.InitializeAsync(cancellationToken);
        _logger.LogInformation("Switched to provider: {Provider}", providerType);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ModelInfoDto>> GetAvailableModelsAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        await EnsureProviderInitializedAsync(cancellationToken);

        if (_currentProvider is not IModelListingProvider modelListing)
        {
            _logger.LogDebug("Current provider does not support model listing");
            return [];
        }

        var models = await modelListing.GetAvailableModelsAsync(cancellationToken);
        return models.Select(m => new ModelInfoDto
        {
            Id = m.Id,
            OwnedBy = m.OwnedBy,
            Created = m.Created
        }).ToList();
    }

    /// <inheritdoc />
    public void SetModel(string modelId)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrWhiteSpace(modelId);

        if (_currentProvider is IModelListingProvider modelListing)
        {
            modelListing.SetModel(modelId);
            _logger.LogInformation("Model set to: {Model}", modelId);
        }
        else
        {
            throw new InvalidOperationException("Current provider does not support model selection.");
        }
    }

    /// <inheritdoc />
    public async Task<CompletionResponse> CompleteAsync(CompletionRequest request, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(request);
        await EnsureProviderInitializedAsync(cancellationToken);

        var options = MapInferenceOptions(request.Options);
        var stopwatch = Stopwatch.StartNew();

        var result = await _currentProvider!.CompleteAsync(request.Prompt, options, cancellationToken);
        stopwatch.Stop();

        return new CompletionResponse
        {
            Text = result.Text,
            TokensGenerated = result.TokensGenerated,
            Duration = stopwatch.Elapsed
        };
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<string> CompleteStreamingAsync(
        CompletionRequest request, 
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(request);
        await EnsureProviderInitializedAsync(cancellationToken);

        var options = MapInferenceOptions(request.Options);

        await foreach (var token in _currentProvider!.CompleteStreamingAsync(request.Prompt, options, cancellationToken))
        {
            yield return token;
        }
    }

    /// <inheritdoc />
    public async Task<CompletionResponse> ChatAsync(ChatRequest request, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(request);
        await EnsureProviderInitializedAsync(cancellationToken);

        var messages = MapMessages(request.Messages);
        var options = MapInferenceOptions(request.Options);
        var stopwatch = Stopwatch.StartNew();

        var result = await _currentProvider!.ChatAsync(messages, options, cancellationToken);
        stopwatch.Stop();

        return new CompletionResponse
        {
            Text = result.Text,
            TokensGenerated = result.TokensGenerated,
            Duration = stopwatch.Elapsed
        };
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<string> ChatStreamingAsync(
        ChatRequest request, 
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(request);
        await EnsureProviderInitializedAsync(cancellationToken);

        var messages = MapMessages(request.Messages);
        var options = MapInferenceOptions(request.Options);

        await foreach (var token in _currentProvider!.ChatStreamingAsync(messages, options, cancellationToken))
        {
            yield return token;
        }
    }

    private ILlmProvider CreateProvider(LlmProviderType providerType)
    {
        return providerType switch
        {
            LlmProviderType.Ollama => CreateOllamaProvider(),
            LlmProviderType.LmStudio => CreateLmStudioProvider(),
            LlmProviderType.OpenAI => CreateOpenAiProvider(),
            _ => throw new ArgumentException($"Unknown provider type: {providerType}")
        };
    }

    private OllamaProvider CreateOllamaProvider()
    {
        var section = _configuration.GetSection("Ollama");
        return new OllamaProvider(new OllamaConfiguration
        {
            Host = section["Host"] ?? "localhost",
            Port = section.GetValue("Port", 11434),
            Scheme = section["Scheme"] ?? "http",
            ApiPath = section["ApiPath"] ?? "/v1",
            Model = section["Model"] ?? "llama3",
            SystemPrompt = section["SystemPrompt"],
            TimeoutSeconds = section.GetValue("TimeoutSeconds", 300),
            BaseUrlOverride = string.IsNullOrWhiteSpace(section["BaseUrlOverride"])
                ? null
                : section["BaseUrlOverride"]
        });
    }

    private LmStudioProvider CreateLmStudioProvider()
    {
        var section = _configuration.GetSection("LmStudio");
        return new LmStudioProvider(new LmStudioConfiguration
        {
            Host = section["Host"] ?? "localhost",
            Port = section.GetValue("Port", 1234),
            Scheme = section["Scheme"] ?? "http",
            ApiPath = section["ApiPath"] ?? "/v1",
            Model = section["Model"] ?? "local-model",
            SystemPrompt = section["SystemPrompt"],
            TimeoutSeconds = section.GetValue("TimeoutSeconds", 300),
            BaseUrlOverride = string.IsNullOrWhiteSpace(section["BaseUrlOverride"])
                ? null
                : section["BaseUrlOverride"]
        });
    }

    private OpenAiProvider CreateOpenAiProvider()
    {
        var section = _configuration.GetSection("OpenAI");
        var apiKey = section["ApiKey"];

        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("OpenAI API key is required. Set 'OpenAI:ApiKey' in configuration.");

        return new OpenAiProvider(new OpenAiConfiguration
        {
            ApiKey = apiKey,
            Model = section["Model"] ?? "gpt-4o-mini",
            SystemPrompt = section["SystemPrompt"],
            BaseUrlOverride = string.IsNullOrWhiteSpace(section["BaseUrl"])
                ? null
                : section["BaseUrl"],
            TimeoutSeconds = section.GetValue("TimeoutSeconds", 120)
        });
    }

    private async Task EnsureProviderInitializedAsync(CancellationToken cancellationToken)
    {
        if (_currentProvider == null)
        {
            await SetProviderAsync(_currentProviderType, cancellationToken);
        }
        else if (!_currentProvider.IsReady)
        {
            await _currentProvider.InitializeAsync(cancellationToken);
        }
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

    private static List<ChatMessage> MapMessages(IReadOnlyList<ChatMessageDto> dtos)
    {
        return dtos.Select(dto => new ChatMessage(
            dto.Role.ToLowerInvariant() switch
            {
                "system" => ChatRole.System,
                "user" => ChatRole.User,
                "assistant" => ChatRole.Assistant,
                _ => ChatRole.User
            },
            dto.Content
        )).ToList();
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        if (_currentProvider != null)
        {
            await _currentProvider.DisposeAsync();
            _currentProvider = null;
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _currentProvider?.Dispose();
        _currentProvider = null;
    }
}

