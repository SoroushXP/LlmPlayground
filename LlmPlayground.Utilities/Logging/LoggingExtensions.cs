using System.Runtime.CompilerServices;

namespace LlmPlayground.Utilities.Logging;

/// <summary>
/// Extension methods for convenient logging operations.
/// </summary>
public static class LoggingExtensions
{
    /// <summary>
    /// Logs a message at Information level with automatic source detection.
    /// </summary>
    public static void LogInfo(
        this ILoggerService logger,
        string message,
        [CallerMemberName] string? memberName = null,
        [CallerFilePath] string? filePath = null)
    {
        var source = GetSource(memberName, filePath);
        logger.Information(message, source);
    }

    /// <summary>
    /// Logs a message at Debug level with automatic source detection.
    /// </summary>
    public static void LogDebug(
        this ILoggerService logger,
        string message,
        [CallerMemberName] string? memberName = null,
        [CallerFilePath] string? filePath = null)
    {
        var source = GetSource(memberName, filePath);
        logger.Debug(message, source);
    }

    /// <summary>
    /// Logs a message at Warning level with automatic source detection.
    /// </summary>
    public static void LogWarning(
        this ILoggerService logger,
        string message,
        Exception? exception = null,
        [CallerMemberName] string? memberName = null,
        [CallerFilePath] string? filePath = null)
    {
        var source = GetSource(memberName, filePath);
        logger.Warning(message, source, exception);
    }

    /// <summary>
    /// Logs a message at Error level with automatic source detection.
    /// </summary>
    public static void LogError(
        this ILoggerService logger,
        string message,
        Exception? exception = null,
        [CallerMemberName] string? memberName = null,
        [CallerFilePath] string? filePath = null)
    {
        var source = GetSource(memberName, filePath);
        logger.Error(message, source, exception);
    }

    /// <summary>
    /// Logs a structured message with properties.
    /// </summary>
    public static void LogStructured(
        this ILoggerService logger,
        LogLevel level,
        string message,
        IDictionary<string, object?> properties,
        [CallerMemberName] string? memberName = null,
        [CallerFilePath] string? filePath = null)
    {
        var source = GetSource(memberName, filePath);
        var entry = new LogEntryBuilder()
            .WithLevel(level)
            .WithMessage(message)
            .WithSource(source)
            .WithProperties(properties)
            .Build();

        logger.Log(entry);
    }

    /// <summary>
    /// Measures and logs the execution time of an action.
    /// </summary>
    public static void LogTimed(
        this ILoggerService logger,
        string operationName,
        Action action,
        LogLevel level = LogLevel.Debug,
        [CallerMemberName] string? memberName = null,
        [CallerFilePath] string? filePath = null)
    {
        var source = GetSource(memberName, filePath);
        var sw = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            action();
        }
        finally
        {
            sw.Stop();
            logger.Log(level, $"{operationName} completed in {sw.ElapsedMilliseconds}ms", source);
        }
    }

    /// <summary>
    /// Measures and logs the execution time of an async operation.
    /// </summary>
    public static async Task LogTimedAsync(
        this ILoggerService logger,
        string operationName,
        Func<Task> action,
        LogLevel level = LogLevel.Debug,
        [CallerMemberName] string? memberName = null,
        [CallerFilePath] string? filePath = null)
    {
        var source = GetSource(memberName, filePath);
        var sw = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            await action().ConfigureAwait(false);
        }
        finally
        {
            sw.Stop();
            logger.Log(level, $"{operationName} completed in {sw.ElapsedMilliseconds}ms", source);
        }
    }

    /// <summary>
    /// Measures and logs the execution time of an async operation with result.
    /// </summary>
    public static async Task<T> LogTimedAsync<T>(
        this ILoggerService logger,
        string operationName,
        Func<Task<T>> action,
        LogLevel level = LogLevel.Debug,
        [CallerMemberName] string? memberName = null,
        [CallerFilePath] string? filePath = null)
    {
        var source = GetSource(memberName, filePath);
        var sw = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            return await action().ConfigureAwait(false);
        }
        finally
        {
            sw.Stop();
            logger.Log(level, $"{operationName} completed in {sw.ElapsedMilliseconds}ms", source);
        }
    }

    /// <summary>
    /// Creates a scoped logger using the caller's context.
    /// </summary>
    public static ILoggerScope CreateAutoScope(
        this ILoggerService logger,
        [CallerFilePath] string? filePath = null)
    {
        var fileName = Path.GetFileNameWithoutExtension(filePath ?? "Unknown");
        return logger.CreateScope(fileName);
    }

    private static string GetSource(string? memberName, string? filePath)
    {
        var fileName = Path.GetFileNameWithoutExtension(filePath ?? "Unknown");
        return string.IsNullOrEmpty(memberName)
            ? fileName
            : $"{fileName}.{memberName}";
    }
}

/// <summary>
/// Disposable scope that logs entry and exit.
/// </summary>
public sealed class LoggingScope : IDisposable
{
    private readonly ILoggerService _logger;
    private readonly string _operationName;
    private readonly string? _source;
    private readonly System.Diagnostics.Stopwatch _stopwatch;

    /// <summary>
    /// Creates a logging scope that logs entry and exit with timing.
    /// </summary>
    public LoggingScope(ILoggerService logger, string operationName, string? source = null)
    {
        _logger = logger;
        _operationName = operationName;
        _source = source;
        _stopwatch = System.Diagnostics.Stopwatch.StartNew();

        _logger.Debug($"Starting: {operationName}", _source);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _stopwatch.Stop();
        _logger.Debug($"Completed: {_operationName} ({_stopwatch.ElapsedMilliseconds}ms)", _source);
    }
}

