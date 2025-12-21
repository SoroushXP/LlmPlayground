using LlmPlayground.Services.Models;

namespace LlmPlayground.Services.Interfaces;

/// <summary>
/// Service for executing Prolog queries and files.
/// </summary>
public interface IPrologService
{
    /// <summary>
    /// Checks if Prolog is available on the system.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Availability response with status and information.</returns>
    Task<PrologAvailabilityResponse> CheckAvailabilityAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a Prolog query directly.
    /// </summary>
    /// <param name="request">The query request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The execution result.</returns>
    Task<PrologResponse> ExecuteQueryAsync(PrologQueryRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a Prolog file.
    /// </summary>
    /// <param name="request">The file execution request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The execution result.</returns>
    Task<PrologResponse> ExecuteFileAsync(PrologFileRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates Prolog syntax without executing.
    /// </summary>
    /// <param name="code">The Prolog code to validate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Validation result with any syntax errors.</returns>
    Task<PrologResponse> ValidateSyntaxAsync(string code, CancellationToken cancellationToken = default);
}

