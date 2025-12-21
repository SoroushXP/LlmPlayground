namespace LlmPlayground.Core;

/// <summary>
/// Enumeration of available LLM provider types.
/// </summary>
public enum LlmProviderType
{
    /// <summary>
    /// Ollama local server provider.
    /// </summary>
    Ollama,

    /// <summary>
    /// LM Studio local server provider.
    /// </summary>
    LmStudio,

    /// <summary>
    /// OpenAI API provider.
    /// </summary>
    OpenAI,

    /// <summary>
    /// Local GGUF model provider via LLamaSharp.
    /// </summary>
    Local
}

/// <summary>
/// Factory for creating LLM provider instances.
/// </summary>
public interface ILlmProviderFactory
{
    /// <summary>
    /// Gets the available provider types.
    /// </summary>
    IReadOnlyList<LlmProviderType> AvailableProviders { get; }

    /// <summary>
    /// Creates an Ollama provider instance.
    /// </summary>
    /// <param name="configuration">Optional configuration override. Uses registered options if null.</param>
    /// <returns>A new Ollama provider instance.</returns>
    OllamaProvider CreateOllamaProvider(OllamaConfiguration? configuration = null);

    /// <summary>
    /// Creates an LM Studio provider instance.
    /// </summary>
    /// <param name="configuration">Optional configuration override. Uses registered options if null.</param>
    /// <returns>A new LM Studio provider instance.</returns>
    LmStudioProvider CreateLmStudioProvider(LmStudioConfiguration? configuration = null);

    /// <summary>
    /// Creates an OpenAI provider instance.
    /// </summary>
    /// <param name="configuration">Optional configuration override. Uses registered options if null.</param>
    /// <returns>A new OpenAI provider instance.</returns>
    OpenAiProvider CreateOpenAiProvider(OpenAiConfiguration? configuration = null);

    /// <summary>
    /// Creates a local GGUF model provider instance.
    /// </summary>
    /// <param name="configuration">Optional configuration override. Uses registered options if null.</param>
    /// <returns>A new local LLM provider instance.</returns>
    LocalLlmProvider CreateLocalProvider(LocalLlmConfiguration? configuration = null);

    /// <summary>
    /// Creates a provider of the specified type.
    /// </summary>
    /// <param name="providerType">The type of provider to create.</param>
    /// <returns>A new provider instance.</returns>
    /// <exception cref="ArgumentException">Thrown if the provider type is not configured.</exception>
    ILlmProvider CreateProvider(LlmProviderType providerType);

    /// <summary>
    /// Checks if the specified provider type is configured and available.
    /// </summary>
    /// <param name="providerType">The provider type to check.</param>
    /// <returns>True if the provider is available; otherwise, false.</returns>
    bool IsProviderAvailable(LlmProviderType providerType);
}

