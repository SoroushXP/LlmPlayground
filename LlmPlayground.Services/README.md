# LlmPlayground.Services

Service layer for LlmPlayground that provides a clean abstraction between API controllers and the core functionality.

## Overview

This project contains service interfaces and implementations that can be injected into ASP.NET Core API controllers or any other consumer that needs to interact with LLM providers, Prolog execution, or prompt engineering features.

## Project Structure

```
LlmPlayground.Services/
├── Extensions/
│   └── ServiceCollectionExtensions.cs   # DI registration extensions
├── Interfaces/
│   ├── ILlmService.cs                   # LLM provider service interface
│   ├── IPrologService.cs                # Prolog execution service interface
│   └── IPromptLabService.cs             # Prompt session service interface
├── Models/
│   ├── LlmModels.cs                     # DTOs for LLM requests/responses
│   ├── PrologModels.cs                  # DTOs for Prolog requests/responses
│   └── PromptLabModels.cs               # DTOs for session management
└── Services/
    ├── LlmService.cs                    # LLM provider service implementation
    ├── PrologService.cs                 # Prolog service implementation
    └── PromptLabService.cs              # Prompt session service implementation
```

## Services

### ILlmService

Direct interaction with LLM providers (Ollama, LM Studio, OpenAI).

```csharp
public interface ILlmService
{
    IReadOnlyList<string> GetAvailableProviders();
    string CurrentProvider { get; }
    bool IsReady { get; }
    string? CurrentModel { get; }
    
    Task SetProviderAsync(LlmProviderType providerType, CancellationToken ct = default);
    Task<IReadOnlyList<ModelInfoDto>> GetAvailableModelsAsync(CancellationToken ct = default);
    void SetModel(string modelId);
    
    Task<CompletionResponse> CompleteAsync(CompletionRequest request, CancellationToken ct = default);
    IAsyncEnumerable<string> CompleteStreamingAsync(CompletionRequest request, CancellationToken ct = default);
    Task<CompletionResponse> ChatAsync(ChatRequest request, CancellationToken ct = default);
    IAsyncEnumerable<string> ChatStreamingAsync(ChatRequest request, CancellationToken ct = default);
}
```

**Usage Example:**
```csharp
// Complete a prompt
var response = await llmService.CompleteAsync(new CompletionRequest
{
    Prompt = "Explain quantum computing",
    Options = new InferenceOptionsDto { MaxTokens = 500 }
});

// Stream chat completion
await foreach (var token in llmService.ChatStreamingAsync(new ChatRequest
{
    Messages = [new ChatMessageDto { Role = "user", Content = "Hello!" }]
}))
{
    Console.Write(token);
}
```

### IPrologService

Execute Prolog queries and files using SWI-Prolog.

```csharp
public interface IPrologService
{
    Task<PrologAvailabilityResponse> CheckAvailabilityAsync(CancellationToken ct = default);
    Task<PrologResponse> ExecuteQueryAsync(PrologQueryRequest request, CancellationToken ct = default);
    Task<PrologResponse> ExecuteFileAsync(PrologFileRequest request, CancellationToken ct = default);
    Task<PrologResponse> ValidateSyntaxAsync(string code, CancellationToken ct = default);
}
```

**Usage Example:**
```csharp
// Check availability
var available = await prologService.CheckAvailabilityAsync();
if (!available.IsAvailable) return;

// Run a query
var result = await prologService.ExecuteQueryAsync(new PrologQueryRequest
{
    Query = "member(X, [1,2,3])"
});

// Run a file with a goal
var result = await prologService.ExecuteFileAsync(new PrologFileRequest
{
    FilePath = "family.pl",
    Goal = "parent(X, mary)"
});
```

### IPromptLabService

Manage stateful prompt engineering sessions with conversation history.

```csharp
public interface IPromptLabService
{
    Task<SessionCreatedResponse> CreateSessionAsync(CreateSessionRequest request, CancellationToken ct = default);
    SessionInfoResponse? GetSession(string sessionId);
    IReadOnlyList<string> GetActiveSessions();
    
    Task<PromptResponse> SendPromptAsync(string sessionId, SendPromptRequest request, CancellationToken ct = default);
    IAsyncEnumerable<string> SendPromptStreamingAsync(string sessionId, SendPromptRequest request, CancellationToken ct = default);
    Task<PromptResponse> RetryLastAsync(string sessionId, CancellationToken ct = default);
    
    bool ClearSessionHistory(string sessionId);
    bool CloseSession(string sessionId);
    
    RenderTemplateResponse RenderTemplate(RenderTemplateRequest request);
    IReadOnlyList<string> GetTemplateVariables(string template);
}
```

**Usage Example:**
```csharp
// Create a session
var session = await promptLabService.CreateSessionAsync(new CreateSessionRequest
{
    Provider = LlmProviderType.Ollama,
    SystemPrompt = "You are a helpful assistant."
});

// Send prompts (maintains conversation history)
var response = await promptLabService.SendPromptAsync(session.SessionId, new SendPromptRequest
{
    Prompt = "What is machine learning?"
});

// Render templates
var rendered = promptLabService.RenderTemplate(new RenderTemplateRequest
{
    Template = "Translate '{{text}}' to {{language}}",
    Variables = new Dictionary<string, string>
    {
        ["text"] = "Hello",
        ["language"] = "Spanish"
    }
});
// Result: "Translate 'Hello' to Spanish"
```

## Dependency Injection

Register all services in your `Program.cs` or `Startup.cs`:

```csharp
using LlmPlayground.Services.Extensions;

// Add all services at once
builder.Services.AddLlmPlaygroundServices();

// Or add individual services as needed
builder.Services.AddLlmService();
builder.Services.AddPrologService();
builder.Services.AddPromptLabService();
```

All services are registered as **singletons**.

## Configuration

Configure providers in `appsettings.json`:

```json
{
  "Ollama": {
    "Host": "localhost",
    "Port": 11434,
    "Model": "llama3",
    "Scheme": "http",
    "ApiPath": "/v1",
    "SystemPrompt": null,
    "TimeoutSeconds": 300,
    "BaseUrlOverride": null
  },
  "LmStudio": {
    "Host": "localhost",
    "Port": 1234,
    "Model": "local-model",
    "Scheme": "http",
    "ApiPath": "/v1",
    "SystemPrompt": null,
    "TimeoutSeconds": 300,
    "BaseUrlOverride": null
  },
  "OpenAI": {
    "ApiKey": "your-api-key",
    "Model": "gpt-4o-mini",
    "SystemPrompt": null,
    "BaseUrlOverride": null,
    "TimeoutSeconds": 120
  },
  "Prolog": {
    "ExecutablePath": null,
    "WorkingDirectory": null
  }
}
```

## DTOs Reference

### LLM Models
- `ChatRequest` - Chat completion request with messages
- `CompletionRequest` - Simple prompt completion request
- `ChatMessageDto` - Individual chat message (role + content)
- `InferenceOptionsDto` - Temperature, MaxTokens, TopP, RepeatPenalty
- `CompletionResponse` - Response with text, tokens, duration
- `ModelInfoDto` - Model information (id, owner, created)
- `LlmProviderType` - Enum from `LlmPlayground.Core`: Ollama, LmStudio, OpenAI, Local

### Prolog Models
- `PrologQueryRequest` - Query string to execute
- `PrologFileRequest` - File path and optional goal
- `PrologResponse` - Success, output, error, exit code
- `PrologAvailabilityResponse` - IsAvailable flag and info

### PromptLab Models
- `CreateSessionRequest` - Provider, system prompt, options
- `SessionCreatedResponse` - Session ID and provider name
- `SendPromptRequest` - Prompt text and stream flag
- `PromptResponse` - Full response with metrics
- `SessionInfoResponse` - Session details and history
- `PromptExchangeDto` - Prompt/response pair with timestamp
- `RenderTemplateRequest` - Template and variables
- `RenderTemplateResponse` - Rendered prompt and variable info

## Project Dependencies

- `LlmPlayground.Core` - LLM provider implementations
- `LlmPlayground.Prolog` - Prolog runner
- `LlmPlayground.PromptLab` - Prompt session management
- `Microsoft.Extensions.DependencyInjection.Abstractions`
- `Microsoft.Extensions.Configuration.Abstractions`
- `Microsoft.Extensions.Logging.Abstractions`
