using LlmPlayground.Core;
using Microsoft.Extensions.Configuration;

namespace LlmPlayground.Console;

/// <summary>
/// Manages the interactive session with the LLM provider.
/// </summary>
public class InteractiveSession
{
    private readonly IConfiguration _config;
    private readonly UserPreferences _preferences;

    public InteractiveSession(IConfiguration config, UserPreferences preferences)
    {
        _config = config;
        _preferences = preferences;
    }

    /// <summary>
    /// Executes a single prompt and returns (for non-interactive/silent mode).
    /// </summary>
    /// <param name="provider">The LLM provider to use.</param>
    /// <param name="prompt">The prompt to execute.</param>
    /// <param name="cancellationToken">Cancellation token for stopping generation.</param>
    public async Task ExecuteSinglePromptAsync(
        ILlmProvider provider,
        string prompt,
        CancellationToken cancellationToken = default)
    {
        var streamingMode = _preferences.StreamingMode ?? false;
        var options = CreateInferenceOptions();
        var conversationHistory = new List<ChatMessage>();

        ConsoleStyles.KeyValue("Prompt", prompt);
        await ProcessPromptAsync(provider, prompt, streamingMode, options, conversationHistory, cancellationToken);
    }

    /// <summary>
    /// Runs the interactive loop with the given LLM provider.
    /// </summary>
    /// <param name="provider">The LLM provider to use.</param>
    public async Task RunAsync(ILlmProvider provider)
    {
        // Load preferences or use config defaults
        var streamingMode = _preferences.StreamingMode ?? false;
        var options = CreateInferenceOptions();

        // Initialize conversation history
        var conversationHistory = new List<ChatMessage>();

        // Show current settings
        if (_preferences.StreamingMode.HasValue || _preferences.MaxTokens.HasValue)
        {
            ConsoleStyles.Muted(
                $"[Loaded preferences: Streaming={streamingMode}, MaxTokens={options.MaxTokens}, Temp={options.Temperature:F1}]");
        }

        ConsoleStyles.Muted("Commands: 'help', 'exit', 'stream', 'settings', 'reset', 'clear', 'history'");
        ConsoleStyles.Blank();

        while (true)
        {
            ConsoleStyles.Prompt(streaming: streamingMode);
            var input = System.Console.ReadLine();

            if (string.IsNullOrWhiteSpace(input))
                continue;

            if (input.Equals("exit", StringComparison.OrdinalIgnoreCase))
                break;

            if (input.Equals("help", StringComparison.OrdinalIgnoreCase))
            {
                ConsoleStyles.Header("Available Commands");
                ConsoleStyles.Muted("  exit      - Exit the application");
                ConsoleStyles.Muted("  help      - Show this help message");
                ConsoleStyles.Muted("  stream    - Toggle streaming mode on/off");
                ConsoleStyles.Muted("  settings  - Configure inference options");
                ConsoleStyles.Muted("  reset     - Reset all saved preferences");
                ConsoleStyles.Muted("  clear     - Clear conversation history");
                ConsoleStyles.Muted("  history   - Show conversation history");
                ConsoleStyles.Blank();
                continue;
            }

            if (input.Equals("stream", StringComparison.OrdinalIgnoreCase))
            {
                streamingMode = !streamingMode;
                _preferences.StreamingMode = streamingMode;
                _preferences.Save();
                ConsoleStyles.Success($"Streaming mode: {(streamingMode ? "ON" : "OFF")} (saved)");
                ConsoleStyles.Blank();
                continue;
            }

            if (input.Equals("clear", StringComparison.OrdinalIgnoreCase))
            {
                conversationHistory.Clear();
                ConsoleStyles.Success("Conversation history cleared.");
                ConsoleStyles.Blank();
                continue;
            }

            if (input.Equals("history", StringComparison.OrdinalIgnoreCase))
            {
                if (conversationHistory.Count == 0)
                {
                    ConsoleStyles.Muted("No conversation history yet.");
                }
                else
                {
                    ConsoleStyles.Header($"Conversation History ({conversationHistory.Count} messages)");
                    foreach (var msg in conversationHistory)
                    {
                        var role = msg.Role switch
                        {
                            ChatRole.System => "[System]",
                            ChatRole.User => "[You]",
                            ChatRole.Assistant => "[Assistant]",
                            _ => "[Unknown]"
                        };
                        var preview = msg.Content.Length > 80
                            ? msg.Content[..80] + "..."
                            : msg.Content;
                        preview = preview.Replace("\n", " ").Replace("\r", "");
                        ConsoleStyles.Muted($"  {role} {preview}");
                    }
                }
                ConsoleStyles.Blank();
                continue;
            }

            if (input.Equals("settings", StringComparison.OrdinalIgnoreCase))
            {
                options = ConfigureSettings(options);
                continue;
            }

            if (input.Equals("reset", StringComparison.OrdinalIgnoreCase))
            {
                ResetPreferences();
                ConsoleStyles.Warning("Preferences reset. Restart the app to apply.");
                ConsoleStyles.Blank();
                continue;
            }

            // Create a per-request cancellation token that can be cancelled with Ctrl+C
            using var requestCts = new CancellationTokenSource();

            // Set up Ctrl+C handler for this request
            void CancelHandler(object? sender, ConsoleCancelEventArgs e)
            {
                e.Cancel = true; // Prevent app termination
                requestCts.Cancel();
            }

            System.Console.CancelKeyPress += CancelHandler;
            try
            {
                await ProcessPromptAsync(provider, input, streamingMode, options, conversationHistory, requestCts.Token);
            }
            finally
            {
                System.Console.CancelKeyPress -= CancelHandler;
            }
        }
    }

    private async Task ProcessPromptAsync(
        ILlmProvider provider,
        string input,
        bool streamingMode,
        LlmInferenceOptions options,
        List<ChatMessage> conversationHistory,
        CancellationToken cancellationToken)
    {
        var wasCancelled = false;
        try
        {
            // Add user message to history
            conversationHistory.Add(new ChatMessage(ChatRole.User, input));

            ConsoleStyles.Blank();
            if (streamingMode)
            {
                ConsoleStyles.KeyValue("Response", "");
                var responseBuilder = new System.Text.StringBuilder();
                await foreach (var token in provider.ChatStreamingAsync(conversationHistory, options, cancellationToken))
                {
                    ConsoleStyles.ResponseToken(token);
                    responseBuilder.Append(token);
                }
                // Add assistant response to history (even partial if cancelled)
                var response = responseBuilder.ToString();
                if (!string.IsNullOrEmpty(response))
                {
                    conversationHistory.Add(new ChatMessage(ChatRole.Assistant, response));
                }
                ConsoleStyles.Blank();
                ConsoleStyles.Blank();
            }
            else
            {
                ConsoleStyles.Status("Generating response... (Ctrl+C to cancel)");
                var result = await provider.ChatAsync(conversationHistory, options, cancellationToken);
                // Add assistant response to history
                conversationHistory.Add(new ChatMessage(ChatRole.Assistant, result.Text));
                ConsoleStyles.Blank();
                ConsoleStyles.KeyValue("Response", result.Text);
                ConsoleStyles.Stats(result.TokensGenerated, result.Duration.TotalSeconds);
                ConsoleStyles.Blank();
            }
        }
        catch (OperationCanceledException)
        {
            wasCancelled = true;
            ConsoleStyles.Blank();
            ConsoleStyles.Warning("Generation cancelled.");
            ConsoleStyles.Blank();
            // Remove the user message if no response was generated
            if (conversationHistory.Count > 0 && conversationHistory[^1].Role == ChatRole.User)
            {
                conversationHistory.RemoveAt(conversationHistory.Count - 1);
            }
        }
        catch (Exception ex) when (!wasCancelled)
        {
            // Remove the failed user message from history
            if (conversationHistory.Count > 0 && conversationHistory[^1].Role == ChatRole.User)
            {
                conversationHistory.RemoveAt(conversationHistory.Count - 1);
            }
            ConsoleStyles.Error(ex.Message);
            ConsoleStyles.Blank();
        }
    }

    private LlmInferenceOptions ConfigureSettings(LlmInferenceOptions options)
    {
        ConsoleStyles.Header("Settings", ConsoleStyles.Emoji.Gear);
        ConsoleStyles.MenuItem(1, $"Max Tokens: {options.MaxTokens}");
        ConsoleStyles.MenuItem(2, $"Temperature: {options.Temperature:F2}");
        ConsoleStyles.MenuItem(3, $"Top-P: {options.TopP:F2}");
        ConsoleStyles.MenuItem(4, $"Repeat Penalty: {options.RepeatPenalty:F2}");
        ConsoleStyles.Blank();
        ConsoleStyles.Muted("Enter number to change, or press Enter to go back:");
        ConsoleStyles.Prompt();

        var input = System.Console.ReadLine();
        if (string.IsNullOrWhiteSpace(input)) return options;

        return input switch
        {
            "1" => ConfigureMaxTokens(options),
            "2" => ConfigureTemperature(options),
            "3" => ConfigureTopP(options),
            "4" => ConfigureRepeatPenalty(options),
            _ => options
        };
    }

    private LlmInferenceOptions ConfigureMaxTokens(LlmInferenceOptions options)
    {
        ConsoleStyles.KeyValue("Max Tokens", $"[{options.MaxTokens}]");
        ConsoleStyles.Prompt();
        if (int.TryParse(System.Console.ReadLine(), out var maxTokens) && maxTokens > 0)
        {
            options = options with { MaxTokens = maxTokens };
            _preferences.MaxTokens = maxTokens;
            _preferences.Save();
            ConsoleStyles.Success($"Max Tokens set to {maxTokens} (saved)");
        }
        ConsoleStyles.Blank();
        return options;
    }

    private LlmInferenceOptions ConfigureTemperature(LlmInferenceOptions options)
    {
        ConsoleStyles.KeyValue("Temperature", $"[{options.Temperature:F2}]");
        ConsoleStyles.Prompt();
        if (float.TryParse(System.Console.ReadLine(), out var temp) && temp >= 0 && temp <= 2)
        {
            options = options with { Temperature = temp };
            _preferences.Temperature = temp;
            _preferences.Save();
            ConsoleStyles.Success($"Temperature set to {temp:F2} (saved)");
        }
        ConsoleStyles.Blank();
        return options;
    }

    private LlmInferenceOptions ConfigureTopP(LlmInferenceOptions options)
    {
        ConsoleStyles.KeyValue("Top-P", $"[{options.TopP:F2}]");
        ConsoleStyles.Prompt();
        if (float.TryParse(System.Console.ReadLine(), out var topP) && topP > 0 && topP <= 1)
        {
            options = options with { TopP = topP };
            _preferences.TopP = topP;
            _preferences.Save();
            ConsoleStyles.Success($"Top-P set to {topP:F2} (saved)");
        }
        ConsoleStyles.Blank();
        return options;
    }

    private LlmInferenceOptions ConfigureRepeatPenalty(LlmInferenceOptions options)
    {
        ConsoleStyles.KeyValue("Repeat Penalty", $"[{options.RepeatPenalty:F2}]");
        ConsoleStyles.Prompt();
        if (float.TryParse(System.Console.ReadLine(), out var penalty) && penalty >= 1)
        {
            options = options with { RepeatPenalty = penalty };
            _preferences.RepeatPenalty = penalty;
            _preferences.Save();
            ConsoleStyles.Success($"Repeat Penalty set to {penalty:F2} (saved)");
        }
        ConsoleStyles.Blank();
        return options;
    }

    private void ResetPreferences()
    {
        _preferences.OllamaModel = null;
        _preferences.LmStudioModel = null;
        _preferences.StreamingMode = null;
        _preferences.MaxTokens = null;
        _preferences.Temperature = null;
        _preferences.TopP = null;
        _preferences.RepeatPenalty = null;
        _preferences.Save();
    }

    private LlmInferenceOptions CreateInferenceOptions()
    {
        return new LlmInferenceOptions
        {
            MaxTokens = _preferences.MaxTokens
                ?? _config.GetValue<int>("Inference:MaxTokens", 2048),
            Temperature = _preferences.Temperature
                ?? _config.GetValue<float>("Inference:Temperature", 0.7f),
            TopP = _preferences.TopP
                ?? _config.GetValue<float>("Inference:TopP", 0.9f),
            RepeatPenalty = _preferences.RepeatPenalty
                ?? _config.GetValue<float>("Inference:RepeatPenalty", 1.1f)
        };
    }
}

