using System.Diagnostics;
using System.Text;

namespace LlmPlayground.Prolog;

/// <summary>
/// Runs Prolog files using an external Prolog interpreter (SWI-Prolog by default).
/// </summary>
public class PrologRunner
{
    private readonly string _prologPath;

    /// <summary>
    /// Common installation paths for SWI-Prolog on different operating systems.
    /// </summary>
    private static readonly string[] CommonPrologPaths = 
    {
        "swipl", // PATH lookup
        @"C:\Program Files\swipl\bin\swipl.exe",
        @"C:\Program Files (x86)\swipl\bin\swipl.exe",
        "/usr/bin/swipl",
        "/usr/local/bin/swipl",
        "/opt/homebrew/bin/swipl",
        "/snap/bin/swipl"
    };

    /// <summary>
    /// Creates a new PrologRunner instance.
    /// </summary>
    /// <param name="prologPath">Path to the Prolog executable. If null, will auto-detect SWI-Prolog installation.</param>
    public PrologRunner(string? prologPath = null)
    {
        _prologPath = prologPath ?? FindPrologExecutable() ?? "swipl";
    }

    /// <summary>
    /// Attempts to find a valid Prolog executable from common installation paths.
    /// </summary>
    private static string? FindPrologExecutable()
    {
        foreach (var path in CommonPrologPaths)
        {
            if (path == "swipl")
            {
                // Check if swipl is in PATH by trying to run it
                if (TryExecuteProlog(path))
                    return path;
            }
            else if (File.Exists(path))
            {
                return path;
            }
        }
        return null;
    }

    /// <summary>
    /// Tests if a Prolog executable can be run successfully.
    /// </summary>
    private static bool TryExecuteProlog(string path)
    {
        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = path,
                    Arguments = "--version",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };
            process.Start();
            
            // Wait for the process to exit with a timeout
            if (!process.WaitForExit(5000))
            {
                // Process didn't exit in time, try to kill it
                try { process.Kill(); } catch { /* ignore */ }
                return false;
            }
            
            // Check if the output contains "SWI-Prolog" to verify it's the right executable
            var output = process.StandardOutput.ReadToEnd();
            return output.Contains("SWI-Prolog", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Executes a Prolog file and returns the output.
    /// </summary>
    /// <param name="filePath">Path to the .pl Prolog file to execute.</param>
    /// <param name="goal">Optional goal to execute after loading the file.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The output from the Prolog interpreter.</returns>
    public async Task<PrologResult> RunFileAsync(
        string filePath,
        string? goal = null,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(filePath))
        {
            return new PrologResult
            {
                Success = false,
                Error = $"File not found: {filePath}"
            };
        }

        var arguments = BuildArguments(filePath, goal);

        return await ExecutePrologAsync(arguments, cancellationToken);
    }

    /// <summary>
    /// Executes a Prolog query directly without loading a file.
    /// </summary>
    /// <param name="query">The Prolog query to execute.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The output from the Prolog interpreter.</returns>
    public async Task<PrologResult> RunQueryAsync(
        string query,
        CancellationToken cancellationToken = default)
    {
        // Remove trailing period if present (we'll add halt(0) after)
        var normalizedQuery = query.TrimEnd();
        if (normalizedQuery.EndsWith('.'))
        {
            normalizedQuery = normalizedQuery[..^1].TrimEnd();
        }

        // Use quiet mode and halt(0) for clean exit
        // The goal becomes: query, halt(0).
        var arguments = $"-q -g \"{normalizedQuery}, halt(0)\"";

        return await ExecutePrologAsync(arguments, cancellationToken);
    }

    /// <summary>
    /// Starts an interactive Prolog session with a loaded file.
    /// </summary>
    /// <param name="filePath">Optional path to a .pl file to load at startup.</param>
    /// <returns>The started Process for the interactive session.</returns>
    public Process StartInteractiveSession(string? filePath = null)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = _prologPath,
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
        {
            startInfo.Arguments = $"-s \"{filePath}\"";
        }

        var process = new Process { StartInfo = startInfo };
        process.Start();

        return process;
    }

    /// <summary>
    /// Checks if the Prolog interpreter is available on the system.
    /// </summary>
    /// <returns>True if Prolog is available, false otherwise.</returns>
    public async Task<bool> IsPrologAvailableAsync()
    {
        try
        {
            var result = await RunQueryAsync("true");
            // Check Success (which accounts for both exit code and error output)
            // Also verify no error message indicating the executable wasn't found
            return result.Success && string.IsNullOrEmpty(result.Error);
        }
        catch
        {
            return false;
        }
    }

    private string BuildArguments(string filePath, string? goal)
    {
        var args = new StringBuilder();

        // Use quiet mode to suppress banner
        args.Append("-q ");

        // Load the file
        args.Append($"-s \"{filePath}\"");

        // If a goal is specified, run it and halt
        if (!string.IsNullOrEmpty(goal))
        {
            // Remove trailing period if present (we'll chain with halt(0))
            var normalizedGoal = goal.TrimEnd();
            if (normalizedGoal.EndsWith('.'))
            {
                normalizedGoal = normalizedGoal[..^1].TrimEnd();
            }
            // Use halt(0) to ensure clean exit after goal
            args.Append($" -g \"{normalizedGoal}, halt(0)\"");
        }
        else
        {
            // Just load and halt (useful for checking syntax / running initialization)
            args.Append(" -g halt");
        }

        return args.ToString();
    }

    private async Task<PrologResult> ExecutePrologAsync(
        string arguments,
        CancellationToken cancellationToken)
    {
        var result = new PrologResult();

        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = _prologPath,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            process.Start();

            var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);

            await process.WaitForExitAsync(cancellationToken);

            result.Output = await outputTask;
            var stderr = await errorTask;
            result.ExitCode = process.ExitCode;

            // Separate warnings from actual errors
            var (warnings, errors) = SeparateWarningsFromErrors(stderr);
            result.Warnings = warnings;
            result.Error = errors;

            // Success if exit code is 0 and there are no actual errors (warnings are OK)
            result.Success = process.ExitCode == 0 && string.IsNullOrEmpty(errors);
        }
        catch (OperationCanceledException)
        {
            result.Success = false;
            result.Error = "Operation was cancelled.";
            throw;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Error = $"Failed to execute Prolog: {ex.Message}";
            result.ExitCode = -1; // Indicate execution failure
        }

        return result;
    }

    /// <summary>
    /// Separates Prolog warnings from actual errors in stderr output.
    /// </summary>
    /// <param name="stderr">The stderr content from SWI-Prolog.</param>
    /// <returns>A tuple with (warnings, errors) where warnings contains warning lines and errors contains actual error lines.</returns>
    private static (string warnings, string errors) SeparateWarningsFromErrors(string stderr)
    {
        if (string.IsNullOrWhiteSpace(stderr))
            return (string.Empty, string.Empty);

        var lines = stderr.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        var warningLines = new List<string>();
        var errorLines = new List<string>();

        bool inWarningBlock = false;

        foreach (var line in lines)
        {
            // SWI-Prolog warnings start with "Warning:" (possibly with a file path before)
            // They may span multiple lines with continuation (indented lines after "Warning:")
            if (line.Contains("Warning:", StringComparison.OrdinalIgnoreCase))
            {
                inWarningBlock = true;
                warningLines.Add(line);
            }
            else if (inWarningBlock && line.StartsWith("    "))
            {
                // Continuation of a warning (indented lines)
                warningLines.Add(line);
            }
            else
            {
                // Not a warning - it's an actual error
                inWarningBlock = false;
                errorLines.Add(line);
            }
        }

        var warnings = warningLines.Count > 0 ? string.Join(Environment.NewLine, warningLines) : string.Empty;
        var errors = errorLines.Count > 0 ? string.Join(Environment.NewLine, errorLines) : string.Empty;

        return (warnings, errors);
    }
}

/// <summary>
/// Represents the result of a Prolog execution.
/// </summary>
public class PrologResult
{
    /// <summary>
    /// Whether the execution was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// The standard output from the Prolog interpreter.
    /// </summary>
    public string Output { get; set; } = string.Empty;

    /// <summary>
    /// Any error output from the Prolog interpreter (excluding warnings).
    /// </summary>
    public string Error { get; set; } = string.Empty;

    /// <summary>
    /// Warning messages from the Prolog interpreter (like singleton variable warnings).
    /// These do not cause execution to fail.
    /// </summary>
    public string Warnings { get; set; } = string.Empty;

    /// <summary>
    /// The exit code from the Prolog process.
    /// </summary>
    public int ExitCode { get; set; }
}


