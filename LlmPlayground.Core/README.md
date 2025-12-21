# LlmPlayground.Core

Core library providing LLM provider abstractions and implementations for connecting to various language model backends.

## Overview

This project defines the foundational interfaces and implementations for interacting with Large Language Models. It supports multiple backends including Ollama, LM Studio, OpenAI, and local GGUF models via LLamaSharp.

## Features

- **Unified Provider Interface** - `ILlmProvider` for all LLM backends
- **Multiple Providers** - Ollama, LM Studio, OpenAI, and local GGUF models
- **Streaming Support** - Real-time token streaming with `IAsyncEnumerable`
- **Chat & Completion** - Both single-prompt and conversation-based APIs
- **Model Listing** - Query available models from supported providers
- **Factory Pattern** - `ILlmProviderFactory` for provider creation
- **Full Async/Await** - All operations support cancellation tokens

## Supported Providers

| Provider | Class | Description |
|----------|-------|-------------|
| Ollama | `OllamaProvider` | Local LLM server using Ollama |
| LM Studio | `LmStudioProvider` | LM Studio local server |
| OpenAI | `OpenAiProvider` | OpenAI API (GPT-4, GPT-4o, etc.) |
| Local | `LocalLlmProvider` | Direct GGUF model loading via LLamaSharp |

## Quick Start

### Using a Provider Directly

```csharp
using LlmPlayground.Core;

// Create and configure a provider
var config = new LmStudioConfiguration
{
    Host = "localhost",
    Port = 1234,
    Model = "local-model"
};

await using var provider = new LmStudioProvider(config);
await provider.InitializeAsync();

// Simple completion
var result = await provider.CompleteAsync("Explain recursion in one sentence.");
Console.WriteLine(result.Text);

// Streaming completion
await foreach (var token in provider.CompleteStreamingAsync("Write a haiku about coding."))
{
    Console.Write(token);
}
```

### Using the Factory

```csharp
using LlmPlayground.Core;
using LlmPlayground.Core.Extensions;
using Microsoft.Extensions.DependencyInjection;

// Register with DI
services.AddLlmProviders(configuration);

// Resolve factory and create provider
var factory = serviceProvider.GetRequiredService<ILlmProviderFactory>();
var provider = factory.CreateProvider(LlmProviderType.Ollama);
await provider.InitializeAsync();
```

### Chat with Conversation History

```csharp
var messages = new List<ChatMessage>
{
    new(ChatRole.System, "You are a helpful assistant."),
    new(ChatRole.User, "What is dependency injection?")
};

var response = await provider.ChatAsync(messages);
Console.WriteLine(response.Text);

// Continue the conversation
messages.Add(new(ChatRole.Assistant, response.Text));
messages.Add(new(ChatRole.User, "Show me an example in C#."));

var followUp = await provider.ChatAsync(messages);
```

## Core Types

### ILlmProvider

The main interface for all LLM providers:

```csharp
public interface ILlmProvider : IAsyncDisposable, IDisposable
{
    string ProviderName { get; }
    bool IsReady { get; }
    
    Task InitializeAsync(CancellationToken ct = default);
    
    Task<LlmCompletionResult> CompleteAsync(string prompt, LlmInferenceOptions? options = null, CancellationToken ct = default);
    IAsyncEnumerable<string> CompleteStreamingAsync(string prompt, LlmInferenceOptions? options = null, CancellationToken ct = default);
    
    Task<LlmCompletionResult> ChatAsync(IReadOnlyList<ChatMessage> messages, LlmInferenceOptions? options = null, CancellationToken ct = default);
    IAsyncEnumerable<string> ChatStreamingAsync(IReadOnlyList<ChatMessage> messages, LlmInferenceOptions? options = null, CancellationToken ct = default);
}
```

### LlmInferenceOptions

Configure generation parameters:

```csharp
var options = new LlmInferenceOptions
{
    MaxTokens = 2048,      // Maximum tokens to generate
    Temperature = 0.7f,    // Sampling temperature (0 = deterministic)
    TopP = 0.9f,           // Nucleus sampling threshold
    RepeatPenalty = 1.1f   // Penalty for token repetition
};
```

### LlmCompletionResult

Response from completion requests:

```csharp
public record LlmCompletionResult(
    string Text,           // Generated text
    int TokensGenerated,   // Number of tokens produced
    TimeSpan Duration      // Time taken for generation
);
```

## Project Structure

```
LlmPlayground.Core/
├── ILlmProvider.cs              # Core provider interface
├── ILlmProviderFactory.cs       # Factory interface + LlmProviderType enum
├── LlmProviderFactory.cs        # Factory implementation
├── OpenAiCompatibleProviderBase.cs  # Base class for OpenAI-compatible APIs
├── OllamaProvider.cs            # Ollama implementation
├── LmStudioProvider.cs          # LM Studio implementation
├── OpenAiProvider.cs            # OpenAI implementation
├── LocalLlmProvider.cs          # Local GGUF model implementation
└── Extensions/
    └── ServiceCollectionExtensions.cs  # DI registration
```

## Configuration Classes

Each provider has a configuration class:

- `OllamaConfiguration` - Host, Port, Scheme, ApiPath, Model, etc.
- `LmStudioConfiguration` - Host, Port, Scheme, ApiPath, Model, etc.
- `OpenAiConfiguration` - ApiKey, Model, BaseUrl, TimeoutSeconds
- `LocalLlmConfiguration` - ModelPath, Backend (Cpu/Vulkan/Cuda), ContextSize, GpuLayerCount, etc.

## Dependencies

- `Azure.AI.OpenAI` - OpenAI API client
- `LLamaSharp` - Local GGUF model inference (with CPU and Vulkan backends)
- `Microsoft.Extensions.Http` - HTTP client factory support
- `Microsoft.Extensions.Options` - Configuration binding
- `Microsoft.Extensions.Configuration.Abstractions` - Configuration support
- `Microsoft.Extensions.DependencyInjection.Abstractions` - DI support
- `Microsoft.Extensions.Logging.Abstractions` - Logging support

