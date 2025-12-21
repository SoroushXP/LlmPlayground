namespace LlmPlayground.Utilities.Logging;

/// <summary>
/// Defines a destination for log entries (console, file, etc.).
/// </summary>
public interface ILogSink : IAsyncDisposable, IDisposable
{
    /// <summary>
    /// Gets the unique name of this sink.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets or sets the minimum log level this sink will process.
    /// </summary>
    LogLevel MinimumLevel { get; set; }

    /// <summary>
    /// Gets a value indicating whether this sink is enabled.
    /// </summary>
    bool IsEnabled { get; }

    /// <summary>
    /// Writes a log entry to this sink.
    /// </summary>
    /// <param name="entry">The log entry to write.</param>
    void Write(LogEntry entry);

    /// <summary>
    /// Writes a log entry to this sink asynchronously.
    /// </summary>
    /// <param name="entry">The log entry to write.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the async operation.</returns>
    ValueTask WriteAsync(LogEntry entry, CancellationToken cancellationToken = default);

    /// <summary>
    /// Flushes any buffered log entries.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the async operation.</returns>
    ValueTask FlushAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Base implementation for log sinks providing common functionality.
/// </summary>
public abstract class LogSinkBase : ILogSink
{
    private bool _disposed;

    /// <inheritdoc />
    public abstract string Name { get; }

    /// <inheritdoc />
    public LogLevel MinimumLevel { get; set; } = LogLevel.Trace;

    /// <inheritdoc />
    public bool IsEnabled => !_disposed;

    /// <summary>
    /// Gets the log formatter used by this sink.
    /// </summary>
    protected ILogFormatter Formatter { get; }

    /// <summary>
    /// Initializes a new instance of the sink with the specified formatter.
    /// </summary>
    protected LogSinkBase(ILogFormatter? formatter = null)
    {
        Formatter = formatter ?? new DefaultLogFormatter();
    }

    /// <inheritdoc />
    public void Write(LogEntry entry)
    {
        if (_disposed || entry.Level < MinimumLevel)
            return;

        WriteCore(entry);
    }

    /// <inheritdoc />
    public async ValueTask WriteAsync(LogEntry entry, CancellationToken cancellationToken = default)
    {
        if (_disposed || entry.Level < MinimumLevel)
            return;

        await WriteCoreAsync(entry, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Core write implementation to be overridden by derived classes.
    /// </summary>
    protected abstract void WriteCore(LogEntry entry);

    /// <summary>
    /// Core async write implementation. Override for true async behavior.
    /// </summary>
    protected virtual ValueTask WriteCoreAsync(LogEntry entry, CancellationToken cancellationToken)
    {
        WriteCore(entry);
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public virtual ValueTask FlushAsync(CancellationToken cancellationToken = default)
        => ValueTask.CompletedTask;

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await DisposeAsyncCore().ConfigureAwait(false);
        Dispose(disposing: false);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes managed resources.
    /// </summary>
    protected virtual void Dispose(bool disposing)
    {
        _disposed = true;
    }

    /// <summary>
    /// Disposes resources asynchronously.
    /// </summary>
    protected virtual ValueTask DisposeAsyncCore()
        => ValueTask.CompletedTask;
}

