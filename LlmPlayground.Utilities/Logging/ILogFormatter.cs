using System.Text;

namespace LlmPlayground.Utilities.Logging;

/// <summary>
/// Defines how log entries are formatted for output.
/// </summary>
public interface ILogFormatter
{
    /// <summary>
    /// Formats a log entry into a string representation.
    /// </summary>
    /// <param name="entry">The log entry to format.</param>
    /// <returns>The formatted log string.</returns>
    string Format(LogEntry entry);
}

/// <summary>
/// Default log formatter with configurable options.
/// </summary>
public sealed class DefaultLogFormatter : ILogFormatter
{
    private readonly LogFormatterOptions _options;

    /// <summary>
    /// Initializes a new instance with default options.
    /// </summary>
    public DefaultLogFormatter() : this(new LogFormatterOptions()) { }

    /// <summary>
    /// Initializes a new instance with the specified options.
    /// </summary>
    public DefaultLogFormatter(LogFormatterOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc />
    public string Format(LogEntry entry)
    {
        var sb = new StringBuilder(256);

        // Timestamp
        if (_options.IncludeTimestamp)
        {
            var timestamp = _options.UseUtcTimestamp
                ? entry.Timestamp.UtcDateTime
                : entry.Timestamp.LocalDateTime;

            sb.Append(timestamp.ToString(_options.TimestampFormat));
            sb.Append(' ');
        }

        // Level
        if (_options.IncludeLevel)
        {
            sb.Append('[');
            sb.Append(FormatLevel(entry.Level));
            sb.Append(']');
            sb.Append(' ');
        }

        // Thread ID
        if (_options.IncludeThreadId)
        {
            sb.Append('[');
            sb.Append(entry.ThreadId.ToString().PadLeft(3));
            sb.Append(']');
            sb.Append(' ');
        }

        // Correlation ID
        if (_options.IncludeCorrelationId && !string.IsNullOrEmpty(entry.CorrelationId))
        {
            sb.Append('<');
            sb.Append(entry.CorrelationId);
            sb.Append('>');
            sb.Append(' ');
        }

        // Source
        if (_options.IncludeSource && !string.IsNullOrEmpty(entry.Source))
        {
            sb.Append(entry.Source);
            sb.Append(": ");
        }

        // Message
        sb.Append(entry.Message);

        // Properties
        if (_options.IncludeProperties && entry.Properties?.Count > 0)
        {
            sb.Append(" {");
            var first = true;
            foreach (var kvp in entry.Properties)
            {
                if (!first) sb.Append(", ");
                sb.Append(kvp.Key);
                sb.Append('=');
                sb.Append(kvp.Value?.ToString() ?? "null");
                first = false;
            }
            sb.Append('}');
        }

        // Exception
        if (entry.Exception is not null)
        {
            sb.AppendLine();
            if (_options.IncludeFullException)
            {
                sb.Append(entry.Exception.ToString());
            }
            else
            {
                sb.Append("  Exception: ");
                sb.Append(entry.Exception.GetType().Name);
                sb.Append(" - ");
                sb.Append(entry.Exception.Message);
            }
        }

        return sb.ToString();
    }

    private string FormatLevel(LogLevel level)
    {
        return _options.UseShortLevelNames
            ? level switch
            {
                LogLevel.Trace => "TRC",
                LogLevel.Debug => "DBG",
                LogLevel.Information => "INF",
                LogLevel.Console => "CON",
                LogLevel.Warning => "WRN",
                LogLevel.Error => "ERR",
                LogLevel.Critical => "CRT",
                _ => "???"
            }
            : level.ToString().ToUpperInvariant().PadRight(11);
    }
}

/// <summary>
/// Configuration options for log formatting.
/// </summary>
public sealed record LogFormatterOptions
{
    /// <summary>
    /// Whether to include timestamp in log output.
    /// </summary>
    public bool IncludeTimestamp { get; init; } = true;

    /// <summary>
    /// Whether to use UTC time for timestamps.
    /// </summary>
    public bool UseUtcTimestamp { get; init; } = true;

    /// <summary>
    /// The timestamp format string.
    /// </summary>
    public string TimestampFormat { get; init; } = "yyyy-MM-dd HH:mm:ss.fff";

    /// <summary>
    /// Whether to include log level in output.
    /// </summary>
    public bool IncludeLevel { get; init; } = true;

    /// <summary>
    /// Whether to use short level names (TRC, DBG, INF, etc.).
    /// </summary>
    public bool UseShortLevelNames { get; init; } = true;

    /// <summary>
    /// Whether to include thread ID in output.
    /// </summary>
    public bool IncludeThreadId { get; init; } = false;

    /// <summary>
    /// Whether to include correlation ID in output.
    /// </summary>
    public bool IncludeCorrelationId { get; init; } = true;

    /// <summary>
    /// Whether to include source/category in output.
    /// </summary>
    public bool IncludeSource { get; init; } = true;

    /// <summary>
    /// Whether to include structured properties in output.
    /// </summary>
    public bool IncludeProperties { get; init; } = true;

    /// <summary>
    /// Whether to include full exception stack trace.
    /// </summary>
    public bool IncludeFullException { get; init; } = true;
}

/// <summary>
/// JSON log formatter for structured logging output.
/// </summary>
public sealed class JsonLogFormatter : ILogFormatter
{
    /// <inheritdoc />
    public string Format(LogEntry entry)
    {
        var properties = new Dictionary<string, object?>
        {
            ["timestamp"] = entry.Timestamp.ToString("o"),
            ["level"] = entry.Level.ToString(),
            ["message"] = entry.Message,
            ["threadId"] = entry.ThreadId
        };

        if (!string.IsNullOrEmpty(entry.Source))
            properties["source"] = entry.Source;

        if (!string.IsNullOrEmpty(entry.CorrelationId))
            properties["correlationId"] = entry.CorrelationId;

        if (entry.IsConsoleCapture)
            properties["isConsoleCapture"] = true;

        if (entry.Properties?.Count > 0)
        {
            foreach (var kvp in entry.Properties)
            {
                properties[$"prop_{kvp.Key}"] = kvp.Value;
            }
        }

        if (entry.Exception is not null)
        {
            properties["exception"] = new Dictionary<string, object?>
            {
                ["type"] = entry.Exception.GetType().FullName,
                ["message"] = entry.Exception.Message,
                ["stackTrace"] = entry.Exception.StackTrace
            };
        }

        return System.Text.Json.JsonSerializer.Serialize(properties);
    }
}

