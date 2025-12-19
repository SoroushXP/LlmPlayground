using LlmPlayground.Console;
using Microsoft.Extensions.Configuration;

// Build configuration from appsettings.json
var config = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .Build();

// Initialize console styling from configuration
ConsoleStyles.Initialize(config);

// Load user preferences
var preferences = UserPreferences.Load();

// Check for silent mode (non-interactive auto-select model)
var silentMode = args.Contains("--silent") || args.Contains("-s");

// Check for single prompt mode: --prompt "your question here"
var promptIndex = Array.FindIndex(args, a => a == "--prompt" || a == "-p");
var singlePrompt = promptIndex >= 0 && promptIndex + 1 < args.Length
    ? args[promptIndex + 1]
    : null;

// Setup cancellation token source (only used for single-prompt mode)
using var globalCts = new CancellationTokenSource();

// Global Ctrl+C handler - only active during single-prompt mode or initialization
ConsoleCancelEventHandler? globalCancelHandler = null;
if (singlePrompt != null)
{
    globalCancelHandler = (_, e) =>
    {
        e.Cancel = true; // Prevent immediate termination
        globalCts.Cancel();
    };
    Console.CancelKeyPress += globalCancelHandler;
}

ConsoleStyles.Banner("LLM Connector Demo");

// Create provider using factory
var modelSelector = new ModelSelector(preferences, silentMode || singlePrompt != null);
var providerFactory = new ProviderFactory(config, modelSelector);
var provider = await providerFactory.CreateProviderAsync(args);

await using (provider)
{
    ConsoleStyles.KeyValue("Provider", provider.ProviderName);
    ConsoleStyles.Status("Initializing (this may take a moment)...");
    ConsoleStyles.Blank();

    await provider.InitializeAsync(globalCts.Token);

    ConsoleStyles.Ready();
    ConsoleStyles.Blank();

    // Single prompt mode: execute one query and exit
    if (singlePrompt != null)
    {
        ConsoleStyles.Muted("Press Ctrl+C to cancel generation.");
        ConsoleStyles.Blank();
        var session = new InteractiveSession(config, preferences);
        await session.ExecuteSinglePromptAsync(provider, singlePrompt, globalCts.Token);
    }
    else
    {
        ConsoleStyles.Info("Enter your prompts below. Press Ctrl+C during generation to cancel.");
        ConsoleStyles.Muted("Type 'help' for available commands.");
        ConsoleStyles.Blank();

        // Run interactive session - it handles its own Ctrl+C for per-request cancellation
        var session = new InteractiveSession(config, preferences);
        await session.RunAsync(provider);
    }
}

ConsoleStyles.Goodbye();