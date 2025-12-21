using System.ClientModel;
using Microsoft.Extensions.Logging;

namespace LlmPlayground.Core;

/// <summary>
/// Configuration for the Ollama provider.
/// </summary>
public record OllamaConfiguration : OpenAiCompatibleConfiguration
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
    public override string Model { get; init; } = "llama3";

    /// <summary>
    /// Optional system prompt to set the assistant's behavior.
    /// </summary>
    public override string? SystemPrompt { get; init; }

    /// <summary>
    /// Request timeout in seconds.
    /// </summary>
    public override int TimeoutSeconds { get; init; } = 300;

    /// <summary>
    /// Optional full base URL override. If set, Host/Port/Scheme/ApiPath are ignored.
    /// </summary>
    public string? BaseUrlOverride { get; init; }

    /// <summary>
    /// Gets the full base URL for the Ollama API.
    /// </summary>
    public override string BaseUrl => !string.IsNullOrWhiteSpace(BaseUrlOverride)
        ? BaseUrlOverride
        : $"{Scheme}://{Host}:{Port}{ApiPath}";
}

/// <summary>
/// LLM provider implementation for Ollama.
/// Uses the OpenAI-compatible API endpoint.
/// </summary>
public sealed class OllamaProvider : OpenAiCompatibleProviderBase
{
    private OllamaConfiguration _configuration;

    /// <summary>
    /// Initializes a new instance of the <see cref="OllamaProvider"/> class with DI support.
    /// </summary>
    /// <param name="configuration">Ollama configuration.</param>
    /// <param name="httpClient">HttpClient for API calls (from IHttpClientFactory).</param>
    /// <param name="logger">Optional logger instance.</param>
    public OllamaProvider(
        OllamaConfiguration configuration,
        HttpClient httpClient,
        ILogger<OllamaProvider>? logger = null)
        : base(httpClient, logger)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        _configuration = configuration;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OllamaProvider"/> class.
    /// Creates its own HttpClient internally.
    /// </summary>
    /// <param name="configuration">Ollama configuration.</param>
    /// <param name="logger">Optional logger instance.</param>
    public OllamaProvider(OllamaConfiguration configuration, ILogger<OllamaProvider>? logger = null)
        : base(configuration?.BaseUrl ?? throw new ArgumentNullException(nameof(configuration)),
               configuration.TimeoutSeconds,
               logger)
    {
        _configuration = configuration;
    }

    /// <inheritdoc />
    protected override OpenAiCompatibleConfiguration Configuration => _configuration;

    /// <inheritdoc />
    protected override ApiKeyCredential GetCredential() => new("ollama");

    /// <inheritdoc />
    public override string ProviderName => $"Ollama ({_configuration.Model})";

    /// <inheritdoc />
    public override string CurrentModel => _configuration.Model;

    /// <inheritdoc />
    public override void SetModel(string modelId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(modelId);
        _configuration = _configuration with { Model = modelId };
        ResetClient();
        Logger.LogInformation("Ollama model changed to {Model}", modelId);
    }
}
