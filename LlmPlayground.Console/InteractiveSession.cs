using LlmPlayground.Services.Interfaces;
using LlmPlayground.Services.Models;
using Microsoft.Extensions.Configuration;

namespace LlmPlayground.Console;

/// <summary>
/// Manages the interactive chat session with the LLM service.
/// </summary>
public class InteractiveSession
{
    private readonly ILlmService _llmService;
    private readonly IConfiguration _config;
    private readonly UserPreferences _preferences;
    private readonly List<ChatMessageDto> _conversationHistory = [];
    
    private bool _streamingMode;
    private InferenceOptionsDto _options = null!;

    public InteractiveSession(ILlmService llmService, IConfiguration config, UserPreferences preferences)
    {
        _llmService = llmService;
        _config = config;
        _preferences = preferences;
    }

    /// <summary>
    /// Executes a single prompt and returns (for non-interactive/silent mode).
    /// </summary>
    public async Task ExecuteSinglePromptAsync(string prompt, CancellationToken cancellationToken = default)
    {
        InitializeSettings();
        ConsoleStyles.KeyValue("Prompt", prompt);
        await ProcessPromptAsync(prompt, cancellationToken);
    }

    /// <summary>
    /// Runs the interactive chat loop.
    /// </summary>
    public async Task RunAsync()
    {
        InitializeSettings();
        ShowLoadedPreferences();
        ConsoleStyles.Muted("Commands: 'help', 'exit', 'stream', 'settings', 'reset', 'clear', 'history'");
        ConsoleStyles.Blank();

        while (true)
        {
            ConsoleStyles.Prompt(streaming: _streamingMode);
            var input = System.Console.ReadLine();

            if (string.IsNullOrWhiteSpace(input))
                continue;

            if (input.Equals("exit", StringComparison.OrdinalIgnoreCase))
                break;

            if (await HandleCommandAsync(input))
                continue;

            await ExecuteWithCancellationAsync(input);
        }
    }

    private void InitializeSettings()
    {
        _streamingMode = _preferences.StreamingMode ?? false;
        _options = CreateInferenceOptions();
    }

    private void ShowLoadedPreferences()
    {
        if (_preferences.StreamingMode.HasValue || _preferences.MaxTokens.HasValue)
        {
            ConsoleStyles.Muted(
                $"[Loaded preferences: Streaming={_streamingMode}, MaxTokens={_options.MaxTokens}, Temp={_options.Temperature:F1}]");
        }
    }

    private Task<bool> HandleCommandAsync(string input)
    {
        switch (input.ToLowerInvariant())
        {
            case "help":
                ShowHelp();
                return Task.FromResult(true);
            case "stream":
                ToggleStreamingMode();
                return Task.FromResult(true);
            case "clear":
                _conversationHistory.Clear();
                ConsoleStyles.Success("Conversation history cleared.");
                ConsoleStyles.Blank();
                return Task.FromResult(true);
            case "history":
                ShowHistory();
                return Task.FromResult(true);
            case "settings":
                _options = ConfigureSettings();
                return Task.FromResult(true);
            case "reset":
                ResetPreferences();
                ConsoleStyles.Warning("Preferences reset. Restart the app to apply.");
                ConsoleStyles.Blank();
                return Task.FromResult(true);
            default:
                return Task.FromResult(false);
        }
    }

    private async Task ExecuteWithCancellationAsync(string input)
    {
        using var requestCts = new CancellationTokenSource();

        void CancelHandler(object? sender, ConsoleCancelEventArgs e)
        {
            e.Cancel = true;
            requestCts.Cancel();
        }

        System.Console.CancelKeyPress += CancelHandler;
        try
        {
            await ProcessPromptAsync(input, requestCts.Token);
        }
        finally
        {
            System.Console.CancelKeyPress -= CancelHandler;
        }
    }

    private async Task ProcessPromptAsync(string input, CancellationToken cancellationToken)
    {
        var wasCancelled = false;
        try
        {
            _conversationHistory.Add(new ChatMessageDto { Role = "user", Content = input });
            ConsoleStyles.Blank();

            if (_streamingMode)
            {
                await ProcessStreamingResponseAsync(cancellationToken);
            }
            else
            {
                await ProcessNonStreamingResponseAsync(cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            wasCancelled = true;
            HandleCancellation();
        }
        catch (Exception ex) when (!wasCancelled)
        {
            HandleError(ex);
        }
    }

    private async Task ProcessStreamingResponseAsync(CancellationToken cancellationToken)
    {
        var request = new ChatRequest
        {
            Messages = _conversationHistory,
            Options = _options
        };

        var fullResponse = new System.Text.StringBuilder();

        await foreach (var token in _llmService.ChatStreamingAsync(request, cancellationToken))
        {
            ConsoleStyles.ResponseToken(token);
            fullResponse.Append(token);
        }

        ConsoleStyles.Blank();
        ConsoleStyles.Blank();
        _conversationHistory.Add(new ChatMessageDto { Role = "assistant", Content = fullResponse.ToString() });
    }

    private async Task ProcessNonStreamingResponseAsync(CancellationToken cancellationToken)
    {
        ConsoleStyles.Muted("Thinking...");

        var request = new ChatRequest
        {
            Messages = _conversationHistory,
            Options = _options
        };

        var response = await _llmService.ChatAsync(request, cancellationToken);
        ConsoleStyles.Response(response.Text);
        _conversationHistory.Add(new ChatMessageDto { Role = "assistant", Content = response.Text });
    }

    private static void HandleCancellation()
    {
        ConsoleStyles.Blank();
        ConsoleStyles.Warning("Request cancelled.");
        ConsoleStyles.Blank();
    }

    private static void HandleError(Exception ex)
    {
        ConsoleStyles.Blank();
        ConsoleStyles.Error($"Error: {ex.Message}");
        ConsoleStyles.Blank();
    }

    private void ShowHelp()
    {
        ConsoleStyles.Header("Available Commands");
        ConsoleStyles.Muted("  exit      - Exit the application");
        ConsoleStyles.Muted("  help      - Show this help message");
        ConsoleStyles.Muted("  stream    - Toggle streaming mode on/off");
        ConsoleStyles.Muted("  settings  - Configure inference parameters");
        ConsoleStyles.Muted("  reset     - Reset all saved preferences");
        ConsoleStyles.Muted("  clear     - Clear conversation history");
        ConsoleStyles.Muted("  history   - Show conversation history");
        ConsoleStyles.Blank();
    }

    private void ToggleStreamingMode()
    {
        _streamingMode = !_streamingMode;
        _preferences.StreamingMode = _streamingMode;
        _preferences.Save();
        ConsoleStyles.Success($"Streaming mode: {(_streamingMode ? "ON" : "OFF")} (saved)");
        ConsoleStyles.Blank();
    }

    private void ShowHistory()
    {
        if (_conversationHistory.Count == 0)
        {
            ConsoleStyles.Muted("No conversation history yet.");
        }
        else
        {
            ConsoleStyles.Header("Conversation History");
            foreach (var msg in _conversationHistory)
            {
                var roleDisplay = msg.Role == "user" ? "You" : "Assistant";
                ConsoleStyles.KeyValue(roleDisplay, msg.Content ?? "");
            }
        }
        ConsoleStyles.Blank();
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

    private InferenceOptionsDto CreateInferenceOptions()
    {
        return new InferenceOptionsDto
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

    private InferenceOptionsDto ConfigureSettings()
    {
        ConsoleStyles.Header("Settings", ConsoleStyles.Emoji.Gear);
        ConsoleStyles.MenuItem(1, $"Max Tokens: {_options.MaxTokens}");
        ConsoleStyles.MenuItem(2, $"Temperature: {_options.Temperature:F2}");
        ConsoleStyles.MenuItem(3, $"Top-P: {_options.TopP:F2}");
        ConsoleStyles.MenuItem(4, $"Repeat Penalty: {_options.RepeatPenalty:F2}");
        ConsoleStyles.Blank();
        ConsoleStyles.Muted("Select option to change (or press Enter to continue):");
        ConsoleStyles.Prompt();

        var choice = System.Console.ReadLine()?.Trim();
        return choice switch
        {
            "1" => ConfigureMaxTokens(),
            "2" => ConfigureTemperature(),
            "3" => ConfigureTopP(),
            "4" => ConfigureRepeatPenalty(),
            _ => _options
        };
    }

    private InferenceOptionsDto ConfigureMaxTokens()
    {
        ConsoleStyles.KeyValue("Max Tokens", $"[{_options.MaxTokens}]");
        ConsoleStyles.Prompt();
        if (int.TryParse(System.Console.ReadLine(), out var value) && value > 0)
        {
            _options = _options with { MaxTokens = value };
            _preferences.MaxTokens = value;
            _preferences.Save();
        }
        ConsoleStyles.Blank();
        return _options;
    }

    private InferenceOptionsDto ConfigureTemperature()
    {
        ConsoleStyles.KeyValue("Temperature", $"[{_options.Temperature:F2}]");
        ConsoleStyles.Prompt();
        if (float.TryParse(System.Console.ReadLine(), out var value) && value >= 0 && value <= 2)
        {
            _options = _options with { Temperature = value };
            _preferences.Temperature = value;
            _preferences.Save();
        }
        ConsoleStyles.Blank();
        return _options;
    }

    private InferenceOptionsDto ConfigureTopP()
    {
        ConsoleStyles.KeyValue("Top-P", $"[{_options.TopP:F2}]");
        ConsoleStyles.Prompt();
        if (float.TryParse(System.Console.ReadLine(), out var value) && value > 0 && value <= 1)
        {
            _options = _options with { TopP = value };
            _preferences.TopP = value;
            _preferences.Save();
        }
        ConsoleStyles.Blank();
        return _options;
    }

    private InferenceOptionsDto ConfigureRepeatPenalty()
    {
        ConsoleStyles.KeyValue("Repeat Penalty", $"[{_options.RepeatPenalty:F2}]");
        ConsoleStyles.Prompt();
        if (float.TryParse(System.Console.ReadLine(), out var value) && value >= 1)
        {
            _options = _options with { RepeatPenalty = value };
            _preferences.RepeatPenalty = value;
            _preferences.Save();
        }
        ConsoleStyles.Blank();
        return _options;
    }
}

