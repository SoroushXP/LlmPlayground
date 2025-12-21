namespace LlmPlayground.Services.Models;

/// <summary>
/// Request model for executing a Prolog query.
/// </summary>
public record PrologQueryRequest
{
    /// <summary>
    /// The Prolog query to execute.
    /// </summary>
    public required string Query { get; init; }
}

/// <summary>
/// Request model for executing a Prolog file.
/// </summary>
public record PrologFileRequest
{
    /// <summary>
    /// Path to the Prolog file to execute.
    /// </summary>
    public required string FilePath { get; init; }

    /// <summary>
    /// Optional goal to execute after loading the file.
    /// </summary>
    public string? Goal { get; init; }
}

/// <summary>
/// Response model for Prolog execution.
/// </summary>
public record PrologResponse
{
    /// <summary>
    /// Whether the execution was successful.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// The standard output from the Prolog interpreter.
    /// </summary>
    public string Output { get; init; } = string.Empty;

    /// <summary>
    /// Any error output from the Prolog interpreter.
    /// </summary>
    public string? Error { get; init; }

    /// <summary>
    /// The exit code from the Prolog process.
    /// </summary>
    public int ExitCode { get; init; }
}

/// <summary>
/// Response for Prolog availability check.
/// </summary>
public record PrologAvailabilityResponse
{
    /// <summary>
    /// Whether Prolog is available on the system.
    /// </summary>
    public bool IsAvailable { get; init; }

    /// <summary>
    /// Additional information about the Prolog installation.
    /// </summary>
    public string? Info { get; init; }
}

