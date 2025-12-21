using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace LlmPlayground.Core.Extensions;

/// <summary>
/// Extension methods for registering LLM providers with dependency injection.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the LLM provider factory and all configured providers to the service collection.
    /// Reads configuration from the provided IConfiguration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration root.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddLlmProviders(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Register HttpClient factory
        services.AddHttpClient();

        // Register named HttpClients for each provider
        services.AddHttpClient("Ollama");
        services.AddHttpClient("LmStudio");
        services.AddHttpClient("OpenAI");

        // Bind Ollama configuration
        var ollamaSection = configuration.GetSection("Ollama");
        if (ollamaSection.Exists())
        {
            var ollamaConfig = new OllamaConfiguration
            {
                Host = ollamaSection["Host"] ?? "localhost",
                Port = int.TryParse(ollamaSection["Port"], out var port) ? port : 11434,
                Scheme = ollamaSection["Scheme"] ?? "http",
                ApiPath = ollamaSection["ApiPath"] ?? "/v1",
                Model = ollamaSection["Model"] ?? "llama3",
                SystemPrompt = ollamaSection["SystemPrompt"],
                TimeoutSeconds = int.TryParse(ollamaSection["TimeoutSeconds"], out var timeout) ? timeout : 300,
                BaseUrlOverride = ollamaSection["BaseUrlOverride"]
            };
            services.AddSingleton(Options.Create(ollamaConfig));
        }

        // Bind LM Studio configuration
        var lmStudioSection = configuration.GetSection("LmStudio");
        if (lmStudioSection.Exists())
        {
            var lmStudioConfig = new LmStudioConfiguration
            {
                Host = lmStudioSection["Host"] ?? "localhost",
                Port = int.TryParse(lmStudioSection["Port"], out var port) ? port : 1234,
                Scheme = lmStudioSection["Scheme"] ?? "http",
                ApiPath = lmStudioSection["ApiPath"] ?? "/v1",
                Model = lmStudioSection["Model"] ?? "local-model",
                SystemPrompt = lmStudioSection["SystemPrompt"],
                TimeoutSeconds = int.TryParse(lmStudioSection["TimeoutSeconds"], out var timeout) ? timeout : 300,
                BaseUrlOverride = lmStudioSection["BaseUrlOverride"]
            };
            services.AddSingleton(Options.Create(lmStudioConfig));
        }

        // Bind OpenAI configuration
        var openAiSection = configuration.GetSection("OpenAI");
        if (openAiSection.Exists() && !string.IsNullOrWhiteSpace(openAiSection["ApiKey"]))
        {
            var openAiConfig = new OpenAiConfiguration
            {
                ApiKey = openAiSection["ApiKey"]!,
                Model = openAiSection["Model"] ?? "gpt-4o-mini",
                SystemPrompt = openAiSection["SystemPrompt"],
                BaseUrlOverride = openAiSection["BaseUrl"],
                TimeoutSeconds = int.TryParse(openAiSection["TimeoutSeconds"], out var timeout) ? timeout : 120
            };
            services.AddSingleton(Options.Create(openAiConfig));
        }

        // Bind Local LLM configuration
        var localSection = configuration.GetSection("LocalLlm");
        if (localSection.Exists() && !string.IsNullOrWhiteSpace(localSection["ModelPath"]))
        {
            var localConfig = new LocalLlmConfiguration
            {
                ModelPath = localSection["ModelPath"]!,
                Backend = Enum.TryParse<LlmBackendType>(localSection["Backend"], out var backend)
                    ? backend
                    : LlmBackendType.Cpu,
                GpuDeviceIndex = int.TryParse(localSection["GpuDeviceIndex"], out var gpuIdx) ? gpuIdx : 0,
                GpuLayerCount = int.TryParse(localSection["GpuLayerCount"], out var gpuLayers) ? gpuLayers : 0,
                ContextSize = uint.TryParse(localSection["ContextSize"], out var ctxSize) ? ctxSize : 2048,
                ThreadCount = uint.TryParse(localSection["ThreadCount"], out var threads)
                    ? threads
                    : (uint)Math.Max(1, Environment.ProcessorCount / 2)
            };
            services.AddSingleton(Options.Create(localConfig));
        }

        // Register the factory
        services.AddSingleton<ILlmProviderFactory, LlmProviderFactory>();

        return services;
    }

    /// <summary>
    /// Adds the Ollama provider with the specified configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration to use.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddOllamaProvider(
        this IServiceCollection services,
        OllamaConfiguration configuration)
    {
        services.AddHttpClient("Ollama");
        services.AddSingleton(Options.Create(configuration));
        EnsureFactoryRegistered(services);
        return services;
    }

    /// <summary>
    /// Adds the Ollama provider with default configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="host">The Ollama server host.</param>
    /// <param name="port">The Ollama server port.</param>
    /// <param name="model">The model to use.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddOllamaProvider(
        this IServiceCollection services,
        string host = "localhost",
        int port = 11434,
        string model = "llama3")
    {
        var configuration = new OllamaConfiguration
        {
            Host = host,
            Port = port,
            Model = model
        };
        return services.AddOllamaProvider(configuration);
    }

    /// <summary>
    /// Adds the LM Studio provider with the specified configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration to use.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddLmStudioProvider(
        this IServiceCollection services,
        LmStudioConfiguration configuration)
    {
        services.AddHttpClient("LmStudio");
        services.AddSingleton(Options.Create(configuration));
        EnsureFactoryRegistered(services);
        return services;
    }

    /// <summary>
    /// Adds the LM Studio provider with default configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="host">The LM Studio server host.</param>
    /// <param name="port">The LM Studio server port.</param>
    /// <param name="model">The model identifier.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddLmStudioProvider(
        this IServiceCollection services,
        string host = "localhost",
        int port = 1234,
        string model = "local-model")
    {
        var configuration = new LmStudioConfiguration
        {
            Host = host,
            Port = port,
            Model = model
        };
        return services.AddLmStudioProvider(configuration);
    }

    /// <summary>
    /// Adds the OpenAI provider with the specified configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration to use.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddOpenAiProvider(
        this IServiceCollection services,
        OpenAiConfiguration configuration)
    {
        services.AddHttpClient("OpenAI");
        services.AddSingleton(Options.Create(configuration));
        EnsureFactoryRegistered(services);
        return services;
    }

    /// <summary>
    /// Adds the OpenAI provider with the specified API key.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="apiKey">The OpenAI API key.</param>
    /// <param name="model">The model to use (default: gpt-4o-mini).</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddOpenAiProvider(
        this IServiceCollection services,
        string apiKey,
        string model = "gpt-4o-mini")
    {
        var configuration = new OpenAiConfiguration
        {
            ApiKey = apiKey,
            Model = model
        };
        return services.AddOpenAiProvider(configuration);
    }

    /// <summary>
    /// Adds the local LLM provider with the specified configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration to use.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddLocalLlmProvider(
        this IServiceCollection services,
        LocalLlmConfiguration configuration)
    {
        services.AddSingleton(Options.Create(configuration));
        EnsureFactoryRegistered(services);
        return services;
    }

    /// <summary>
    /// Adds the local LLM provider with the specified model path.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="modelPath">Path to the GGUF model file.</param>
    /// <param name="backend">The backend to use for inference.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddLocalLlmProvider(
        this IServiceCollection services,
        string modelPath,
        LlmBackendType backend = LlmBackendType.Cpu)
    {
        var configuration = new LocalLlmConfiguration
        {
            ModelPath = modelPath,
            Backend = backend
        };
        return services.AddLocalLlmProvider(configuration);
    }

    /// <summary>
    /// Adds only the provider factory without any default configurations.
    /// Use this when you want to provide configurations at runtime.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddLlmProviderFactory(this IServiceCollection services)
    {
        services.AddHttpClient();
        EnsureFactoryRegistered(services);
        return services;
    }

    private static void EnsureFactoryRegistered(IServiceCollection services)
    {
        // Check if factory is already registered
        var factoryRegistered = false;
        foreach (var descriptor in services)
        {
            if (descriptor.ServiceType == typeof(ILlmProviderFactory))
            {
                factoryRegistered = true;
                break;
            }
        }

        if (!factoryRegistered)
        {
            services.AddSingleton<ILlmProviderFactory, LlmProviderFactory>();
        }
    }
}
