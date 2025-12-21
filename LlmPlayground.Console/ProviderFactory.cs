using LlmPlayground.Core;
using Microsoft.Extensions.Configuration;

namespace LlmPlayground.Console;

/// <summary>
/// Factory for creating LLM provider instances from configuration.
/// </summary>
public class ProviderFactory
{
    private readonly IConfiguration _config;
    private readonly ModelSelector _modelSelector;

    public ProviderFactory(IConfiguration config, ModelSelector modelSelector)
    {
        _config = config;
        _modelSelector = modelSelector;
    }

    /// <summary>
    /// Creates an LLM provider based on the configured provider type.
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    /// <returns>The configured LLM provider.</returns>
    public async Task<ILlmProvider> CreateProviderAsync(string[] args)
    {
        var providerType = _config.GetValue<string>("Provider", "LocalLlm") ?? "LocalLlm";
        return providerType.ToLowerInvariant() switch
        {
            "openai" => CreateOpenAiProvider(),
            "ollama" => await CreateOllamaProviderAsync(),
            "lmstudio" => await CreateLmStudioProviderAsync(),
            _ => CreateLocalLlmProvider(args)
        };
    }

    private ILlmProvider CreateOpenAiProvider()
    {
        var apiKey = _config["OpenAI:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            ConsoleStyles.Error("OpenAI API Key Not Configured!");
            ConsoleStyles.Muted("Set your API key in appsettings.json under OpenAI:ApiKey");
            ConsoleStyles.Muted("Or set the Provider to 'LocalLlm' to use a local model.");
            Environment.Exit(1);
        }

        var openAiConfig = new OpenAiConfiguration
        {
            ApiKey = apiKey,
            Model = _config.GetValue<string>("OpenAI:Model", "gpt-4o-mini") ?? "gpt-4o-mini",
            SystemPrompt = _config["OpenAI:SystemPrompt"],
            BaseUrlOverride = string.IsNullOrWhiteSpace(_config["OpenAI:BaseUrl"]) ? null : _config["OpenAI:BaseUrl"],
            TimeoutSeconds = _config.GetValue<int>("OpenAI:TimeoutSeconds", 120)
        };

        ConsoleStyles.Model(openAiConfig.Model);
        if (!string.IsNullOrWhiteSpace(openAiConfig.SystemPrompt))
            ConsoleStyles.KeyValue("System", openAiConfig.SystemPrompt);
        ConsoleStyles.Blank();

        return new OpenAiProvider(openAiConfig);
    }

    private async Task<ILlmProvider> CreateOllamaProviderAsync()
    {
        var baseUrlOverride = _config["Ollama:BaseUrlOverride"];
        var configuredModel = _config.GetValue<string>("Ollama:Model", "") ?? "";
        var ollamaConfig = new OllamaConfiguration
        {
            Host = _config.GetValue<string>("Ollama:Host", "localhost") ?? "localhost",
            Port = _config.GetValue<int>("Ollama:Port", 11434),
            Scheme = _config.GetValue<string>("Ollama:Scheme", "http") ?? "http",
            ApiPath = _config.GetValue<string>("Ollama:ApiPath", "/v1") ?? "/v1",
            Model = string.IsNullOrWhiteSpace(configuredModel) ? "placeholder" : configuredModel,
            SystemPrompt = _config["Ollama:SystemPrompt"],
            TimeoutSeconds = _config.GetValue<int>("Ollama:TimeoutSeconds", 300),
            BaseUrlOverride = string.IsNullOrWhiteSpace(baseUrlOverride) ? null : baseUrlOverride
        };

        ConsoleStyles.Connection("Ollama", ollamaConfig.BaseUrl);

        var provider = new OllamaProvider(ollamaConfig);

        var selectedModel = await _modelSelector.DiscoverAndSelectModelAsync(provider, configuredModel, "ollama");
        if (selectedModel != configuredModel)
        {
            provider.SetModel(selectedModel);
        }

        ConsoleStyles.Model(provider.CurrentModel);
        if (!string.IsNullOrWhiteSpace(ollamaConfig.SystemPrompt))
            ConsoleStyles.KeyValue("System", ollamaConfig.SystemPrompt);
        ConsoleStyles.Blank();

        return provider;
    }

    private async Task<ILlmProvider> CreateLmStudioProviderAsync()
    {
        var baseUrlOverride = _config["LmStudio:BaseUrlOverride"];
        var configuredModel = _config.GetValue<string>("LmStudio:Model", "") ?? "";
        var lmStudioConfig = new LmStudioConfiguration
        {
            Host = _config.GetValue<string>("LmStudio:Host", "localhost") ?? "localhost",
            Port = _config.GetValue<int>("LmStudio:Port", 1234),
            Scheme = _config.GetValue<string>("LmStudio:Scheme", "http") ?? "http",
            ApiPath = _config.GetValue<string>("LmStudio:ApiPath", "/v1") ?? "/v1",
            Model = string.IsNullOrWhiteSpace(configuredModel) ? "placeholder" : configuredModel,
            SystemPrompt = _config["LmStudio:SystemPrompt"],
            TimeoutSeconds = _config.GetValue<int>("LmStudio:TimeoutSeconds", 300),
            BaseUrlOverride = string.IsNullOrWhiteSpace(baseUrlOverride) ? null : baseUrlOverride
        };

        ConsoleStyles.Connection("LM Studio", lmStudioConfig.BaseUrl);

        var provider = new LmStudioProvider(lmStudioConfig);

        var selectedModel = await _modelSelector.DiscoverAndSelectModelAsync(provider, configuredModel, "lmstudio");
        if (selectedModel != configuredModel)
        {
            provider.SetModel(selectedModel);
        }

        ConsoleStyles.Model(provider.CurrentModel);
        if (!string.IsNullOrWhiteSpace(lmStudioConfig.SystemPrompt))
            ConsoleStyles.KeyValue("System", lmStudioConfig.SystemPrompt);
        ConsoleStyles.Blank();

        return provider;
    }

    private ILlmProvider CreateLocalLlmProvider(string[] args)
    {
        var modelPath = args.Length > 0 ? args[0] : _config["LocalLlm:ModelPath"];

        if (string.IsNullOrWhiteSpace(modelPath))
        {
            modelPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                                     "models", "model.gguf");
        }

        if (!File.Exists(modelPath))
        {
            ConsoleStyles.Error("Local LLM Model Not Found!");
            ConsoleStyles.KeyValue("Expected path", modelPath);
            ConsoleStyles.Blank();
            ConsoleStyles.Header("Options", ConsoleStyles.Emoji.Question);
            ConsoleStyles.ListItem("Set Provider to 'OpenAI' and configure API key");
            ConsoleStyles.ListItem("Download a GGUF model from https://huggingface.co/");
            ConsoleStyles.Blank();
            ConsoleStyles.Header("Popular models", ConsoleStyles.Emoji.Star);
            ConsoleStyles.ListItem("TinyLlama: https://huggingface.co/TheBloke/TinyLlama-1.1B-Chat-v1.0-GGUF");
            ConsoleStyles.ListItem("Phi-2: https://huggingface.co/TheBloke/phi-2-GGUF");
            ConsoleStyles.ListItem("Mistral-7B: https://huggingface.co/TheBloke/Mistral-7B-Instruct-v0.2-GGUF");
            Environment.Exit(1);
        }

        var backendStr = _config.GetValue<string>("LocalLlm:Backend", "Cpu") ?? "Cpu";
        var backend = Enum.TryParse<LlmBackendType>(backendStr, ignoreCase: true, out var parsedBackend)
            ? parsedBackend
            : LlmBackendType.Cpu;
        var gpuDeviceIndex = _config.GetValue<int>("LocalLlm:GpuDeviceIndex", 0);
        var gpuLayerCount = _config.GetValue<int>("LocalLlm:GpuLayerCount", 0);
        var contextSize = _config.GetValue<uint>("LocalLlm:ContextSize", 2048);
        var threadCount = _config.GetValue<uint>("LocalLlm:ThreadCount", 0);
        if (threadCount == 0)
            threadCount = (uint)Math.Max(1, Environment.ProcessorCount / 2);

        ConsoleStyles.Model(modelPath);
        ConsoleStyles.KeyValue("Backend", $"{backend}, GPU Device: {gpuDeviceIndex}, GPU Layers: {gpuLayerCount}");
        ConsoleStyles.KeyValue("Context Size", $"{contextSize}, Threads: {threadCount}");
        ConsoleStyles.Blank();

        return new LocalLlmProvider(new LocalLlmConfiguration
        {
            ModelPath = modelPath,
            Backend = backend,
            GpuDeviceIndex = gpuDeviceIndex,
            GpuLayerCount = gpuLayerCount,
            ContextSize = contextSize,
            ThreadCount = threadCount
        });
    }
}

