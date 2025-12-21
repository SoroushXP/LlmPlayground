using System.Collections.Concurrent;

namespace LlmPlayground.Utilities.Logging;

/// <summary>
/// Central logging service implementation with support for multiple sinks and console interception.
/// </summary>
public sealed class LoggerService : ILoggerService
{
    private readonly ConcurrentDictionary<string, ILogSink> _sinks = new();
    private readonly AsyncLocal<string?> _correlationId = new();
    private readonly object _interceptorLock = new();
    
    private ConsoleInterceptor? _consoleInterceptor;
    private bool _disposed;

    /// <inheritdoc />
    public LogLevel MinimumLevel { get; set; } = LogLevel.Information;

    /// <inheritdoc />
    public string? CorrelationId
    {
        get => _correlationId.Value;
        set => _correlationId.Value = value;
    }

    /// <inheritdoc />
    public bool IsConsoleInterceptorActive => _consoleInterceptor?.IsActive ?? false;

    /// <inheritdoc />
    public void Log(LogLevel level, string message, string? source = null, Exception? exception = null)
    {
        if (_disposed || level < MinimumLevel)
            return;

        var entry = new LogEntry
        {
            Level = level,
            Message = message,
            Source = source,
            Exception = exception,
            CorrelationId = _correlationId.Value
        };

        LogCore(entry);
    }

    /// <inheritdoc />
    public void Log(LogEntry entry)
    {
        if (_disposed || entry.Level < MinimumLevel)
            return;

        // Enrich with correlation ID if not set
        if (string.IsNullOrEmpty(entry.CorrelationId) && !string.IsNullOrEmpty(_correlationId.Value))
        {
            entry = entry with { CorrelationId = _correlationId.Value };
        }

        LogCore(entry);
    }

    private void LogCore(LogEntry entry)
    {
        foreach (var sink in _sinks.Values)
        {
            if (sink.IsEnabled && entry.Level >= sink.MinimumLevel)
            {
                try
                {
                    sink.Write(entry);
                }
                catch
                {
                    // Swallow sink errors to prevent logging from crashing the application
                }
            }
        }
    }

    /// <inheritdoc />
    public void Trace(string message, string? source = null)
        => Log(LogLevel.Trace, message, source);

    /// <inheritdoc />
    public void Debug(string message, string? source = null)
        => Log(LogLevel.Debug, message, source);

    /// <inheritdoc />
    public void Information(string message, string? source = null)
        => Log(LogLevel.Information, message, source);

    /// <inheritdoc />
    public void Warning(string message, string? source = null, Exception? exception = null)
        => Log(LogLevel.Warning, message, source, exception);

    /// <inheritdoc />
    public void Error(string message, string? source = null, Exception? exception = null)
        => Log(LogLevel.Error, message, source, exception);

    /// <inheritdoc />
    public void Critical(string message, string? source = null, Exception? exception = null)
        => Log(LogLevel.Critical, message, source, exception);

    /// <inheritdoc />
    public ILoggerScope CreateScope(string source)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(source);
        return new LoggerScope(this, source);
    }

    /// <inheritdoc />
    public IDisposable BeginCorrelationScope(string? correlationId = null)
    {
        var id = correlationId ?? Guid.NewGuid().ToString("N")[..12];
        var previousId = _correlationId.Value;
        _correlationId.Value = id;
        return new CorrelationScope(this, previousId);
    }

    /// <inheritdoc />
    public void AddSink(ILogSink sink)
    {
        ArgumentNullException.ThrowIfNull(sink);
        _sinks.TryAdd(sink.Name, sink);
    }

    /// <inheritdoc />
    public bool RemoveSink(string sinkName)
    {
        return _sinks.TryRemove(sinkName, out _);
    }

    /// <inheritdoc />
    public ILogSink? GetSink(string sinkName)
    {
        _sinks.TryGetValue(sinkName, out var sink);
        return sink;
    }

    /// <inheritdoc />
    public void StartConsoleInterceptor()
    {
        lock (_interceptorLock)
        {
            if (_consoleInterceptor?.IsActive == true)
                return;

            _consoleInterceptor = new ConsoleInterceptor(this);
            _consoleInterceptor.Start();
        }
    }

    /// <inheritdoc />
    public void StopConsoleInterceptor()
    {
        lock (_interceptorLock)
        {
            _consoleInterceptor?.Stop();
            _consoleInterceptor = null;
        }
    }

    /// <inheritdoc />
    public async ValueTask FlushAsync(CancellationToken cancellationToken = default)
    {
        foreach (var sink in _sinks.Values)
        {
            try
            {
                await sink.FlushAsync(cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                // Swallow flush errors
            }
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        StopConsoleInterceptor();

        foreach (var sink in _sinks.Values)
        {
            try
            {
                sink.Dispose();
            }
            catch
            {
                // Swallow dispose errors
            }
        }

        _sinks.Clear();
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        StopConsoleInterceptor();

        foreach (var sink in _sinks.Values)
        {
            try
            {
                await sink.DisposeAsync().ConfigureAwait(false);
            }
            catch
            {
                // Swallow dispose errors
            }
        }

        _sinks.Clear();
    }

    /// <summary>
    /// Internal method to log console capture entries.
    /// </summary>
    internal void LogConsoleCapture(string text, bool isError)
    {
        if (_disposed || string.IsNullOrEmpty(text))
            return;

        var entry = new LogEntry
        {
            Level = isError ? LogLevel.Warning : LogLevel.Console,
            Message = text.TrimEnd('\r', '\n'),
            Source = "Console",
            CorrelationId = _correlationId.Value,
            IsConsoleCapture = true
        };

        LogCore(entry);
    }

    private sealed class LoggerScope : ILoggerScope
    {
        private readonly LoggerService _logger;

        public string Source { get; }

        public LoggerScope(LoggerService logger, string source)
        {
            _logger = logger;
            Source = source;
        }

        public void Trace(string message) => _logger.Trace(message, Source);
        public void Debug(string message) => _logger.Debug(message, Source);
        public void Information(string message) => _logger.Information(message, Source);
        public void Warning(string message, Exception? exception = null) => _logger.Warning(message, Source, exception);
        public void Error(string message, Exception? exception = null) => _logger.Error(message, Source, exception);
        public void Critical(string message, Exception? exception = null) => _logger.Critical(message, Source, exception);

        public void Log(LogLevel level, string message, Exception? exception = null, IReadOnlyDictionary<string, object?>? properties = null)
        {
            _logger.Log(new LogEntry
            {
                Level = level,
                Message = message,
                Source = Source,
                Exception = exception,
                Properties = properties,
                CorrelationId = _logger.CorrelationId
            });
        }
    }

    private sealed class CorrelationScope : IDisposable
    {
        private readonly LoggerService _logger;
        private readonly string? _previousId;

        public CorrelationScope(LoggerService logger, string? previousId)
        {
            _logger = logger;
            _previousId = previousId;
        }

        public void Dispose()
        {
            _logger._correlationId.Value = _previousId;
        }
    }
}

