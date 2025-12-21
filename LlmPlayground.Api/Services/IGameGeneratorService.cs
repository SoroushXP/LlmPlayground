using LlmPlayground.Api.Models;

namespace LlmPlayground.Api.Services;

/// <summary>
/// Service for generating Prolog-based logic games using LLM providers.
/// </summary>
public interface IGameGeneratorService
{
    /// <summary>
    /// Generates a complete Prolog-based logic game.
    /// </summary>
    /// <param name="request">The game generation request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The game generation response with idea, code, and optional execution results.</returns>
    Task<GameGenerationResponse> GenerateGameAsync(
        GameGenerationRequest request,
        CancellationToken cancellationToken = default);
}

