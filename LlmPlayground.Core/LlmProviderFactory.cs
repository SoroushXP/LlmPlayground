using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace LlmPlayground.Core;

/// <summary>
/// Factory implementation for creating LLM provider instances.
/// </summary>
public sealed class LlmProviderFactory : ILlmProviderFactory
{
    private readonly IOptions<OllamaConfiguration>? _ollamaOptions;
    private readonly IOptions<LmStudioConfiguration>? _lmStudioOptions;
    private readonly IOptions<OpenAiConfiguration>? _openAiOptions;
    private readonly IOptions<LocalLlmConfiguration>? _localLlmOptions;
    private readonly IHttpClientFactory? _httpClientFactory;
    private readonly ILoggerFactory _loggerFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="LlmProviderFactory"/> class.
    /// </summary>
    public LlmProviderFactory(
        IOptions<OllamaConfiguration>? ollamaOptions = null,
        IOptions<LmStudioConfiguration>? lmStudioOptions = null,
        IOptions<OpenAiConfiguration>? openAiOptions = null,
        IOptions<LocalLlmConfiguration>? localLlmOptions = null,
        IHttpClientFactory? httpClientFactory = null,
        ILoggerFactory? loggerFactory = null)
    {
        _ollamaOptions = ollamaOptions;
        _lmStudioOptions = lmStudioOptions;
        _openAiOptions = openAiOptions;
        _localLlmOptions = localLlmOptions;
        _httpClientFactory = httpClientFactory;
        _loggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
    }

    /// <inheritdoc />
    public IReadOnlyList<LlmProviderType> AvailableProviders
    {
        get
        {
            var providers = new List<LlmProviderType>();

            if (IsProviderAvailable(LlmProviderType.Ollama))
                providers.Add(LlmProviderType.Ollama);

            if (IsProviderAvailable(LlmProviderType.LmStudio))
                providers.Add(LlmProviderType.LmStudio);

            if (IsProviderAvailable(LlmProviderType.OpenAI))
                providers.Add(LlmProviderType.OpenAI);

            if (IsProviderAvailable(LlmProviderType.Local))
                providers.Add(LlmProviderType.Local);

            return providers;
        }
    }

    /// <inheritdoc />
    public OllamaProvider CreateOllamaProvider(OllamaConfiguration? configuration = null)
    {
        var config = configuration ?? _ollamaOptions?.Value;

        if (config is null)
            throw new InvalidOperationException(
                "Ollama configuration is not available. Either provide a configuration or register OllamaConfiguration in DI.");

        var logger = _loggerFactory.CreateLogger<OllamaProvider>();

        // Use IHttpClientFactory if available, otherwise let the provider create its own
        if (_httpClientFactory != null)
        {
            var httpClient = _httpClientFactory.CreateClient("Ollama");
            ConfigureHttpClient(httpClient, config.BaseUrl, config.TimeoutSeconds);
            return new OllamaProvider(config, httpClient, logger);
        }

        return new OllamaProvider(config, logger);
    }

    /// <inheritdoc />
    public LmStudioProvider CreateLmStudioProvider(LmStudioConfiguration? configuration = null)
    {
        var config = configuration ?? _lmStudioOptions?.Value;

        if (config is null)
            throw new InvalidOperationException(
                "LM Studio configuration is not available. Either provide a configuration or register LmStudioConfiguration in DI.");

        var logger = _loggerFactory.CreateLogger<LmStudioProvider>();

        // Use IHttpClientFactory if available, otherwise let the provider create its own
        if (_httpClientFactory != null)
        {
            var httpClient = _httpClientFactory.CreateClient("LmStudio");
            ConfigureHttpClient(httpClient, config.BaseUrl, config.TimeoutSeconds);
            return new LmStudioProvider(config, httpClient, logger);
        }

        return new LmStudioProvider(config, logger);
    }

    /// <inheritdoc />
    public OpenAiProvider CreateOpenAiProvider(OpenAiConfiguration? configuration = null)
    {
        var config = configuration ?? _openAiOptions?.Value;

        if (config is null)
            throw new InvalidOperationException(
                "OpenAI configuration is not available. Either provide a configuration or register OpenAiConfiguration in DI.");

        var logger = _loggerFactory.CreateLogger<OpenAiProvider>();

        // Use IHttpClientFactory if available, otherwise let the provider create its own
        if (_httpClientFactory != null)
        {
            var httpClient = _httpClientFactory.CreateClient("OpenAI");
            ConfigureHttpClient(httpClient, config.BaseUrl, config.TimeoutSeconds);
            return new OpenAiProvider(config, httpClient, logger);
        }

        return new OpenAiProvider(config, logger);
    }

    /// <inheritdoc />
    public LocalLlmProvider CreateLocalProvider(LocalLlmConfiguration? configuration = null)
    {
        var config = configuration ?? _localLlmOptions?.Value;

        if (config is null)
            throw new InvalidOperationException(
                "Local LLM configuration is not available. Either provide a configuration or register LocalLlmConfiguration in DI.");

        var logger = _loggerFactory.CreateLogger<LocalLlmProvider>();
        return new LocalLlmProvider(config, logger);
    }

    /// <inheritdoc />
    public ILlmProvider CreateProvider(LlmProviderType providerType)
    {
        return providerType switch
        {
            LlmProviderType.Ollama => CreateOllamaProvider(),
            LlmProviderType.LmStudio => CreateLmStudioProvider(),
            LlmProviderType.OpenAI => CreateOpenAiProvider(),
            LlmProviderType.Local => CreateLocalProvider(),
            _ => throw new ArgumentOutOfRangeException(nameof(providerType), providerType, "Unknown provider type.")
        };
    }

    /// <inheritdoc />
    public bool IsProviderAvailable(LlmProviderType providerType)
    {
        return providerType switch
        {
            LlmProviderType.Ollama => _ollamaOptions?.Value is not null,
            LlmProviderType.LmStudio => _lmStudioOptions?.Value is not null,
            LlmProviderType.OpenAI => _openAiOptions?.Value is not null &&
                                      !string.IsNullOrWhiteSpace(_openAiOptions.Value.ApiKey),
            LlmProviderType.Local => _localLlmOptions?.Value is not null &&
                                     !string.IsNullOrWhiteSpace(_localLlmOptions.Value.ModelPath),
            _ => false
        };
    }

    private static void ConfigureHttpClient(HttpClient httpClient, string baseUrl, int timeoutSeconds)
    {
        var url = baseUrl.EndsWith('/') ? baseUrl : baseUrl + "/";
        httpClient.BaseAddress = new Uri(url);
        httpClient.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
    }
}
