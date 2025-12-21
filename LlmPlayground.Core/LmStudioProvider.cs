using System.ClientModel;
using Microsoft.Extensions.Logging;

namespace LlmPlayground.Core;

/// <summary>
/// Configuration for the LM Studio provider.
/// </summary>
public record LmStudioConfiguration : OpenAiCompatibleConfiguration
{
    /// <summary>
    /// LM Studio server host (default: localhost).
    /// </summary>
    public string Host { get; init; } = "localhost";

    /// <summary>
    /// LM Studio server port (default: 1234).
    /// </summary>
    public int Port { get; init; } = 1234;

    /// <summary>
    /// URL scheme - "http" or "https" (default: http).
    /// </summary>
    public string Scheme { get; init; } = "http";

    /// <summary>
    /// API base path (default: /v1).
    /// </summary>
    public string ApiPath { get; init; } = "/v1";

    /// <summary>
    /// Model identifier (optional - LM Studio uses the currently loaded model).
    /// </summary>
    public override string Model { get; init; } = "local-model";

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
    /// Gets the full base URL for the LM Studio API.
    /// </summary>
    public override string BaseUrl => !string.IsNullOrWhiteSpace(BaseUrlOverride)
        ? BaseUrlOverride
        : $"{Scheme}://{Host}:{Port}{ApiPath}";
}

/// <summary>
/// LLM provider implementation for LM Studio.
/// Uses the OpenAI-compatible API endpoint.
/// </summary>
public sealed class LmStudioProvider : OpenAiCompatibleProviderBase
{
    private LmStudioConfiguration _configuration;

    /// <summary>
    /// Initializes a new instance of the <see cref="LmStudioProvider"/> class with DI support.
    /// </summary>
    /// <param name="configuration">LM Studio configuration.</param>
    /// <param name="httpClient">HttpClient for API calls (from IHttpClientFactory).</param>
    /// <param name="logger">Optional logger instance.</param>
    public LmStudioProvider(
        LmStudioConfiguration configuration,
        HttpClient httpClient,
        ILogger<LmStudioProvider>? logger = null)
        : base(httpClient, logger)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        _configuration = configuration;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LmStudioProvider"/> class.
    /// Creates its own HttpClient internally.
    /// </summary>
    /// <param name="configuration">LM Studio configuration.</param>
    /// <param name="logger">Optional logger instance.</param>
    public LmStudioProvider(LmStudioConfiguration configuration, ILogger<LmStudioProvider>? logger = null)
        : base(configuration?.BaseUrl ?? throw new ArgumentNullException(nameof(configuration)),
               configuration.TimeoutSeconds,
               logger)
    {
        _configuration = configuration;
    }

    /// <inheritdoc />
    protected override OpenAiCompatibleConfiguration Configuration => _configuration;

    /// <inheritdoc />
    protected override ApiKeyCredential GetCredential() => new("lm-studio");

    /// <inheritdoc />
    public override string ProviderName => $"LM Studio ({_configuration.Model})";

    /// <inheritdoc />
    public override string CurrentModel => _configuration.Model;

    /// <inheritdoc />
    public override void SetModel(string modelId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(modelId);
        _configuration = _configuration with { Model = modelId };
        ResetClient();
        Logger.LogInformation("LM Studio model changed to {Model}", modelId);
    }
}
