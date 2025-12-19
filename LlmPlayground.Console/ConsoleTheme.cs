namespace LlmPlayground.Console;

/// <summary>
/// Configuration for console theme colors and styles.
/// </summary>
public class ConsoleTheme
{
    /// <summary>
    /// Whether to enable colorful console output. Default: true.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Whether to show emoji/unicode decorations. Default: true.
    /// </summary>
    public bool ShowEmoji { get; set; } = true;

    /// <summary>
    /// Color for titles and headers.
    /// </summary>
    public ConsoleColor TitleColor { get; set; } = ConsoleColor.Cyan;

    /// <summary>
    /// Color for success messages.
    /// </summary>
    public ConsoleColor SuccessColor { get; set; } = ConsoleColor.Green;

    /// <summary>
    /// Color for warning messages.
    /// </summary>
    public ConsoleColor WarningColor { get; set; } = ConsoleColor.Yellow;

    /// <summary>
    /// Color for error messages.
    /// </summary>
    public ConsoleColor ErrorColor { get; set; } = ConsoleColor.Red;

    /// <summary>
    /// Color for informational messages.
    /// </summary>
    public ConsoleColor InfoColor { get; set; } = ConsoleColor.Blue;

    /// <summary>
    /// Color for AI/LLM responses.
    /// </summary>
    public ConsoleColor ResponseColor { get; set; } = ConsoleColor.Magenta;

    /// <summary>
    /// Color for the user prompt indicator.
    /// </summary>
    public ConsoleColor PromptColor { get; set; } = ConsoleColor.Green;

    /// <summary>
    /// Color for muted/secondary text.
    /// </summary>
    public ConsoleColor MutedColor { get; set; } = ConsoleColor.DarkGray;

    /// <summary>
    /// Color for highlighted/accent text.
    /// </summary>
    public ConsoleColor AccentColor { get; set; } = ConsoleColor.White;

    /// <summary>
    /// Color for labels and keys.
    /// </summary>
    public ConsoleColor LabelColor { get; set; } = ConsoleColor.DarkCyan;

    /// <summary>
    /// Color for values.
    /// </summary>
    public ConsoleColor ValueColor { get; set; } = ConsoleColor.White;

    /// <summary>
    /// Gets a dark/minimal theme.
    /// </summary>
    public static ConsoleTheme Minimal => new()
    {
        ShowEmoji = false,
        TitleColor = ConsoleColor.White,
        SuccessColor = ConsoleColor.Gray,
        WarningColor = ConsoleColor.Gray,
        ErrorColor = ConsoleColor.DarkRed,
        InfoColor = ConsoleColor.Gray,
        ResponseColor = ConsoleColor.White,
        PromptColor = ConsoleColor.Gray,
        MutedColor = ConsoleColor.DarkGray,
        AccentColor = ConsoleColor.White,
        LabelColor = ConsoleColor.Gray,
        ValueColor = ConsoleColor.White
    };

    /// <summary>
    /// Gets a vibrant colorful theme.
    /// </summary>
    public static ConsoleTheme Vibrant => new()
    {
        ShowEmoji = true,
        TitleColor = ConsoleColor.Cyan,
        SuccessColor = ConsoleColor.Green,
        WarningColor = ConsoleColor.Yellow,
        ErrorColor = ConsoleColor.Red,
        InfoColor = ConsoleColor.Blue,
        ResponseColor = ConsoleColor.Magenta,
        PromptColor = ConsoleColor.Green,
        MutedColor = ConsoleColor.DarkGray,
        AccentColor = ConsoleColor.White,
        LabelColor = ConsoleColor.DarkCyan,
        ValueColor = ConsoleColor.White
    };

    /// <summary>
    /// Gets a disabled/no-color theme.
    /// </summary>
    public static ConsoleTheme None => new()
    {
        Enabled = false,
        ShowEmoji = false
    };
}

