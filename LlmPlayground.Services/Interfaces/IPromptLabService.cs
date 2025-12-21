using LlmPlayground.Services.Models;

namespace LlmPlayground.Services.Interfaces;

/// <summary>
/// Service for managing prompt engineering sessions.
/// </summary>
public interface IPromptLabService
{
    /// <summary>
    /// Creates a new prompt session.
    /// </summary>
    /// <param name="request">The session creation request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created session information.</returns>
    Task<SessionCreatedResponse> CreateSessionAsync(CreateSessionRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets information about an existing session.
    /// </summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <returns>Session information, or null if not found.</returns>
    SessionInfoResponse? GetSession(string sessionId);

    /// <summary>
    /// Gets all active session IDs.
    /// </summary>
    /// <returns>List of active session identifiers.</returns>
    IReadOnlyList<string> GetActiveSessions();

    /// <summary>
    /// Sends a prompt in an existing session.
    /// </summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <param name="request">The prompt request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The prompt response.</returns>
    Task<PromptResponse> SendPromptAsync(string sessionId, SendPromptRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Streams a prompt response in an existing session.
    /// </summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <param name="request">The prompt request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An async enumerable of response tokens.</returns>
    IAsyncEnumerable<string> SendPromptStreamingAsync(string sessionId, SendPromptRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retries the last prompt in a session.
    /// </summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The prompt response.</returns>
    Task<PromptResponse> RetryLastAsync(string sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears the conversation history for a session.
    /// </summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <returns>True if the session was found and cleared.</returns>
    bool ClearSessionHistory(string sessionId);

    /// <summary>
    /// Closes and removes a session.
    /// </summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <returns>True if the session was found and closed.</returns>
    bool CloseSession(string sessionId);

    /// <summary>
    /// Renders a prompt template with variables.
    /// </summary>
    /// <param name="request">The template render request.</param>
    /// <returns>The rendered template response.</returns>
    RenderTemplateResponse RenderTemplate(RenderTemplateRequest request);

    /// <summary>
    /// Gets the variables in a template.
    /// </summary>
    /// <param name="template">The template string.</param>
    /// <returns>List of variable names found in the template.</returns>
    IReadOnlyList<string> GetTemplateVariables(string template);
}

