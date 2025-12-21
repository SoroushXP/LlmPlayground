namespace LlmPlayground.Utilities.Logging;

/// <summary>
/// Defines the severity levels for log entries.
/// </summary>
public enum LogLevel
{
    /// <summary>
    /// Detailed tracing information, typically only enabled during development.
    /// </summary>
    Trace = 0,

    /// <summary>
    /// Debugging information useful during development and troubleshooting.
    /// </summary>
    Debug = 1,

    /// <summary>
    /// General informational messages about application flow.
    /// </summary>
    Information = 2,

    /// <summary>
    /// Console output captured from standard output streams.
    /// </summary>
    Console = 3,

    /// <summary>
    /// Potentially harmful situations that don't prevent operation.
    /// </summary>
    Warning = 4,

    /// <summary>
    /// Error events that might still allow the application to continue.
    /// </summary>
    Error = 5,

    /// <summary>
    /// Critical errors that cause application failure.
    /// </summary>
    Critical = 6,

    /// <summary>
    /// Disables logging when used as a filter level.
    /// </summary>
    None = 7
}

