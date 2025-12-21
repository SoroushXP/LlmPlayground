using LlmPlayground.Prolog;
using LlmPlayground.Services.Interfaces;
using LlmPlayground.Services.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LlmPlayground.Services.Services;

/// <summary>
/// Service implementation for executing Prolog queries and files.
/// </summary>
public class PrologService : IPrologService
{
    private readonly PrologRunner _runner;
    private readonly ILogger<PrologService> _logger;
    private readonly string? _workingDirectory;

    public PrologService(IConfiguration configuration, ILogger<PrologService> logger)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        var section = configuration.GetSection("Prolog");
        var prologPath = section["ExecutablePath"];
        _workingDirectory = section["WorkingDirectory"];

        // Pass null if the path is empty so PrologRunner will auto-detect
        _runner = new PrologRunner(string.IsNullOrWhiteSpace(prologPath) ? null : prologPath);
    }

    /// <summary>
    /// Creates a PrologService with a custom PrologRunner (for testing).
    /// </summary>
    internal PrologService(PrologRunner runner, ILogger<PrologService> logger, string? workingDirectory = null)
    {
        _runner = runner ?? throw new ArgumentNullException(nameof(runner));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _workingDirectory = workingDirectory;
    }

    /// <inheritdoc />
    public async Task<PrologAvailabilityResponse> CheckAvailabilityAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var isAvailable = await _runner.IsPrologAvailableAsync();
            
            return new PrologAvailabilityResponse
            {
                IsAvailable = isAvailable,
                Info = isAvailable ? "SWI-Prolog is available" : "SWI-Prolog not found on system"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking Prolog availability");
            return new PrologAvailabilityResponse
            {
                IsAvailable = false,
                Info = $"Error checking availability: {ex.Message}"
            };
        }
    }

    /// <inheritdoc />
    public async Task<PrologResponse> ExecuteQueryAsync(PrologQueryRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Query);

        _logger.LogDebug("Executing Prolog query: {Query}", request.Query);

        try
        {
            var result = await _runner.RunQueryAsync(request.Query, cancellationToken);
            return MapResult(result);
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("Prolog query was cancelled");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing Prolog query");
            return new PrologResponse
            {
                Success = false,
                Error = ex.Message,
                ExitCode = -1
            };
        }
    }

    /// <inheritdoc />
    public async Task<PrologResponse> ExecuteFileAsync(PrologFileRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.FilePath);

        var filePath = ResolveFilePath(request.FilePath);
        _logger.LogDebug("Executing Prolog file: {FilePath}, Goal: {Goal}", filePath, request.Goal);

        try
        {
            var result = await _runner.RunFileAsync(filePath, request.Goal, cancellationToken);
            return MapResult(result);
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("Prolog file execution was cancelled");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing Prolog file: {FilePath}", filePath);
            return new PrologResponse
            {
                Success = false,
                Error = ex.Message,
                ExitCode = -1
            };
        }
    }

    /// <inheritdoc />
    public async Task<PrologResponse> ValidateSyntaxAsync(string code, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        _logger.LogDebug("Validating Prolog syntax");

        // Create a temporary file to validate
        var tempFile = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(tempFile, code, cancellationToken);
            
            // Change extension to .pl
            var plFile = Path.ChangeExtension(tempFile, ".pl");
            File.Move(tempFile, plFile);

            try
            {
                // Just load the file without executing any goal to check syntax
                var result = await _runner.RunFileAsync(plFile, cancellationToken: cancellationToken);
                return MapResult(result);
            }
            finally
            {
                File.Delete(plFile);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Error validating Prolog syntax");
            return new PrologResponse
            {
                Success = false,
                Error = ex.Message,
                ExitCode = -1
            };
        }
    }

    private string ResolveFilePath(string filePath)
    {
        if (Path.IsPathRooted(filePath))
        {
            return filePath;
        }

        if (!string.IsNullOrEmpty(_workingDirectory))
        {
            return Path.Combine(_workingDirectory, filePath);
        }

        return Path.GetFullPath(filePath);
    }

    private static PrologResponse MapResult(PrologResult result)
    {
        return new PrologResponse
        {
            Success = result.Success,
            Output = result.Output,
            Error = string.IsNullOrEmpty(result.Error) ? null : result.Error,
            ExitCode = result.ExitCode
        };
    }
}

