using LlmPlayground.Utilities.Logging.Sinks;

namespace LlmPlayground.Utilities.Logging;

/// <summary>
/// Configuration options for the logging system.
/// </summary>
public sealed record LoggingConfiguration
{
    /// <summary>
    /// Gets the default logging configuration.
    /// </summary>
    public static LoggingConfiguration Default { get; } = new();

    /// <summary>
    /// The global minimum log level.
    /// </summary>
    public LogLevel MinimumLevel { get; init; } = LogLevel.Information;

    /// <summary>
    /// Whether to enable the console sink.
    /// </summary>
    public bool EnableConsoleSink { get; init; } = true;

    /// <summary>
    /// Minimum level for console output.
    /// </summary>
    public LogLevel ConsoleMinimumLevel { get; init; } = LogLevel.Information;

    /// <summary>
    /// Whether to use colors in console output.
    /// </summary>
    public bool ConsoleUseColors { get; init; } = true;

    /// <summary>
    /// Whether to enable the file sink.
    /// </summary>
    public bool EnableFileSink { get; init; } = false;

    /// <summary>
    /// Directory for log files.
    /// </summary>
    public string FileDirectory { get; init; } = "logs";

    /// <summary>
    /// Minimum level for file output.
    /// </summary>
    public LogLevel FileMinimumLevel { get; init; } = LogLevel.Debug;

    /// <summary>
    /// File name prefix for log files.
    /// </summary>
    public string FileNamePrefix { get; init; } = "llmplayground_";

    /// <summary>
    /// Rolling policy for log files.
    /// </summary>
    public RollingPolicy FileRollingPolicy { get; init; } = RollingPolicy.Daily;

    /// <summary>
    /// Maximum file size in MB before rolling.
    /// </summary>
    public int MaxFileSizeMb { get; init; } = 10;

    /// <summary>
    /// Number of days to retain log files (default: 7 days, older files are auto-deleted).
    /// </summary>
    public int FileRetainDays { get; init; } = 7;

    /// <summary>
    /// Whether to intercept console output and log it.
    /// </summary>
    public bool InterceptConsoleOutput { get; init; } = false;

    /// <summary>
    /// Whether to include timestamps in log output.
    /// </summary>
    public bool IncludeTimestamps { get; init; } = true;

    /// <summary>
    /// Whether to include thread ID in log output.
    /// </summary>
    public bool IncludeThreadId { get; init; } = false;

    /// <summary>
    /// Whether to use JSON formatting for file logs.
    /// </summary>
    public bool UseJsonFormatting { get; init; } = false;
}

/// <summary>
/// Builder for creating configured logger services.
/// </summary>
public sealed class LoggerBuilder
{
    private LoggingConfiguration _config = LoggingConfiguration.Default;
    private readonly List<ILogSink> _additionalSinks = new();

    /// <summary>
    /// Configures the logger with the specified configuration.
    /// </summary>
    public LoggerBuilder WithConfiguration(LoggingConfiguration config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        return this;
    }

    /// <summary>
    /// Sets the minimum log level.
    /// </summary>
    public LoggerBuilder WithMinimumLevel(LogLevel level)
    {
        _config = _config with { MinimumLevel = level };
        return this;
    }

    /// <summary>
    /// Enables the console sink.
    /// </summary>
    public LoggerBuilder WithConsoleSink(LogLevel? minimumLevel = null, bool useColors = true)
    {
        _config = _config with
        {
            EnableConsoleSink = true,
            ConsoleMinimumLevel = minimumLevel ?? _config.ConsoleMinimumLevel,
            ConsoleUseColors = useColors
        };
        return this;
    }

    /// <summary>
    /// Enables the file sink.
    /// </summary>
    public LoggerBuilder WithFileSink(
        string? directory = null,
        LogLevel? minimumLevel = null,
        RollingPolicy? rollingPolicy = null,
        int? retainDays = null)
    {
        _config = _config with
        {
            EnableFileSink = true,
            FileDirectory = directory ?? _config.FileDirectory,
            FileMinimumLevel = minimumLevel ?? _config.FileMinimumLevel,
            FileRollingPolicy = rollingPolicy ?? _config.FileRollingPolicy,
            FileRetainDays = retainDays ?? _config.FileRetainDays
        };
        return this;
    }

    /// <summary>
    /// Enables console output interception.
    /// </summary>
    public LoggerBuilder WithConsoleInterception()
    {
        _config = _config with { InterceptConsoleOutput = true };
        return this;
    }

    /// <summary>
    /// Adds a custom sink.
    /// </summary>
    public LoggerBuilder WithSink(ILogSink sink)
    {
        _additionalSinks.Add(sink ?? throw new ArgumentNullException(nameof(sink)));
        return this;
    }

    /// <summary>
    /// Uses JSON formatting for file logs.
    /// </summary>
    public LoggerBuilder WithJsonFormatting()
    {
        _config = _config with { UseJsonFormatting = true };
        return this;
    }

    /// <summary>
    /// Builds the configured logger service.
    /// </summary>
    public LoggerService Build()
    {
        var logger = new LoggerService
        {
            MinimumLevel = _config.MinimumLevel
        };

        // Add console sink
        if (_config.EnableConsoleSink)
        {
            var formatter = new DefaultLogFormatter(new LogFormatterOptions
            {
                IncludeTimestamp = _config.IncludeTimestamps,
                IncludeThreadId = _config.IncludeThreadId
            });

            ILogSink consoleSink = _config.InterceptConsoleOutput
                ? new FilteredConsoleSink(formatter, _config.ConsoleUseColors)
                : new ConsoleSink(formatter, _config.ConsoleUseColors);

            consoleSink.MinimumLevel = _config.ConsoleMinimumLevel;
            logger.AddSink(consoleSink);
        }

        // Add file sink
        if (_config.EnableFileSink)
        {
            ILogFormatter fileFormatter = _config.UseJsonFormatting
                ? new JsonLogFormatter()
                : new DefaultLogFormatter(new LogFormatterOptions
                {
                    IncludeTimestamp = true,
                    IncludeThreadId = true,
                    IncludeFullException = true
                });

            var fileSink = new FileSink(
                new FileSinkOptions
                {
                    Directory = _config.FileDirectory,
                    FileNamePrefix = _config.FileNamePrefix,
                    RollingPolicy = _config.FileRollingPolicy,
                    MaxFileSizeBytes = _config.MaxFileSizeMb * 1024 * 1024,
                    RetainDays = _config.FileRetainDays
                },
                fileFormatter)
            {
                MinimumLevel = _config.FileMinimumLevel
            };

            logger.AddSink(fileSink);
        }

        // Add custom sinks
        foreach (var sink in _additionalSinks)
        {
            logger.AddSink(sink);
        }

        // Start console interception if enabled
        if (_config.InterceptConsoleOutput)
        {
            logger.StartConsoleInterceptor();
        }

        return logger;
    }
}

