namespace LlmPlayground.Utilities.Logging;

/// <summary>
/// Represents a single log entry with all associated metadata.
/// </summary>
public sealed record LogEntry
{
    /// <summary>
    /// Gets the timestamp when the log entry was created (UTC).
    /// </summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets the severity level of this log entry.
    /// </summary>
    public LogLevel Level { get; init; } = LogLevel.Information;

    /// <summary>
    /// Gets the log message.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Gets the source/category of the log entry (e.g., class name, component).
    /// </summary>
    public string? Source { get; init; }

    /// <summary>
    /// Gets the exception associated with this log entry, if any.
    /// </summary>
    public Exception? Exception { get; init; }

    /// <summary>
    /// Gets additional structured properties for the log entry.
    /// </summary>
    public IReadOnlyDictionary<string, object?>? Properties { get; init; }

    /// <summary>
    /// Gets the thread ID where the log entry was created.
    /// </summary>
    public int ThreadId { get; init; } = Environment.CurrentManagedThreadId;

    /// <summary>
    /// Gets the correlation ID for tracing related operations.
    /// </summary>
    public string? CorrelationId { get; init; }

    /// <summary>
    /// Indicates whether this entry was captured from console output.
    /// </summary>
    public bool IsConsoleCapture { get; init; }
}

/// <summary>
/// Builder for creating log entries with a fluent API.
/// </summary>
public sealed class LogEntryBuilder
{
    private LogLevel _level = LogLevel.Information;
    private string _message = string.Empty;
    private string? _source;
    private Exception? _exception;
    private Dictionary<string, object?>? _properties;
    private string? _correlationId;
    private bool _isConsoleCapture;

    /// <summary>
    /// Sets the log level.
    /// </summary>
    public LogEntryBuilder WithLevel(LogLevel level)
    {
        _level = level;
        return this;
    }

    /// <summary>
    /// Sets the log message.
    /// </summary>
    public LogEntryBuilder WithMessage(string message)
    {
        _message = message;
        return this;
    }

    /// <summary>
    /// Sets the log source/category.
    /// </summary>
    public LogEntryBuilder WithSource(string source)
    {
        _source = source;
        return this;
    }

    /// <summary>
    /// Sets an exception for error logs.
    /// </summary>
    public LogEntryBuilder WithException(Exception exception)
    {
        _exception = exception;
        return this;
    }

    /// <summary>
    /// Adds a structured property to the log entry.
    /// </summary>
    public LogEntryBuilder WithProperty(string key, object? value)
    {
        _properties ??= new Dictionary<string, object?>();
        _properties[key] = value;
        return this;
    }

    /// <summary>
    /// Adds multiple structured properties.
    /// </summary>
    public LogEntryBuilder WithProperties(IEnumerable<KeyValuePair<string, object?>> properties)
    {
        _properties ??= new Dictionary<string, object?>();
        foreach (var kvp in properties)
        {
            _properties[kvp.Key] = kvp.Value;
        }
        return this;
    }

    /// <summary>
    /// Sets the correlation ID for distributed tracing.
    /// </summary>
    public LogEntryBuilder WithCorrelationId(string correlationId)
    {
        _correlationId = correlationId;
        return this;
    }

    /// <summary>
    /// Marks this entry as captured from console output.
    /// </summary>
    public LogEntryBuilder AsConsoleCapture(bool isCapture = true)
    {
        _isConsoleCapture = isCapture;
        return this;
    }

    /// <summary>
    /// Builds the log entry.
    /// </summary>
    public LogEntry Build()
    {
        return new LogEntry
        {
            Level = _level,
            Message = _message,
            Source = _source,
            Exception = _exception,
            Properties = _properties,
            CorrelationId = _correlationId,
            IsConsoleCapture = _isConsoleCapture
        };
    }
}

