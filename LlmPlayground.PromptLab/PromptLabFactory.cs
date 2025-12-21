using LlmPlayground.Core;
using Microsoft.Extensions.Configuration;

namespace LlmPlayground.PromptLab;

/// <summary>
/// Factory for creating prompt sessions and LLM providers.
/// </summary>
public static class PromptLabFactory
{
    /// <summary>
    /// Creates an LLM provider from configuration.
    /// </summary>
    /// <param name="providerName">Provider name: "Ollama", "LmStudio", or "OpenAI".</param>
    /// <param name="configuration">Configuration containing provider settings.</param>
    /// <returns>The configured LLM provider.</returns>
    public static ILlmProvider CreateProvider(string providerName, IConfiguration configuration)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerName);
        ArgumentNullException.ThrowIfNull(configuration);

        return providerName.ToLowerInvariant() switch
        {
            "ollama" => CreateOllamaProvider(configuration),
            "lmstudio" or "lm-studio" => CreateLmStudioProvider(configuration),
            "openai" => CreateOpenAiProvider(configuration),
            _ => throw new ArgumentException($"Unknown provider: {providerName}. Supported: Ollama, LmStudio, OpenAI")
        };
    }

    /// <summary>
    /// Creates an Ollama provider.
    /// </summary>
    public static OllamaProvider CreateOllamaProvider(OllamaConfiguration config)
    {
        return new OllamaProvider(config);
    }

    /// <summary>
    /// Creates an Ollama provider from configuration.
    /// </summary>
    public static OllamaProvider CreateOllamaProvider(IConfiguration configuration)
    {
        var section = configuration.GetSection("Ollama");
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

    /// <summary>
    /// Creates an LM Studio provider.
    /// </summary>
    public static LmStudioProvider CreateLmStudioProvider(LmStudioConfiguration config)
    {
        return new LmStudioProvider(config);
    }

    /// <summary>
    /// Creates an LM Studio provider from configuration.
    /// </summary>
    public static LmStudioProvider CreateLmStudioProvider(IConfiguration configuration)
    {
        var section = configuration.GetSection("LmStudio");
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

    /// <summary>
    /// Creates an OpenAI provider.
    /// </summary>
    public static OpenAiProvider CreateOpenAiProvider(OpenAiConfiguration config)
    {
        return new OpenAiProvider(config);
    }

    /// <summary>
    /// Creates an OpenAI provider from configuration.
    /// </summary>
    public static OpenAiProvider CreateOpenAiProvider(IConfiguration configuration)
    {
        var section = configuration.GetSection("OpenAI");
        var apiKey = section["ApiKey"];
        
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("OpenAI API key is required. Set 'OpenAI:ApiKey' in configuration.");

        return new OpenAiProvider(new OpenAiConfiguration
        {
            ApiKey = apiKey,
            Model = section["Model"] ?? "gpt-4o-mini",
            SystemPrompt = section["SystemPrompt"],
            BaseUrl = string.IsNullOrWhiteSpace(section["BaseUrl"]) 
                ? null 
                : section["BaseUrl"],
            TimeoutSeconds = section.GetValue("TimeoutSeconds", 120)
        });
    }

    /// <summary>
    /// Creates a prompt session with the specified provider.
    /// </summary>
    /// <param name="provider">The LLM provider.</param>
    /// <param name="systemPrompt">Optional system prompt.</param>
    /// <param name="options">Optional inference options.</param>
    /// <returns>A new prompt session.</returns>
    public static PromptSession CreateSession(
        ILlmProvider provider, 
        string? systemPrompt = null, 
        LlmInferenceOptions? options = null)
    {
        return new PromptSession(provider, systemPrompt, options);
    }

    /// <summary>
    /// Creates inference options from configuration.
    /// </summary>
    public static LlmInferenceOptions CreateInferenceOptions(IConfiguration configuration)
    {
        var section = configuration.GetSection("Inference");
        return new LlmInferenceOptions
        {
            MaxTokens = section.GetValue("MaxTokens", 4096),
            Temperature = section.GetValue("Temperature", 0.7f),
            TopP = section.GetValue("TopP", 0.9f),
            RepeatPenalty = section.GetValue("RepeatPenalty", 1.1f)
        };
    }
}

