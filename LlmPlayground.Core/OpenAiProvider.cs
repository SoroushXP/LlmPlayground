using System.ClientModel;
using Microsoft.Extensions.Logging;

namespace LlmPlayground.Core;

/// <summary>
/// Configuration for the OpenAI provider.
/// </summary>
public record OpenAiConfiguration : OpenAiCompatibleConfiguration
{
    private const string DefaultBaseUrl = "https://api.openai.com/v1";

    /// <summary>
    /// OpenAI API key.
    /// </summary>
    public required string ApiKey { get; init; }

    /// <summary>
    /// Model to use (e.g., "gpt-4", "gpt-4o", "gpt-3.5-turbo").
    /// </summary>
    public override string Model { get; init; } = "gpt-4o-mini";

    /// <summary>
    /// Optional system prompt to set the assistant's behavior.
    /// </summary>
    public override string? SystemPrompt { get; init; }

    /// <summary>
    /// Optional base URL for API endpoint (for Azure OpenAI or proxies).
    /// </summary>
    public string? BaseUrlOverride { get; init; }

    /// <summary>
    /// Request timeout in seconds.
    /// </summary>
    public override int TimeoutSeconds { get; init; } = 120;

    /// <summary>
    /// Gets the full base URL for the OpenAI API.
    /// </summary>
    public override string BaseUrl => !string.IsNullOrWhiteSpace(BaseUrlOverride)
        ? BaseUrlOverride
        : DefaultBaseUrl;
}

/// <summary>
/// LLM provider implementation for OpenAI (ChatGPT) API.
/// </summary>
public sealed class OpenAiProvider : OpenAiCompatibleProviderBase
{
    private readonly OpenAiConfiguration _configuration;

    /// <summary>
    /// Initializes a new instance of the <see cref="OpenAiProvider"/> class with DI support.
    /// </summary>
    /// <param name="configuration">OpenAI configuration.</param>
    /// <param name="httpClient">HttpClient for API calls (from IHttpClientFactory).</param>
    /// <param name="logger">Optional logger instance.</param>
    public OpenAiProvider(
        OpenAiConfiguration configuration,
        HttpClient httpClient,
        ILogger<OpenAiProvider>? logger = null)
        : base(httpClient, logger)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ValidateConfiguration(configuration);
        _configuration = configuration;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OpenAiProvider"/> class.
    /// Creates its own HttpClient internally.
    /// </summary>
    /// <param name="configuration">OpenAI configuration.</param>
    /// <param name="logger">Optional logger instance.</param>
    public OpenAiProvider(OpenAiConfiguration configuration, ILogger<OpenAiProvider>? logger = null)
        : base(configuration?.BaseUrl ?? throw new ArgumentNullException(nameof(configuration)),
               configuration.TimeoutSeconds,
               logger)
    {
        ValidateConfiguration(configuration);
        _configuration = configuration;
    }

    /// <inheritdoc />
    protected override OpenAiCompatibleConfiguration Configuration => _configuration;

    /// <inheritdoc />
    protected override ApiKeyCredential GetCredential() => new(_configuration.ApiKey);

    /// <inheritdoc />
    public override string ProviderName => $"OpenAI ({_configuration.Model})";

    /// <inheritdoc />
    public override string CurrentModel => _configuration.Model;

    /// <inheritdoc />
    public override void SetModel(string modelId)
    {
        // OpenAI configuration is immutable after construction
        // To change models, create a new provider instance
        throw new NotSupportedException(
            "OpenAI provider does not support changing models after construction. " +
            "Create a new provider instance with the desired model.");
    }

    private static void ValidateConfiguration(OpenAiConfiguration configuration)
    {
        if (string.IsNullOrWhiteSpace(configuration.ApiKey))
            throw new ArgumentException("API key cannot be empty.", nameof(configuration));
    }
}
