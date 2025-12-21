namespace LlmPlayground.Utilities.Logging;

/// <summary>
/// Central logging service interface for the application.
/// </summary>
public interface ILoggerService : IAsyncDisposable, IDisposable
{
    /// <summary>
    /// Gets or sets the global minimum log level.
    /// </summary>
    LogLevel MinimumLevel { get; set; }

    /// <summary>
    /// Gets or sets the current correlation ID for distributed tracing.
    /// </summary>
    string? CorrelationId { get; set; }

    /// <summary>
    /// Gets a value indicating whether console interception is active.
    /// </summary>
    bool IsConsoleInterceptorActive { get; }

    /// <summary>
    /// Logs an entry at the specified level.
    /// </summary>
    void Log(LogLevel level, string message, string? source = null, Exception? exception = null);

    /// <summary>
    /// Logs an entry with additional properties.
    /// </summary>
    void Log(LogEntry entry);

    /// <summary>
    /// Logs at Trace level.
    /// </summary>
    void Trace(string message, string? source = null);

    /// <summary>
    /// Logs at Debug level.
    /// </summary>
    void Debug(string message, string? source = null);

    /// <summary>
    /// Logs at Information level.
    /// </summary>
    void Information(string message, string? source = null);

    /// <summary>
    /// Logs at Warning level.
    /// </summary>
    void Warning(string message, string? source = null, Exception? exception = null);

    /// <summary>
    /// Logs at Error level.
    /// </summary>
    void Error(string message, string? source = null, Exception? exception = null);

    /// <summary>
    /// Logs at Critical level.
    /// </summary>
    void Critical(string message, string? source = null, Exception? exception = null);

    /// <summary>
    /// Creates a scoped logger with a specific source name.
    /// </summary>
    ILoggerScope CreateScope(string source);

    /// <summary>
    /// Creates a correlation scope for distributed tracing.
    /// </summary>
    IDisposable BeginCorrelationScope(string? correlationId = null);

    /// <summary>
    /// Adds a log sink to the logger.
    /// </summary>
    void AddSink(ILogSink sink);

    /// <summary>
    /// Removes a log sink by name.
    /// </summary>
    bool RemoveSink(string sinkName);

    /// <summary>
    /// Gets a sink by name.
    /// </summary>
    ILogSink? GetSink(string sinkName);

    /// <summary>
    /// Starts intercepting console output.
    /// </summary>
    void StartConsoleInterceptor();

    /// <summary>
    /// Stops intercepting console output and restores original streams.
    /// </summary>
    void StopConsoleInterceptor();

    /// <summary>
    /// Flushes all sinks.
    /// </summary>
    ValueTask FlushAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Scoped logger with a specific source context.
/// </summary>
public interface ILoggerScope
{
    /// <summary>
    /// Gets the source name for this scope.
    /// </summary>
    string Source { get; }

    /// <summary>
    /// Logs at Trace level.
    /// </summary>
    void Trace(string message);

    /// <summary>
    /// Logs at Debug level.
    /// </summary>
    void Debug(string message);

    /// <summary>
    /// Logs at Information level.
    /// </summary>
    void Information(string message);

    /// <summary>
    /// Logs at Warning level.
    /// </summary>
    void Warning(string message, Exception? exception = null);

    /// <summary>
    /// Logs at Error level.
    /// </summary>
    void Error(string message, Exception? exception = null);

    /// <summary>
    /// Logs at Critical level.
    /// </summary>
    void Critical(string message, Exception? exception = null);

    /// <summary>
    /// Logs an entry with additional properties.
    /// </summary>
    void Log(LogLevel level, string message, Exception? exception = null, IReadOnlyDictionary<string, object?>? properties = null);
}

