namespace LlmPlayground.Utilities.Logging.Sinks;

/// <summary>
/// Log sink that writes to the console with optional color support.
/// </summary>
public sealed class ConsoleSink : LogSinkBase
{
    private readonly object _lock = new();
    private readonly TextWriter _output;
    private readonly TextWriter _errorOutput;
    private readonly bool _useColors;
    private readonly bool _writeToStdErr;

    /// <inheritdoc />
    public override string Name => "Console";

    /// <summary>
    /// Initializes a new console sink with default settings.
    /// </summary>
    public ConsoleSink(
        ILogFormatter? formatter = null,
        bool useColors = true,
        bool writeErrorsToStdErr = true)
        : this(Console.Out, Console.Error, formatter, useColors, writeErrorsToStdErr)
    {
    }

    /// <summary>
    /// Initializes a new console sink with custom output streams (useful for testing).
    /// </summary>
    public ConsoleSink(
        TextWriter output,
        TextWriter errorOutput,
        ILogFormatter? formatter = null,
        bool useColors = true,
        bool writeErrorsToStdErr = true)
        : base(formatter)
    {
        _output = output ?? throw new ArgumentNullException(nameof(output));
        _errorOutput = errorOutput ?? throw new ArgumentNullException(nameof(errorOutput));
        _useColors = useColors && !Console.IsOutputRedirected;
        _writeToStdErr = writeErrorsToStdErr;
    }

    /// <inheritdoc />
    protected override void WriteCore(LogEntry entry)
    {
        var formattedMessage = Formatter.Format(entry);
        var writer = ShouldUseStdErr(entry.Level) ? _errorOutput : _output;

        if (_useColors)
        {
            WriteWithColor(writer, formattedMessage, GetColorForLevel(entry.Level));
        }
        else
        {
            lock (_lock)
            {
                writer.WriteLine(formattedMessage);
            }
        }
    }

    private void WriteWithColor(TextWriter writer, string message, ConsoleColor color)
    {
        lock (_lock)
        {
            var originalColor = Console.ForegroundColor;
            try
            {
                Console.ForegroundColor = color;
                writer.WriteLine(message);
            }
            finally
            {
                Console.ForegroundColor = originalColor;
            }
        }
    }

    private bool ShouldUseStdErr(LogLevel level)
    {
        return _writeToStdErr && level >= LogLevel.Error;
    }

    private static ConsoleColor GetColorForLevel(LogLevel level)
    {
        return level switch
        {
            LogLevel.Trace => ConsoleColor.DarkGray,
            LogLevel.Debug => ConsoleColor.Gray,
            LogLevel.Information => ConsoleColor.White,
            LogLevel.Console => ConsoleColor.Cyan,
            LogLevel.Warning => ConsoleColor.Yellow,
            LogLevel.Error => ConsoleColor.Red,
            LogLevel.Critical => ConsoleColor.DarkRed,
            _ => ConsoleColor.White
        };
    }
}

/// <summary>
/// Console sink that filters out console captures to prevent infinite loops.
/// Use this when console interception is enabled.
/// </summary>
public sealed class FilteredConsoleSink : LogSinkBase
{
    private readonly ConsoleSink _innerSink;

    /// <inheritdoc />
    public override string Name => "FilteredConsole";

    /// <summary>
    /// Initializes a new filtered console sink.
    /// </summary>
    public FilteredConsoleSink(ILogFormatter? formatter = null, bool useColors = true)
        : base(formatter)
    {
        _innerSink = new ConsoleSink(formatter, useColors);
    }

    /// <inheritdoc />
    protected override void WriteCore(LogEntry entry)
    {
        // Skip console captures to prevent infinite recursion
        if (entry.IsConsoleCapture)
            return;

        _innerSink.Write(entry);
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _innerSink.Dispose();
        }
        base.Dispose(disposing);
    }
}

