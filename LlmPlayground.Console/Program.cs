using LlmPlayground.Console;
using LlmPlayground.Core;
using LlmPlayground.Services.Extensions;
using LlmPlayground.Services.Interfaces;
using LlmPlayground.Utilities.Validation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

// Build configuration from appsettings.json
var config = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .Build();

// Initialize console styling from configuration
ConsoleStyles.Initialize(config);

// Check for silent mode and single prompt mode early (needed for DI setup)
var silentMode = args.Contains("--silent") || args.Contains("-s");
var promptIndex = Array.FindIndex(args, a => a == "--prompt" || a == "-p");
var singlePrompt = promptIndex >= 0 && promptIndex + 1 < args.Length
    ? args[promptIndex + 1]
    : null;

// Build DI container with services
var services = new ServiceCollection();
services.AddSingleton<IConfiguration>(config);
services.AddLogging(builder =>
{
    builder.AddConfiguration(config.GetSection("Logging"));
    // Only add console logging if explicitly enabled - default to no console logging
    if (config.GetValue("Logging:EnableConsole", false))
    {
        builder.AddConsole();
    }
});

// Add LlmPlayground services (ILlmService, IPrologService, IPromptLabService)
services.AddLlmPlaygroundServices();

// Add validation services
services.AddSingleton<IRequestValidator, RequestValidator>();

// Add console-specific services
services.AddSingleton<UserPreferences>(_ => UserPreferences.Load());
services.AddSingleton(new CommandLineOptions(silentMode, singlePrompt));
services.AddSingleton<ModelSelector>();
services.AddSingleton<ServiceInitializer>();
services.AddSingleton<InteractiveSession>();

await using var serviceProvider = services.BuildServiceProvider();

// Setup cancellation token source
using var globalCts = new CancellationTokenSource();

// Global Ctrl+C handler - only active during single-prompt mode or initialization
if (singlePrompt != null)
{
    Console.CancelKeyPress += (_, e) =>
    {
        e.Cancel = true; // Prevent immediate termination
        globalCts.Cancel();
    };
}

ConsoleStyles.Banner("LLM Connector Demo");

// Get services from DI
var llmService = serviceProvider.GetRequiredService<ILlmService>();
var serviceInitializer = serviceProvider.GetRequiredService<ServiceInitializer>();
var session = serviceProvider.GetRequiredService<InteractiveSession>();

// Determine provider type from configuration and initialize service
var providerTypeStr = config.GetValue<string>("Provider", "Ollama") ?? "Ollama";
var providerType = Enum.TryParse<LlmProviderType>(providerTypeStr, ignoreCase: true, out var pt)
    ? pt
    : LlmProviderType.Ollama;

await serviceInitializer.InitializeAsync(llmService, providerType, globalCts.Token);

ConsoleStyles.KeyValue("Provider", llmService.CurrentProvider);
if (llmService.CurrentModel != null)
{
    ConsoleStyles.KeyValue("Model", llmService.CurrentModel);
}
ConsoleStyles.Ready();
ConsoleStyles.Blank();

// Single prompt mode: execute one query and exit
if (singlePrompt != null)
{
    ConsoleStyles.Muted("Press Ctrl+C to cancel generation.");
    ConsoleStyles.Blank();
    await session.ExecuteSinglePromptAsync(singlePrompt, globalCts.Token);
}
else
{
    ConsoleStyles.Info("Enter your prompts below. Press Ctrl+C during generation to cancel.");
    ConsoleStyles.Muted("Type 'help' for available commands.");
    ConsoleStyles.Blank();

    // Run interactive session - it handles its own Ctrl+C for per-request cancellation
    await session.RunAsync();
}

ConsoleStyles.Goodbye();