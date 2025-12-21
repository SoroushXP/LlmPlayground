using System.Collections.Concurrent;
using System.Text;

namespace LlmPlayground.Utilities.Logging.Sinks;

/// <summary>
/// Log sink that writes to files with rolling support.
/// </summary>
public sealed class FileSink : LogSinkBase
{
    private readonly FileSinkOptions _options;
    private readonly ConcurrentQueue<LogEntry> _buffer;
    private readonly SemaphoreSlim _writeLock = new(1, 1);
    private readonly Timer _flushTimer;
    
    private StreamWriter? _currentWriter;
    private string _currentFilePath = string.Empty;
    private DateOnly _currentFileDate;
    private long _currentFileSize;
    private bool _disposed;

    /// <inheritdoc />
    public override string Name => $"File:{_options.Directory}";

    /// <summary>
    /// Gets the current log file path.
    /// </summary>
    public string CurrentFilePath => _currentFilePath;

    /// <summary>
    /// Initializes a new file sink with the specified options.
    /// </summary>
    public FileSink(FileSinkOptions options, ILogFormatter? formatter = null)
        : base(formatter ?? new DefaultLogFormatter(new LogFormatterOptions { UseShortLevelNames = true }))
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _buffer = new ConcurrentQueue<LogEntry>();

        // Ensure directory exists
        Directory.CreateDirectory(_options.Directory);

        // Setup periodic flush
        _flushTimer = new Timer(
            _ => _ = FlushBufferAsync(),
            null,
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(_options.FlushIntervalSeconds));
    }

    /// <summary>
    /// Initializes a new file sink with a directory path.
    /// </summary>
    public FileSink(string directory, ILogFormatter? formatter = null)
        : this(new FileSinkOptions { Directory = directory }, formatter)
    {
    }

    /// <inheritdoc />
    protected override void WriteCore(LogEntry entry)
    {
        if (_options.BufferSize > 0)
        {
            _buffer.Enqueue(entry);
            
            // Flush if buffer is full
            if (_buffer.Count >= _options.BufferSize)
            {
                _ = FlushBufferAsync();
            }
        }
        else
        {
            // Synchronous write
            WriteEntryToFile(entry);
        }
    }

    /// <inheritdoc />
    protected override async ValueTask WriteCoreAsync(LogEntry entry, CancellationToken cancellationToken)
    {
        _buffer.Enqueue(entry);

        if (_buffer.Count >= _options.BufferSize)
        {
            await FlushBufferAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public override async ValueTask FlushAsync(CancellationToken cancellationToken = default)
    {
        await FlushBufferAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task FlushBufferAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed || _buffer.IsEmpty)
            return;

        await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            while (_buffer.TryDequeue(out var entry))
            {
                await WriteEntryToFileAsync(entry, cancellationToken).ConfigureAwait(false);
            }

            if (_currentWriter is not null)
            {
                await _currentWriter.FlushAsync(cancellationToken).ConfigureAwait(false);
            }
        }
        finally
        {
            _writeLock.Release();
        }
    }

    private void WriteEntryToFile(LogEntry entry)
    {
        _writeLock.Wait();
        try
        {
            EnsureFileReady(entry.Timestamp);
            var line = Formatter.Format(entry);
            _currentWriter!.WriteLine(line);
            _currentFileSize += Encoding.UTF8.GetByteCount(line) + Environment.NewLine.Length;
        }
        finally
        {
            _writeLock.Release();
        }
    }

    private async ValueTask WriteEntryToFileAsync(LogEntry entry, CancellationToken cancellationToken)
    {
        EnsureFileReady(entry.Timestamp);
        var line = Formatter.Format(entry);
        await _currentWriter!.WriteLineAsync(line.AsMemory(), cancellationToken).ConfigureAwait(false);
        _currentFileSize += Encoding.UTF8.GetByteCount(line) + Environment.NewLine.Length;
    }

    private void EnsureFileReady(DateTimeOffset timestamp)
    {
        var today = DateOnly.FromDateTime(timestamp.LocalDateTime);
        var needNewFile = _currentWriter is null
                          || (_options.RollingPolicy == RollingPolicy.Daily && today != _currentFileDate)
                          || (_options.RollingPolicy == RollingPolicy.Size && _currentFileSize >= _options.MaxFileSizeBytes);

        if (needNewFile)
        {
            OpenNewFile(today);
        }
    }

    private void OpenNewFile(DateOnly date)
    {
        _currentWriter?.Dispose();

        _currentFileDate = date;
        _currentFileSize = 0;

        var fileName = GenerateFileName(date);
        _currentFilePath = Path.Combine(_options.Directory, fileName);

        // If file exists and we're rolling by size, find next available
        if (_options.RollingPolicy == RollingPolicy.Size && File.Exists(_currentFilePath))
        {
            var sequence = 1;
            while (File.Exists(_currentFilePath))
            {
                fileName = GenerateFileName(date, sequence++);
                _currentFilePath = Path.Combine(_options.Directory, fileName);
            }
        }

        _currentWriter = new StreamWriter(
            new FileStream(_currentFilePath, FileMode.Append, FileAccess.Write, FileShare.Read, 4096, useAsync: true),
            Encoding.UTF8)
        {
            AutoFlush = false
        };

        // Clean up old files if retention is configured
        if (_options.RetainDays > 0)
        {
            CleanupOldFiles();
        }
    }

    private string GenerateFileName(DateOnly date, int? sequence = null)
    {
        var dateStr = date.ToString("yyyy-MM-dd");
        var seqStr = sequence.HasValue ? $"_{sequence.Value:D3}" : "";
        return $"{_options.FileNamePrefix}{dateStr}{seqStr}.log";
    }

    private void CleanupOldFiles()
    {
        try
        {
            var cutoffDate = DateOnly.FromDateTime(DateTime.Now.AddDays(-_options.RetainDays));
            var files = Directory.GetFiles(_options.Directory, $"{_options.FileNamePrefix}*.log");

            foreach (var file in files)
            {
                var fileName = Path.GetFileNameWithoutExtension(file);
                var datePart = fileName.Replace(_options.FileNamePrefix, "").Split('_')[0];

                if (DateOnly.TryParse(datePart, out var fileDate) && fileDate < cutoffDate)
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch
                    {
                        // Ignore deletion errors
                    }
                }
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (_disposed) return;
        _disposed = true;

        if (disposing)
        {
            _flushTimer.Dispose();
            
            // Final flush
            _writeLock.Wait();
            try
            {
                while (_buffer.TryDequeue(out var entry))
                {
                    var line = Formatter.Format(entry);
                    _currentWriter?.WriteLine(line);
                }
                _currentWriter?.Dispose();
            }
            finally
            {
                _writeLock.Release();
            }

            _writeLock.Dispose();
        }

        base.Dispose(disposing);
    }

    /// <inheritdoc />
    protected override async ValueTask DisposeAsyncCore()
    {
        if (_disposed) return;
        _disposed = true;

        await _flushTimer.DisposeAsync().ConfigureAwait(false);
        await FlushBufferAsync().ConfigureAwait(false);

        if (_currentWriter is not null)
        {
            await _currentWriter.DisposeAsync().ConfigureAwait(false);
        }

        _writeLock.Dispose();
    }
}

/// <summary>
/// Configuration options for the file sink.
/// </summary>
public sealed record FileSinkOptions
{
    /// <summary>
    /// The directory where log files are written.
    /// </summary>
    public required string Directory { get; init; }

    /// <summary>
    /// Prefix for log file names.
    /// </summary>
    public string FileNamePrefix { get; init; } = "log_";

    /// <summary>
    /// The rolling policy for creating new files.
    /// </summary>
    public RollingPolicy RollingPolicy { get; init; } = RollingPolicy.Daily;

    /// <summary>
    /// Maximum file size in bytes before rolling (when using Size policy).
    /// </summary>
    public long MaxFileSizeBytes { get; init; } = 10 * 1024 * 1024; // 10 MB

    /// <summary>
    /// Number of days to retain log files. Older files are automatically deleted.
    /// Set to 0 to disable cleanup. Default: 7 days.
    /// </summary>
    public int RetainDays { get; init; } = 7;

    /// <summary>
    /// Number of entries to buffer before writing.
    /// </summary>
    public int BufferSize { get; init; } = 100;

    /// <summary>
    /// Interval in seconds between automatic flushes.
    /// </summary>
    public int FlushIntervalSeconds { get; init; } = 5;
}

/// <summary>
/// Defines when to create new log files.
/// </summary>
public enum RollingPolicy
{
    /// <summary>
    /// Create a new file each day.
    /// </summary>
    Daily,

    /// <summary>
    /// Create a new file when size limit is reached.
    /// </summary>
    Size,

    /// <summary>
    /// Never roll - append to a single file.
    /// </summary>
    Never
}

