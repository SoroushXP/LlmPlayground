# LlmPlayground.PromptLab

A .NET class library for prompt engineering with LLM providers. Provides a clean, reusable API for building prompts, managing conversation sessions, and integrating with various LLM backends.

## Features

- **PromptSession**: Manages conversation history with streaming support
- **PromptTemplate**: Variable substitution with `{{variable}}` syntax
- **PromptBuilder**: Fluent API for constructing complex prompts
- **PromptLabFactory**: Easy provider creation from configuration

## Installation

Add a reference to `LlmPlayground.PromptLab` in your project:

```xml
<ProjectReference Include="..\LlmPlayground.PromptLab\LlmPlayground.PromptLab.csproj" />
```

## Quick Start

### Basic Usage

```csharp
using LlmPlayground.Core;
using LlmPlayground.PromptLab;

// Create a provider
var provider = new LmStudioProvider(new LmStudioConfiguration
{
    Host = "localhost",
    Port = 1234
});
await provider.InitializeAsync();

// Create a session
using var session = new PromptSession(provider, 
    systemPrompt: "You are a helpful coding assistant.");

// Send a prompt
var result = await session.SendAsync("Explain async/await in C#");
Console.WriteLine(result.Response);
Console.WriteLine($"Tokens: {result.TokensGenerated}, Time: {result.Duration.TotalSeconds:F2}s");
```

### Streaming Responses

```csharp
await foreach (var token in session.SendStreamingAsync("Write a poem about coding"))
{
    Console.Write(token);
}
```

### With Callback

```csharp
var result = await session.SendWithCallbackAsync(
    "Explain recursion",
    token => Console.Write(token));

Console.WriteLine($"\n\nTotal tokens: {result.TokensGenerated}");
```

## Prompt Templates

Create reusable prompts with variable substitution:

```csharp
// Create a template
var template = new PromptTemplate("""
    You are an expert in {{language}}.
    Please review this code and suggest improvements:
    
    ```{{language}}
    {{code}}
    ```
    
    Focus on: {{focus_areas}}
    """);

// Render with variables
var prompt = template.Render(new 
{
    language = "csharp",
    code = "public void DoSomething() { /* ... */ }",
    focus_areas = "performance and readability"
});

var result = await session.SendAsync(prompt);
```

### Loading Templates from Files

```csharp
var template = await PromptTemplate.FromFileAsync("prompts/code-review.txt");
```

### Validating Variables

```csharp
var missing = template.GetMissingVariables(myVariables);
if (missing.Any())
{
    Console.WriteLine($"Missing: {string.Join(", ", missing)}");
}
```

## Prompt Builder

Build complex prompts with a fluent API:

```csharp
var prompt = new PromptBuilder()
    .WithSystem("You are a senior developer.")
    .AppendLine("Please analyze this code:")
    .AppendCodeBlock(sourceCode, "csharp")
    .AppendLine()
    .AppendLine("Consider the following aspects:")
    .AppendNumberedList([
        "Code quality and best practices",
        "Performance implications",
        "Error handling",
        "Testing considerations"
    ])
    .AppendIf(includeSecurityReview, "Also review for security vulnerabilities.")
    .Build();
```

## Factory Methods

Create providers and sessions from configuration:

```csharp
// From IConfiguration (e.g., appsettings.json)
var provider = PromptLabFactory.CreateProvider("LmStudio", configuration);

// Or create specific providers
var ollamaProvider = PromptLabFactory.CreateOllamaProvider(configuration);
var lmStudioProvider = PromptLabFactory.CreateLmStudioProvider(configuration);
var openAiProvider = PromptLabFactory.CreateOpenAiProvider(configuration);

// Create inference options from config
var options = PromptLabFactory.CreateInferenceOptions(configuration);

// Create a session
var session = PromptLabFactory.CreateSession(provider, "System prompt", options);
```

### Configuration Format

```json
{
  "Provider": "LmStudio",
  "LmStudio": {
    "Host": "localhost",
    "Port": 1234,
    "Model": "local-model",
    "TimeoutSeconds": 300
  },
  "Ollama": {
    "Host": "localhost",
    "Port": 11434,
    "Model": "llama3"
  },
  "OpenAI": {
    "ApiKey": "your-api-key",
    "Model": "gpt-4o-mini"
  },
  "Inference": {
    "MaxTokens": 4096,
    "Temperature": 0.7,
    "TopP": 0.9,
    "RepeatPenalty": 1.1
  }
}
```

## Conversation History

The session automatically maintains conversation history:

```csharp
// First message
await session.SendAsync("What is dependency injection?");

// Follow-up (context is maintained)
await session.SendAsync("Can you show an example in C#?");

// Access history
foreach (var exchange in session.History)
{
    Console.WriteLine($"User: {exchange.Prompt}");
    Console.WriteLine($"Assistant: {exchange.Response}");
}

// Clear history to start fresh
session.ClearHistory();
```

## API Integration Example

```csharp
public class ChatService
{
    private readonly ILlmProvider _provider;
    private readonly LlmInferenceOptions _options;

    public ChatService(ILlmProvider provider, LlmInferenceOptions options)
    {
        _provider = provider;
        _options = options;
    }

    public async Task<ChatResponse> GetResponseAsync(ChatRequest request)
    {
        using var session = new PromptSession(_provider, request.SystemPrompt, _options);
        
        var result = await session.SendAsync(request.Message);
        
        return new ChatResponse
        {
            Message = result.Response,
            TokensUsed = result.TokensGenerated,
            ProcessingTime = result.Duration
        };
    }

    public async IAsyncEnumerable<string> StreamResponseAsync(
        string prompt,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var session = new PromptSession(_provider, options: _options);
        
        await foreach (var token in session.SendStreamingAsync(prompt, cancellationToken))
        {
            yield return token;
        }
    }
}
```

## Supported Providers

| Provider | Class | Description |
|----------|-------|-------------|
| LM Studio | `LmStudioProvider` | Local LLM server |
| Ollama | `OllamaProvider` | Local LLM with Ollama |
| OpenAI | `OpenAiProvider` | OpenAI API (GPT-4, etc.) |
| Local LLM | `LocalLlmProvider` | Local GGUF model inference via LLamaSharp |

## Dependencies

- `LlmPlayground.Core` - Core LLM provider interfaces
- `Microsoft.Extensions.Configuration.Abstractions` - Configuration support
- `Microsoft.Extensions.Configuration.Binder` - Configuration binding

## License

See the main repository LICENSE file.

