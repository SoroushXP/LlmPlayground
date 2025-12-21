using LlmPlayground.Console.Models;
using LlmPlayground.Console.Services;
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
    private readonly IGameGeneratorService _gameGeneratorService;
    private readonly IConfiguration _config;
    private readonly UserPreferences _preferences;
    private readonly List<ChatMessageDto> _conversationHistory = [];

    private bool _streamingMode;
    private InferenceOptionsDto _options = null!;

    public InteractiveSession(
        ILlmService llmService,
        IGameGeneratorService gameGeneratorService,
        IConfiguration config,
        UserPreferences preferences)
    {
        _llmService = llmService;
        _gameGeneratorService = gameGeneratorService;
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
        ConsoleStyles.Muted("Commands: 'help', 'exit', 'stream', 'settings', 'reset', 'clear', 'history', 'game'");
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

    private async Task<bool> HandleCommandAsync(string input)
    {
        switch (input.ToLowerInvariant())
        {
            case "help":
                ShowHelp();
                return true;
            case "stream":
                ToggleStreamingMode();
                return true;
            case "clear":
                _conversationHistory.Clear();
                ConsoleStyles.Success("Conversation history cleared.");
                ConsoleStyles.Blank();
                return true;
            case "history":
                ShowHistory();
                return true;
            case "settings":
                _options = ConfigureSettings();
                return true;
            case "reset":
                ResetPreferences();
                ConsoleStyles.Warning("Preferences reset. Restart the app to apply.");
                ConsoleStyles.Blank();
                return true;
            case "game":
                await ExecuteGameGenerationWithCancellationAsync();
                return true;
            default:
                return false;
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

    private async Task ExecuteGameGenerationWithCancellationAsync()
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
            await ProcessGameGenerationAsync(requestCts.Token);
        }
        finally
        {
            System.Console.CancelKeyPress -= CancelHandler;
        }
    }

    private async Task ProcessGameGenerationAsync(CancellationToken cancellationToken)
    {
        try
        {
            ConsoleStyles.Header("Game Generation", ConsoleStyles.Emoji.Sparkles);

            // Prompt for theme
            ConsoleStyles.Muted("Enter a theme (or press Enter for random):");
            ConsoleStyles.Prompt();
            var theme = System.Console.ReadLine()?.Trim();
            if (string.IsNullOrWhiteSpace(theme)) theme = null;

            // Prompt for description
            ConsoleStyles.Muted("Enter additional requirements (or press Enter to skip):");
            ConsoleStyles.Prompt();
            var description = System.Console.ReadLine()?.Trim();
            if (string.IsNullOrWhiteSpace(description)) description = null;

            // Prompt for execution preference
            ConsoleStyles.Muted("Execute the generated game? (Y/n):");
            ConsoleStyles.Prompt();
            var executeInput = System.Console.ReadLine()?.Trim();
            var executeGame = string.IsNullOrEmpty(executeInput) ||
                              executeInput.Equals("y", StringComparison.OrdinalIgnoreCase) ||
                              executeInput.Equals("yes", StringComparison.OrdinalIgnoreCase);

            ConsoleStyles.Blank();
            ConsoleStyles.Info("Starting game generation...");
            ConsoleStyles.Blank();

            var request = new GameGenerationRequest
            {
                Theme = theme,
                Description = description,
                ExecuteGame = executeGame
            };

            var result = await _gameGeneratorService.GenerateGameAsync(request, cancellationToken);

            DisplayGameGenerationResult(result);
        }
        catch (OperationCanceledException)
        {
            HandleCancellation();
        }
        catch (Exception ex)
        {
            HandleError(ex);
        }
    }

    private void DisplayGameGenerationResult(GameGenerationResponse result)
    {
        if (!result.Success)
        {
            ConsoleStyles.Error($"Game generation failed: {result.Error}");
            ConsoleStyles.Blank();
            return;
        }

        ConsoleStyles.Header("Game Idea", ConsoleStyles.Emoji.Star);
        ConsoleStyles.Response(result.GameIdea ?? "(No game idea generated)");
        ConsoleStyles.Blank();

        ConsoleStyles.Header("Prolog Code", ConsoleStyles.Emoji.Gear);
        ConsoleStyles.Muted(result.PrologCode ?? "(No code generated)");
        ConsoleStyles.Blank();

        if (result.GeneratedFilePath != null)
        {
            ConsoleStyles.KeyValue("Generated File", result.GeneratedFilePath);
        }

        if (result.ExecutionSuccess.HasValue)
        {
            ConsoleStyles.Header("Execution Result", ConsoleStyles.Emoji.Rocket);
            if (result.ExecutionSuccess.Value)
            {
                ConsoleStyles.Success("Execution successful!");
                ConsoleStyles.Response(result.ExecutionOutput ?? "(No output)");
            }
            else
            {
                ConsoleStyles.Warning("Execution failed.");
                ConsoleStyles.Error(result.ExecutionError ?? "(Unknown error)");
            }
            ConsoleStyles.Blank();
        }

        if (result.FixAttempts > 0)
        {
            ConsoleStyles.KeyValue("Fix Attempts", result.FixAttempts.ToString());
        }

        ConsoleStyles.KeyValue("Provider", result.ProviderUsed ?? "Unknown");
        ConsoleStyles.KeyValue("Duration", $"{result.Duration.TotalSeconds:F2}s");

        if (result.Timings != null)
        {
            ConsoleStyles.Muted($"  - Game Idea: {result.Timings.GameIdeaGeneration.TotalSeconds:F2}s");
            ConsoleStyles.Muted($"  - Prolog Code: {result.Timings.PrologCodeGeneration.TotalSeconds:F2}s");
            if (result.Timings.PrologExecution.HasValue)
            {
                ConsoleStyles.Muted($"  - Execution: {result.Timings.PrologExecution.Value.TotalSeconds:F2}s");
            }
        }

        ConsoleStyles.Blank();

        // Offer to play the game interactively if execution was successful
        if (result.ExecutionSuccess == true && !string.IsNullOrEmpty(result.GeneratedFilePath))
        {
            OfferInteractivePlay(result.GeneratedFilePath);
        }
    }

    private void OfferInteractivePlay(string prologFilePath)
    {
        ConsoleStyles.Info("Would you like to play the game interactively? (Y/n):");
        ConsoleStyles.Prompt();
        var playInput = System.Console.ReadLine()?.Trim();
        var wantsToPlay = string.IsNullOrEmpty(playInput) ||
                          playInput.Equals("y", StringComparison.OrdinalIgnoreCase) ||
                          playInput.Equals("yes", StringComparison.OrdinalIgnoreCase);

        if (!wantsToPlay)
        {
            return;
        }

        ConsoleStyles.Blank();
        ConsoleStyles.Header("Interactive Prolog Session", ConsoleStyles.Emoji.Sparkles);
        ConsoleStyles.Muted("Type Prolog queries at the '?-' prompt. Examples:");
        ConsoleStyles.Muted("  main.          - Run the game demonstration again");
        ConsoleStyles.Muted("  halt.          - Exit the Prolog session");
        ConsoleStyles.Muted("  listing.       - Show all defined predicates");
        ConsoleStyles.Blank();
        ConsoleStyles.Warning("Starting SWI-Prolog... (type 'halt.' to return)");
        ConsoleStyles.Blank();

        try
        {
            var processInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "swipl",
                Arguments = $"-s \"{prologFilePath}\"",
                UseShellExecute = false,
                RedirectStandardInput = false,
                RedirectStandardOutput = false,
                RedirectStandardError = false
            };

            using var process = System.Diagnostics.Process.Start(processInfo);
            process?.WaitForExit();
        }
        catch (Exception ex)
        {
            ConsoleStyles.Error($"Failed to start Prolog: {ex.Message}");
        }

        ConsoleStyles.Blank();
        ConsoleStyles.Success("Returned from Prolog session.");
        ConsoleStyles.Blank();
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
        ConsoleStyles.Muted("  game      - Generate a Prolog-based logic game");
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

