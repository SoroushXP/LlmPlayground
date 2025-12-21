using LlmPlayground.Core;
using LlmPlayground.Services.Interfaces;
using Microsoft.Extensions.Configuration;

namespace LlmPlayground.Console;

/// <summary>
/// Initializes the LLM service with the configured provider and handles model selection.
/// </summary>
public class ServiceInitializer
{
    private readonly IConfiguration _config;
    private readonly ModelSelector _modelSelector;

    public ServiceInitializer(IConfiguration config, ModelSelector modelSelector)
    {
        _config = config;
        _modelSelector = modelSelector;
    }

    /// <summary>
    /// Initializes the LLM service with the specified provider type and handles model selection.
    /// </summary>
    /// <param name="llmService">The LLM service to initialize.</param>
    /// <param name="providerType">The provider type to use.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task InitializeAsync(
        ILlmService llmService,
        LlmProviderType providerType,
        CancellationToken cancellationToken = default)
    {
        // Validate OpenAI API key if using OpenAI
        if (providerType == LlmProviderType.OpenAI)
        {
            var apiKey = _config["OpenAI:ApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                ConsoleStyles.Error("OpenAI API Key Not Configured!");
                ConsoleStyles.Muted("Set your API key in appsettings.json under OpenAI:ApiKey");
                ConsoleStyles.Muted("Or set the Provider to 'Ollama' or 'LmStudio' to use a local model.");
                Environment.Exit(1);
            }
        }

        // Set the provider
        ConsoleStyles.Status($"Connecting to {providerType}...");
        await llmService.SetProviderAsync(providerType, cancellationToken);

        // Handle model selection
        await SelectModelAsync(llmService, providerType, cancellationToken);
    }

    private async Task SelectModelAsync(
        ILlmService llmService,
        LlmProviderType providerType,
        CancellationToken cancellationToken)
    {
        var configuredModel = GetConfiguredModel(providerType);
        var providerKey = providerType.ToString().ToLowerInvariant();

        // Get available models from service
        var models = await llmService.GetAvailableModelsAsync(cancellationToken);

        if (models.Count > 0)
        {
            // Convert to LlmModelInfo for ModelSelector compatibility
            var modelInfos = models.Select(m => new LlmModelInfo(m.Id, m.OwnedBy, m.Created)).ToList();

            var selectedModel = await _modelSelector.SelectModelAsync(modelInfos, configuredModel, providerKey);

            if (!string.IsNullOrWhiteSpace(selectedModel))
            {
                llmService.SetModel(selectedModel);
                ConsoleStyles.Model(selectedModel);
            }
        }
        else if (!string.IsNullOrWhiteSpace(configuredModel))
        {
            // No models available from API, use configured model
            llmService.SetModel(configuredModel);
            ConsoleStyles.Model(configuredModel);
        }
    }

    private string GetConfiguredModel(LlmProviderType providerType)
    {
        return providerType switch
        {
            LlmProviderType.Ollama => _config.GetValue<string>("Ollama:Model", "") ?? "",
            LlmProviderType.LmStudio => _config.GetValue<string>("LmStudio:Model", "") ?? "",
            LlmProviderType.OpenAI => _config.GetValue<string>("OpenAI:Model", "gpt-4o-mini") ?? "gpt-4o-mini",
            _ => ""
        };
    }
}

