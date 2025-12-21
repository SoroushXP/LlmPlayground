using LlmPlayground.Core;

namespace LlmPlayground.Console;

/// <summary>
/// Handles model selection for providers that support listing models.
/// </summary>
public class ModelSelector
{
    private readonly UserPreferences _preferences;
    private readonly CommandLineOptions _options;

    public ModelSelector(UserPreferences preferences, CommandLineOptions options)
    {
        _preferences = preferences;
        _options = options;
    }

    /// <summary>
    /// Allows the user to select a model from a list of available models.
    /// </summary>
    /// <param name="models">The list of available models.</param>
    /// <param name="configuredModel">The model configured in settings.</param>
    /// <param name="providerKey">Key to identify the provider for saving preferences.</param>
    /// <returns>The selected model ID.</returns>
    public Task<string> SelectModelAsync(
        IReadOnlyList<LlmModelInfo> models,
        string configuredModel,
        string providerKey)
    {
        // Check for saved preference first
        var savedModel = GetSavedModel(providerKey);

        if (models.Count == 0)
        {
            return HandleNoModelsAvailable(savedModel, configuredModel);
        }

        // If we have a saved preference and it's still available, use it automatically
        if (!string.IsNullOrWhiteSpace(savedModel) && models.Any(m => m.Id == savedModel))
        {
            ConsoleStyles.Success($"Using saved model: {savedModel}");
            return Task.FromResult(savedModel);
        }

        DisplayAvailableModels(models);

        // In silent mode or single-prompt mode, auto-select
        if (_options.SilentMode || _options.SinglePrompt != null)
        {
            return Task.FromResult(AutoSelectModel(models, configuredModel, providerKey));
        }

        // Interactive mode: let user choose
        return Task.FromResult(SelectModelInteractively(models, configuredModel, providerKey));
    }

    private string? GetSavedModel(string providerKey)
    {
        return providerKey switch
        {
            "ollama" => _preferences.OllamaModel,
            "lmstudio" => _preferences.LmStudioModel,
            _ => null
        };
    }

    private Task<string> HandleNoModelsAvailable(string? savedModel, string configuredModel)
    {
        ConsoleStyles.Warning("Could not fetch available models from server.");

        if (!string.IsNullOrWhiteSpace(savedModel))
        {
            ConsoleStyles.Info($"Using saved model: {savedModel}");
            return Task.FromResult(savedModel);
        }

        if (!string.IsNullOrWhiteSpace(configuredModel))
        {
            ConsoleStyles.Info($"Using configured model: {configuredModel}");
            return Task.FromResult(configuredModel);
        }

        throw new InvalidOperationException(
            "No models available and no model configured. Please configure a model or ensure the server is running.");
    }

    private static void DisplayAvailableModels(IReadOnlyList<LlmModelInfo> models)
    {
        ConsoleStyles.Info($"Found {models.Count} model(s):");
        ConsoleStyles.Blank();
        for (var i = 0; i < models.Count; i++)
        {
            var model = models[i];
            var ownedBy = !string.IsNullOrWhiteSpace(model.OwnedBy) ? $" (by {model.OwnedBy})" : "";
            ConsoleStyles.MenuItem(i + 1, $"{model.Id}{ownedBy}");
        }
        ConsoleStyles.Blank();
    }

    private string AutoSelectModel(IReadOnlyList<LlmModelInfo> models, string configuredModel, string providerKey)
    {
        if (!string.IsNullOrWhiteSpace(configuredModel) && models.Any(m => m.Id == configuredModel))
        {
            ConsoleStyles.Muted($"Auto-selecting configured model: '{configuredModel}'");
            return configuredModel;
        }

        var selectedModel = models[0].Id;
        ConsoleStyles.Muted($"Auto-selecting first available model: '{selectedModel}'");
        SaveModelPreference(providerKey, selectedModel);
        return selectedModel;
    }

    private string SelectModelInteractively(
        IReadOnlyList<LlmModelInfo> models,
        string configuredModel,
        string providerKey)
    {
        // If there's a valid configured model, use it as default
        var defaultIndex = 0;
        if (!string.IsNullOrWhiteSpace(configuredModel))
        {
            var configIndex = models.ToList().FindIndex(m => m.Id == configuredModel);
            if (configIndex >= 0) defaultIndex = configIndex;
        }

        while (true)
        {
            ConsoleStyles.Muted($"Select model [1-{models.Count}] (default: {defaultIndex + 1}):");
            ConsoleStyles.Prompt();
            var input = System.Console.ReadLine();

            string selectedModel;
            if (string.IsNullOrWhiteSpace(input))
            {
                selectedModel = models[defaultIndex].Id;
            }
            else if (int.TryParse(input, out var selection) && selection >= 1 && selection <= models.Count)
            {
                selectedModel = models[selection - 1].Id;
            }
            else
            {
                ConsoleStyles.Warning("Invalid selection. Please try again.");
                continue;
            }

            // Save the selection for next time
            SaveModelPreference(providerKey, selectedModel);
            return selectedModel;
        }
    }

    private void SaveModelPreference(string providerKey, string model)
    {
        switch (providerKey)
        {
            case "ollama":
                _preferences.OllamaModel = model;
                break;
            case "lmstudio":
                _preferences.LmStudioModel = model;
                break;
        }
        _preferences.Save();
        ConsoleStyles.Muted("(Model preference saved)");
    }
}

