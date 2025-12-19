using System.Text.Json;
using System.Text.Json.Serialization;

namespace LlmPlayground.Console;

/// <summary>
/// Stores user preferences that persist across sessions.
/// </summary>
public class UserPreferences
{
    private static readonly string PreferencesPath = Path.Combine(
        AppContext.BaseDirectory, "userpreferences.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Selected model for Ollama provider.
    /// </summary>
    [JsonPropertyName("ollamaModel")]
    public string? OllamaModel { get; set; }

    /// <summary>
    /// Selected model for LM Studio provider.
    /// </summary>
    [JsonPropertyName("lmStudioModel")]
    public string? LmStudioModel { get; set; }

    /// <summary>
    /// Whether streaming mode is enabled.
    /// </summary>
    [JsonPropertyName("streamingMode")]
    public bool? StreamingMode { get; set; }

    /// <summary>
    /// Maximum tokens to generate.
    /// </summary>
    [JsonPropertyName("maxTokens")]
    public int? MaxTokens { get; set; }

    /// <summary>
    /// Temperature for sampling.
    /// </summary>
    [JsonPropertyName("temperature")]
    public float? Temperature { get; set; }

    /// <summary>
    /// Top-p (nucleus) sampling threshold.
    /// </summary>
    [JsonPropertyName("topP")]
    public float? TopP { get; set; }

    /// <summary>
    /// Repeat penalty.
    /// </summary>
    [JsonPropertyName("repeatPenalty")]
    public float? RepeatPenalty { get; set; }

    /// <summary>
    /// Loads preferences from disk.
    /// </summary>
    public static UserPreferences Load()
    {
        try
        {
            if (File.Exists(PreferencesPath))
            {
                var json = File.ReadAllText(PreferencesPath);
                return JsonSerializer.Deserialize<UserPreferences>(json, JsonOptions)
                    ?? new UserPreferences();
            }
        }
        catch
        {
            // Ignore errors, return default preferences
        }

        return new UserPreferences();
    }

    /// <summary>
    /// Saves preferences to disk.
    /// </summary>
    public void Save()
    {
        try
        {
            var json = JsonSerializer.Serialize(this, JsonOptions);
            File.WriteAllText(PreferencesPath, json);
        }
        catch
        {
            // Ignore save errors
        }
    }
}

