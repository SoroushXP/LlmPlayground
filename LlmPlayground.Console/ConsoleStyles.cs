using System.Text;
using Microsoft.Extensions.Configuration;

namespace LlmPlayground.Console;

/// <summary>
/// Provides beautiful, colorful console output with themes and styling.
/// </summary>
public static class ConsoleStyles
{
    private static ConsoleTheme _theme = ConsoleTheme.Vibrant;
    private static readonly object _lock = new();
    private static bool _initialized;

    /// <summary>
    /// Initializes the console styles from configuration.
    /// </summary>
    /// <param name="config">The configuration to read theme settings from.</param>
    public static void Initialize(IConfiguration config)
    {
        // Set UTF-8 encoding for proper character display
        if (!_initialized)
        {
            try
            {
                System.Console.OutputEncoding = Encoding.UTF8;
                System.Console.InputEncoding = Encoding.UTF8;
            }
            catch
            {
                // Ignore encoding errors on systems that don't support it
            }
            _initialized = true;
        }

        var section = config.GetSection("Console");
        if (!section.Exists())
        {
            _theme = ConsoleTheme.Vibrant;
            return;
        }

        // Check for preset theme
        var themeName = section.GetValue<string>("Theme");
        _theme = themeName?.ToLowerInvariant() switch
        {
            "minimal" => ConsoleTheme.Minimal,
            "none" or "disabled" => ConsoleTheme.None,
            _ => ConsoleTheme.Vibrant
        };

        // Override with specific settings if provided
        if (section.GetValue<bool?>("Enabled").HasValue)
            _theme.Enabled = section.GetValue<bool>("Enabled");
        if (section.GetValue<bool?>("ShowEmoji").HasValue)
            _theme.ShowEmoji = section.GetValue<bool>("ShowEmoji");

        // Load individual colors if specified
        TryParseColor(section, "TitleColor", c => _theme.TitleColor = c);
        TryParseColor(section, "SuccessColor", c => _theme.SuccessColor = c);
        TryParseColor(section, "WarningColor", c => _theme.WarningColor = c);
        TryParseColor(section, "ErrorColor", c => _theme.ErrorColor = c);
        TryParseColor(section, "InfoColor", c => _theme.InfoColor = c);
        TryParseColor(section, "ResponseColor", c => _theme.ResponseColor = c);
        TryParseColor(section, "PromptColor", c => _theme.PromptColor = c);
        TryParseColor(section, "MutedColor", c => _theme.MutedColor = c);
        TryParseColor(section, "AccentColor", c => _theme.AccentColor = c);
        TryParseColor(section, "LabelColor", c => _theme.LabelColor = c);
        TryParseColor(section, "ValueColor", c => _theme.ValueColor = c);
    }

    private static void TryParseColor(IConfigurationSection section, string key, Action<ConsoleColor> setter)
    {
        var value = section.GetValue<string>(key);
        if (!string.IsNullOrWhiteSpace(value) && Enum.TryParse<ConsoleColor>(value, true, out var color))
        {
            setter(color);
        }
    }

    /// <summary>
    /// Simple ASCII-compatible symbols for decoration (works on all terminals).
    /// </summary>
    public static class Emoji
    {
        public const string Sparkles = "*";
        public const string Robot = "[AI]";
        public const string Lightning = ">";
        public const string Check = "[OK]";
        public const string Cross = "[X]";
        public const string Arrow = ">";
        public const string Dot = "*";
        public const string Star = "*";
        public const string Gear = "[#]";
        public const string Chat = ">";
        public const string Brain = "[~]";
        public const string Rocket = ">>";
        public const string Warning = "[!]";
        public const string Info = "[i]";
        public const string Question = "[?]";
        public const string Hourglass = "[...]";
        public const string Success = "[OK]";
        public const string Error = "[X]";
        public const string Wave = "~";
    }

    /// <summary>
    /// Gets or sets the current theme.
    /// </summary>
    public static ConsoleTheme Theme
    {
        get => _theme;
        set => _theme = value ?? ConsoleTheme.Vibrant;
    }

    /// <summary>
    /// Writes a banner/title with decorations.
    /// </summary>
    public static void Banner(string text, char borderChar = '=')
    {
        var line = new string(borderChar, text.Length + 8);

        WriteLineColored($"\n{line}", _theme.TitleColor);
        WriteColored($"{borderChar}{borderChar}  ", _theme.TitleColor);
        WriteColored(text, _theme.AccentColor);
        WriteLineColored($"  {borderChar}{borderChar}", _theme.TitleColor);
        WriteLineColored($"{line}\n", _theme.TitleColor);
    }

    /// <summary>
    /// Writes a section header.
    /// </summary>
    public static void Header(string text, string emoji = null!)
    {
        var icon = _theme.ShowEmoji && emoji != null ? $"{emoji} " : "";
        WriteLineColored($"\n{icon}{text}", _theme.TitleColor);
        WriteLineColored(new string('-', text.Length + icon.Length), _theme.MutedColor);
    }

    /// <summary>
    /// Writes a success message.
    /// </summary>
    public static void Success(string text)
    {
        var icon = _theme.ShowEmoji ? $"{Emoji.Check} " : "";
        WriteLineColored($"{icon}{text}", _theme.SuccessColor);
    }

    /// <summary>
    /// Writes a warning message.
    /// </summary>
    public static void Warning(string text)
    {
        var icon = _theme.ShowEmoji ? $"{Emoji.Warning} " : "";
        WriteLineColored($"{icon}{text}", _theme.WarningColor);
    }

    /// <summary>
    /// Writes an error message.
    /// </summary>
    public static void Error(string text)
    {
        var icon = _theme.ShowEmoji ? $"{Emoji.Cross} " : "";
        WriteLineColored($"{icon}{text}", _theme.ErrorColor);
    }

    /// <summary>
    /// Writes an info message.
    /// </summary>
    public static void Info(string text)
    {
        var icon = _theme.ShowEmoji ? $"{Emoji.Info} " : "";
        WriteLineColored($"{icon}{text}", _theme.InfoColor);
    }

    /// <summary>
    /// Writes muted/secondary text.
    /// </summary>
    public static void Muted(string text) => WriteLineColored(text, _theme.MutedColor);

    /// <summary>
    /// Writes a label-value pair.
    /// </summary>
    public static void KeyValue(string label, object value, string separator = ":")
    {
        WriteColored($"  {label}{separator} ", _theme.LabelColor);
        WriteLineColored(value?.ToString() ?? "null", _theme.ValueColor);
    }

    /// <summary>
    /// Writes a list item with bullet.
    /// </summary>
    public static void ListItem(string text, int indent = 0)
    {
        var bullet = _theme.ShowEmoji ? Emoji.Dot : "-";
        var padding = new string(' ', indent * 2);
        WriteColored($"{padding}{bullet} ", _theme.AccentColor);
        WriteLineColored(text, _theme.ValueColor);
    }

    /// <summary>
    /// Writes the user prompt indicator.
    /// </summary>
    public static void Prompt(string prefix = "", bool streaming = false)
    {
        var icon = _theme.ShowEmoji ? Emoji.Arrow : ">";
        var streamLabel = streaming ? "[Streaming] " : "";
        WriteColored($"{prefix}{streamLabel}{icon} ", _theme.PromptColor);
    }

    /// <summary>
    /// Writes AI response text.
    /// </summary>
    public static void Response(string text) => WriteLineColored(text, _theme.ResponseColor);

    /// <summary>
    /// Writes streaming response token.
    /// </summary>
    public static void ResponseToken(string token) => WriteColored(token, _theme.ResponseColor);

    /// <summary>
    /// Writes a progress/status message.
    /// </summary>
    public static void Status(string text)
    {
        var icon = _theme.ShowEmoji ? $"{Emoji.Hourglass} " : "";
        WriteLineColored($"{icon}{text}", _theme.InfoColor);
    }

    /// <summary>
    /// Writes a "ready" message.
    /// </summary>
    public static void Ready(string text = "Ready!")
    {
        var icon = _theme.ShowEmoji ? $"{Emoji.Rocket} " : "";
        WriteLineColored($"{icon}{text}", _theme.SuccessColor);
    }

    /// <summary>
    /// Writes a goodbye message.
    /// </summary>
    public static void Goodbye(string text = "Goodbye!")
    {
        var icon = _theme.ShowEmoji ? $"{Emoji.Wave} " : "";
        WriteLineColored($"\n{icon}{text}", _theme.InfoColor);
    }

    /// <summary>
    /// Writes a numbered menu item.
    /// </summary>
    public static void MenuItem(int number, string text, bool isSelected = false)
    {
        var bullet = _theme.ShowEmoji ? Emoji.Dot : "*";
        if (isSelected)
        {
            WriteColored($"  {bullet} ", _theme.SuccessColor);
            WriteColored($"{number}. ", _theme.AccentColor);
            WriteLineColored(text, _theme.SuccessColor);
        }
        else
        {
            WriteColored($"    {number}. ", _theme.MutedColor);
            WriteLineColored(text, _theme.ValueColor);
        }
    }

    /// <summary>
    /// Writes connection info.
    /// </summary>
    public static void Connection(string label, string url)
    {
        var icon = _theme.ShowEmoji ? $"{Emoji.Lightning} " : "";
        WriteColored($"{icon}{label}: ", _theme.LabelColor);
        WriteLineColored(url, _theme.AccentColor);
    }

    /// <summary>
    /// Writes model info.
    /// </summary>
    public static void Model(string modelName)
    {
        var icon = _theme.ShowEmoji ? $"{Emoji.Brain} " : "";
        WriteColored($"{icon}Model: ", _theme.LabelColor);
        WriteLineColored(modelName, _theme.AccentColor);
    }

    /// <summary>
    /// Writes stats/metrics line.
    /// </summary>
    public static void Stats(int tokens, double seconds)
    {
        var icon = _theme.ShowEmoji ? $"{Emoji.Lightning} " : "";
        WriteLineColored($"{icon}[Tokens: {tokens}, Time: {seconds:F2}s]", _theme.MutedColor);
    }

    /// <summary>
    /// Writes a horizontal divider.
    /// </summary>
    public static void Divider(char character = '-', int length = 40)
    {
        WriteLineColored(new string(character, length), _theme.MutedColor);
    }

    /// <summary>
    /// Writes empty line.
    /// </summary>
    public static void Blank() => System.Console.WriteLine();

    /// <summary>
    /// Core method to write colored text without newline.
    /// </summary>
    private static void WriteColored(string text, ConsoleColor color)
    {
        if (!_theme.Enabled)
        {
            System.Console.Write(text);
            return;
        }

        lock (_lock)
        {
            var original = System.Console.ForegroundColor;
            System.Console.ForegroundColor = color;
            System.Console.Write(text);
            System.Console.ForegroundColor = original;
        }
    }

    /// <summary>
    /// Core method to write colored text with newline.
    /// </summary>
    private static void WriteLineColored(string text, ConsoleColor color)
    {
        if (!_theme.Enabled)
        {
            System.Console.WriteLine(text);
            return;
        }

        lock (_lock)
        {
            var original = System.Console.ForegroundColor;
            System.Console.ForegroundColor = color;
            System.Console.WriteLine(text);
            System.Console.ForegroundColor = original;
        }
    }
}

