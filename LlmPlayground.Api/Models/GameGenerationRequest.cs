using System.ComponentModel.DataAnnotations;

namespace LlmPlayground.Api.Models;

/// <summary>
/// Request model for generating a Prolog-based logic game.
/// </summary>
public sealed record GameGenerationRequest
{
    /// <summary>
    /// Optional theme for the game (e.g., "mystery", "adventure", "puzzle").
    /// If not provided, the LLM will choose a theme.
    /// </summary>
    [MaxLength(200)]
    public string? Theme { get; init; }

    /// <summary>
    /// Optional description or additional requirements for the game.
    /// </summary>
    [MaxLength(1000)]
    public string? Description { get; init; }

    /// <summary>
    /// LLM provider to use. If not specified, uses the default from configuration.
    /// Valid values: "Ollama", "LmStudio", "OpenAI"
    /// </summary>
    public string? Provider { get; init; }

    /// <summary>
    /// Whether to execute the generated Prolog code and include the output.
    /// </summary>
    public bool ExecuteGame { get; init; } = true;

    /// <summary>
    /// Custom Prolog goal to execute. If not specified, uses "main" from configuration.
    /// </summary>
    [MaxLength(200)]
    public string? PrologGoal { get; init; }
}

