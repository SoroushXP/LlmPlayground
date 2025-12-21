namespace LlmPlayground.Console;

/// <summary>
/// Command line options parsed from arguments.
/// </summary>
/// <param name="SilentMode">Whether to run in silent mode (non-interactive model selection).</param>
/// <param name="SinglePrompt">Optional single prompt to execute and exit.</param>
public record CommandLineOptions(bool SilentMode, string? SinglePrompt);

