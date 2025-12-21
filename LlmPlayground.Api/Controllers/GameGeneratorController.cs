using LlmPlayground.Api.Models;
using LlmPlayground.Api.Services;
using LlmPlayground.Utilities.Sanitization;
using LlmPlayground.Utilities.Validation;
using Microsoft.AspNetCore.Mvc;

namespace LlmPlayground.Api.Controllers;

/// <summary>
/// Controller for generating Prolog-based logic games using LLM providers.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class GameGeneratorController : ControllerBase
{
    private readonly IGameGeneratorService _gameGeneratorService;
    private readonly IRequestValidator _requestValidator;
    private readonly ILogger<GameGeneratorController> _logger;

    public GameGeneratorController(
        IGameGeneratorService gameGeneratorService,
        IRequestValidator requestValidator,
        ILogger<GameGeneratorController> logger)
    {
        _gameGeneratorService = gameGeneratorService ?? throw new ArgumentNullException(nameof(gameGeneratorService));
        _requestValidator = requestValidator ?? throw new ArgumentNullException(nameof(requestValidator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Generates a Prolog-based logic game using LLM.
    /// </summary>
    /// <param name="request">The game generation request with optional theme and description.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The generated game including idea, Prolog code, and execution results.</returns>
    /// <response code="200">Returns the generated game</response>
    /// <response code="400">If the request validation fails</response>
    /// <response code="500">If an error occurs during generation</response>
    [HttpPost("generate")]
    [ProducesResponseType(typeof(GameGenerationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<GameGenerationResponse>> GenerateGame(
        [FromBody] GameGenerationRequest request,
        CancellationToken cancellationToken)
    {
        // Validate request
        var validationErrors = ValidateRequest(request);
        if (validationErrors.Count > 0)
        {
            _logger.LogWarning("Game generation request validation failed: {Errors}",
                string.Join("; ", validationErrors));

            return BadRequest(new ErrorResponse
            {
                Error = "Validation failed",
                Details = validationErrors
            });
        }

        // Sanitize inputs
        var sanitizedRequest = SanitizeRequest(request);

        _logger.LogInformation(
            "Starting game generation - Theme: {Theme}, Provider: {Provider}",
            sanitizedRequest.Theme ?? "(none)",
            sanitizedRequest.Provider ?? "(default)");

        try
        {
            var result = await _gameGeneratorService.GenerateGameAsync(sanitizedRequest, cancellationToken);

            if (!result.Success)
            {
                _logger.LogWarning("Game generation failed: {Error}", result.Error);
            }
            else
            {
                _logger.LogInformation(
                    "Game generation completed successfully in {Duration}ms using {Provider}",
                    result.Duration.TotalMilliseconds,
                    result.ProviderUsed);
            }

            return Ok(result);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Game generation was cancelled");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during game generation");

            return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse
            {
                Error = "An unexpected error occurred during game generation",
                Details = [ex.Message]
            });
        }
    }

    /// <summary>
    /// Gets the health status of the game generator service.
    /// </summary>
    /// <returns>Health status information.</returns>
    [HttpGet("health")]
    [ProducesResponseType(typeof(HealthResponse), StatusCodes.Status200OK)]
    public ActionResult<HealthResponse> GetHealth()
    {
        return Ok(new HealthResponse
        {
            Status = "healthy",
            Timestamp = DateTime.UtcNow
        });
    }

    private List<string> ValidateRequest(GameGenerationRequest request)
    {
        var errors = new List<string>();

        if (!string.IsNullOrEmpty(request.Theme))
        {
            var themeValidation = _requestValidator.ValidatePrompt(request.Theme, 200);
            if (!themeValidation.IsValid)
            {
                errors.AddRange(themeValidation.Errors.Select(e => $"Theme: {e.Message}"));
            }
        }

        if (!string.IsNullOrEmpty(request.Description))
        {
            var descValidation = _requestValidator.ValidatePrompt(request.Description, 1000);
            if (!descValidation.IsValid)
            {
                errors.AddRange(descValidation.Errors.Select(e => $"Description: {e.Message}"));
            }
        }

        if (!string.IsNullOrEmpty(request.Provider))
        {
            var validProviders = new[] { "Ollama", "LmStudio", "OpenAI" };
            if (!validProviders.Contains(request.Provider, StringComparer.OrdinalIgnoreCase))
            {
                errors.Add($"Provider: Invalid provider '{request.Provider}'. Valid values: {string.Join(", ", validProviders)}");
            }
        }

        if (!string.IsNullOrEmpty(request.PrologGoal))
        {
            var goalValidation = _requestValidator.ValidatePrologQuery(request.PrologGoal);
            if (!goalValidation.IsValid)
            {
                errors.AddRange(goalValidation.Errors.Select(e => $"PrologGoal: {e.Message}"));
            }
        }

        return errors;
    }

    private static GameGenerationRequest SanitizeRequest(GameGenerationRequest request)
    {
        return request with
        {
            Theme = InputSanitizer.Sanitize(request.Theme, SanitizationOptions.Default),
            Description = InputSanitizer.Sanitize(request.Description, SanitizationOptions.Default),
            PrologGoal = InputSanitizer.Sanitize(request.PrologGoal, SanitizationOptions.Strict)
        };
    }
}

/// <summary>
/// Standard error response model.
/// </summary>
public sealed record ErrorResponse
{
    /// <summary>
    /// The error message.
    /// </summary>
    public required string Error { get; init; }

    /// <summary>
    /// Additional error details.
    /// </summary>
    public IReadOnlyList<string> Details { get; init; } = [];
}

/// <summary>
/// Health check response model.
/// </summary>
public sealed record HealthResponse
{
    /// <summary>
    /// The health status.
    /// </summary>
    public required string Status { get; init; }

    /// <summary>
    /// When the health check was performed.
    /// </summary>
    public DateTime Timestamp { get; init; }
}

