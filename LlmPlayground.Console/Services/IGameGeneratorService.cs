using LlmPlayground.Console.Models;

namespace LlmPlayground.Console.Services;

/// <summary>
/// Service interface for generating Prolog-based logic games using LLM providers.
/// </summary>
public interface IGameGeneratorService
{
    /// <summary>
    /// Generates a Prolog-based logic game.
    /// </summary>
    /// <param name="request">The game generation request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The generation response with game idea, code, and execution results.</returns>
    Task<GameGenerationResponse> GenerateGameAsync(
        GameGenerationRequest request,
        CancellationToken cancellationToken = default);
}

