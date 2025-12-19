# LlmPlayground

A .NET 9 library for connecting to various LLM (Large Language Model) APIs with a unified interface.

## Features

- **OpenAI Support** - Connect to ChatGPT (GPT-4, GPT-4o, GPT-3.5-turbo, etc.)
- **Ollama Support** - Connect to locally running Ollama server
- **LM Studio Support** - Connect to LM Studio's local server
- **Local LLM Support** - Run GGUF models directly using LLamaSharp
- **Multi-Backend GPU Acceleration** - CPU, Vulkan (AMD/Intel/NVIDIA), CUDA (NVIDIA)
- **Streaming Support** - Real-time token generation with `IAsyncEnumerable`
- **Conversation History** - Multi-turn chat with `ChatAsync` and `ChatStreamingAsync`
- **Cancellation Support** - Cancel requests gracefully with `CancellationToken`
- **Configuration-Driven** - All settings via `appsettings.json`
- **Extensible Architecture** - Easy to add new LLM providers

## Project Structure

```
LlmPlayground/
├── LlmPlayground.sln
├── LlmPlayground.Core/          # Class Library
│   ├── ILlmProvider.cs          # Base interface for all LLM providers
│   ├── OpenAiProvider.cs        # OpenAI/ChatGPT provider
│   ├── OllamaProvider.cs        # Ollama provider
│   ├── LmStudioProvider.cs      # LM Studio provider
│   └── LocalLlmProvider.cs      # Local GGUF model provider
├── LlmPlayground.Console/       # Console Demo App
│   ├── Program.cs
│   └── appsettings.json
└── Tests/                       # Unit Tests
    ├── LlmPlayground.Core.Tests/
    └── LlmPlayground.Console.Tests/
```

## Getting Started

### Prerequisites

- .NET 9 SDK
- A GGUF model file (e.g., from [Hugging Face](https://huggingface.co/models?search=gguf))

### Installation

```bash
cd LlmPlayground
dotnet restore
dotnet build
```

### Configuration

Edit `LlmPlayground.Console/appsettings.json`:

```json
{
  "Provider": "Ollama",
  "OpenAI": {
    "ApiKey": "sk-your-api-key",
    "Model": "gpt-4o-mini",
    "SystemPrompt": "You are a helpful assistant."
  },
  "Ollama": {
    "Host": "localhost",
    "Port": 11434,
    "Scheme": "http",
    "ApiPath": "/v1",
    "Model": "llama3",
    "SystemPrompt": "You are a helpful assistant."
  },
  "LmStudio": {
    "Host": "localhost",
    "Port": 1234,
    "Scheme": "http",
    "ApiPath": "/v1",
    "Model": "local-model",
    "SystemPrompt": "You are a helpful assistant."
  },
  "LocalLlm": {
    "ModelPath": "C:\\models\\your-model.gguf",
    "Backend": "Vulkan",
    "GpuLayerCount": -1
  },
  "Inference": {
    "MaxTokens": 256,
    "Temperature": 0.7
  }
}
```

### Provider Options

| Provider | Description |
|----------|-------------|
| `OpenAI` | OpenAI ChatGPT API (requires API key) |
| `Ollama` | Ollama local server (default: localhost:11434) |
| `LmStudio` | LM Studio local server (default: localhost:1234) |
| `LocalLlm` | Direct GGUF model loading via LLamaSharp |

---

## Configuration Reference

### OpenAI Configuration

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `ApiKey` | string | *(required)* | OpenAI API key |
| `Model` | string | `gpt-4o-mini` | Model to use (gpt-4, gpt-4o, gpt-3.5-turbo, o1-preview, etc.) |
| `SystemPrompt` | string | `null` | Optional system prompt to set assistant behavior |
| `BaseUrl` | string | `null` | Custom API endpoint (for Azure OpenAI or proxies) |
| `TimeoutSeconds` | int | `120` | Request timeout in seconds |

**Supported Models:**
- `gpt-4o`, `gpt-4o-mini` (recommended)
- `gpt-4`, `gpt-4-turbo`
- `gpt-3.5-turbo`
- `o1-preview`, `o1-mini`

**Example: Azure OpenAI**
```json
{
  "OpenAI": {
    "ApiKey": "your-azure-key",
    "Model": "gpt-4",
    "BaseUrl": "https://your-resource.openai.azure.com"
  }
}
```

---

### Ollama Configuration

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Host` | string | `localhost` | Server hostname or IP address |
| `Port` | int | `11434` | Server port |
| `Scheme` | string | `http` | URL scheme (`http` or `https`) |
| `ApiPath` | string | `/v1` | API base path (change if API version updates) |
| `Model` | string | `llama3` | Model identifier (must be pulled first: `ollama pull llama3`) |
| `SystemPrompt` | string | `null` | Optional system prompt |
| `TimeoutSeconds` | int | `300` | Request timeout in seconds |
| `BaseUrlOverride` | string | `null` | Full URL override (ignores Host/Port/Scheme/ApiPath) |

**Popular Ollama Models:**
- `llama3`, `llama3:70b`
- `mistral`, `mixtral`
- `codellama`, `deepseek-coder`
- `phi3`, `gemma`

**Example: Remote Ollama server with HTTPS**
```json
{
  "Ollama": {
    "Host": "ollama.example.com",
    "Port": 443,
    "Scheme": "https",
    "Model": "llama3"
  }
}
```

**Example: Full URL override**
```json
{
  "Ollama": {
    "BaseUrlOverride": "https://proxy.company.com/ollama/api",
    "Model": "llama3"
  }
}
```

---

### LM Studio Configuration

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Host` | string | `localhost` | Server hostname or IP address |
| `Port` | int | `1234` | Server port |
| `Scheme` | string | `http` | URL scheme (`http` or `https`) |
| `ApiPath` | string | `/v1` | API base path (change if API version updates) |
| `Model` | string | `local-model` | Model identifier (LM Studio uses loaded model) |
| `SystemPrompt` | string | `null` | Optional system prompt |
| `TimeoutSeconds` | int | `300` | Request timeout in seconds |
| `BaseUrlOverride` | string | `null` | Full URL override (ignores Host/Port/Scheme/ApiPath) |

---

### LocalLlm Configuration

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `ModelPath` | string | *(required)* | Path to the GGUF model file |
| `Backend` | enum | `Cpu` | Inference backend: `Cpu`, `Vulkan`, or `Cuda` |
| `GpuDeviceIndex` | int | `0` | GPU device index (for multi-GPU systems) |
| `GpuLayerCount` | int | `0` | Layers to offload to GPU (`0` = CPU only, `-1` = all layers) |
| `ContextSize` | uint | `2048` | Context window size in tokens |
| `ThreadCount` | uint | `CPU cores / 2` | Number of threads for inference |

**Backend Options:**

| Backend | Description | GPU Support |
|---------|-------------|-------------|
| `Cpu` | CPU-only inference | None |
| `Vulkan` | Vulkan API acceleration | AMD, Intel, NVIDIA |
| `Cuda` | CUDA acceleration | NVIDIA only |

**Example: AMD GPU with Vulkan**
```json
{
  "LocalLlm": {
    "ModelPath": "C:\\models\\llama-2-7b.Q4_K_M.gguf",
    "Backend": "Vulkan",
    "GpuLayerCount": -1,
    "ContextSize": 4096
  }
}
```

**Example: NVIDIA GPU with CUDA**
```json
{
  "LocalLlm": {
    "ModelPath": "/models/mistral-7b.Q5_K_M.gguf",
    "Backend": "Cuda",
    "GpuDeviceIndex": 0,
    "GpuLayerCount": -1,
    "ContextSize": 8192,
    "ThreadCount": 8
  }
}
```

---

### Inference Options

These options can be passed to `CompleteAsync()` and `CompleteStreamingAsync()`:

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `MaxTokens` | int | `256` | Maximum tokens to generate |
| `Temperature` | float | `0.7` | Sampling temperature (0.0 = deterministic, higher = more random) |
| `TopP` | float | `0.9` | Top-p (nucleus) sampling threshold |
| `RepeatPenalty` | float | `1.1` | Penalty for repeating tokens |

**Example in appsettings.json:**
```json
{
  "Inference": {
    "MaxTokens": 512,
    "Temperature": 0.5,
    "TopP": 0.95,
    "RepeatPenalty": 1.2
  }
}
```

---

### Full Configuration Example

```json
{
  "Provider": "Ollama",
  "OpenAI": {
    "ApiKey": "sk-your-api-key",
    "Model": "gpt-4o-mini",
    "SystemPrompt": "You are a helpful assistant.",
    "BaseUrl": "",
    "TimeoutSeconds": 120
  },
  "Ollama": {
    "Host": "localhost",
    "Port": 11434,
    "Scheme": "http",
    "ApiPath": "/v1",
    "Model": "llama3",
    "SystemPrompt": "You are a helpful assistant.",
    "TimeoutSeconds": 300,
    "BaseUrlOverride": ""
  },
  "LmStudio": {
    "Host": "localhost",
    "Port": 1234,
    "Scheme": "http",
    "ApiPath": "/v1",
    "Model": "local-model",
    "SystemPrompt": "You are a helpful assistant.",
    "TimeoutSeconds": 300,
    "BaseUrlOverride": ""
  },
  "LocalLlm": {
    "ModelPath": "C:\\models\\your-model.gguf",
    "Backend": "Vulkan",
    "GpuDeviceIndex": 0,
    "GpuLayerCount": -1,
    "ContextSize": 4096,
    "ThreadCount": 0
  },
  "Inference": {
    "MaxTokens": 256,
    "Temperature": 0.7,
    "TopP": 0.9,
    "RepeatPenalty": 1.1
  }
}
```

### Running

```bash
cd LlmPlayground.Console
dotnet run
```

Or specify a local model path directly:

```bash
dotnet run -- "C:\models\llama-2-7b.Q4_K_M.gguf"
```

**Silent Mode** (auto-select first available model):
```bash
dotnet run -- --silent
```

### Interactive Commands

| Command | Description |
|---------|-------------|
| `help` | Show available commands |
| `exit` | Exit the application |
| `stream` | Toggle streaming mode on/off |
| `settings` | Configure inference options (MaxTokens, Temperature, etc.) |
| `reset` | Reset all saved preferences |
| `clear` | Clear conversation history |
| `history` | Show conversation history |

### Conversation History

The console app maintains conversation history within a session, allowing the LLM to remember context from previous exchanges. Use `clear` to start a fresh conversation, or `history` to see what's been discussed.

### Cancellation

Press **Ctrl+C** during generation to stop the current response. This sends a cancellation signal to the LLM server to stop generating tokens. The app remains running and you can continue with a new prompt.

**Note:** Ctrl+C only cancels the current generation—it does not exit the application. Use the `exit` command to quit.

**Single prompt mode** also supports cancellation:
```bash
dotnet run -- --prompt "Your question here"
# Press Ctrl+C to stop generation
```

### Model Discovery

For **Ollama** and **LM Studio** providers, the console app automatically:
1. Discovers available models from the server
2. Lets you choose which model to use (interactive mode)
3. **Remembers your selection** for future sessions

Preferences are stored in `userpreferences.json` and include:
- Selected model per provider
- Streaming mode preference
- Inference settings (MaxTokens, Temperature, TopP, RepeatPenalty)

## Usage

### OpenAI Provider

```csharp
var config = new OpenAiConfiguration
{
    ApiKey = "sk-your-api-key",
    Model = "gpt-4o",
    SystemPrompt = "You are a helpful assistant."
};

await using var provider = new OpenAiProvider(config);
await provider.InitializeAsync();

var result = await provider.CompleteAsync("What is the capital of France?");
Console.WriteLine(result.Text);
```

### Ollama Provider

```csharp
var config = new OllamaConfiguration
{
    Host = "localhost",
    Port = 11434,
    Model = "llama3"
};

await using var provider = new OllamaProvider(config);
await provider.InitializeAsync();

var result = await provider.CompleteAsync("What is the capital of France?");
Console.WriteLine(result.Text);
```

### LM Studio Provider

```csharp
var config = new LmStudioConfiguration
{
    Host = "localhost",
    Port = 1234,
    Model = "local-model"
};

await using var provider = new LmStudioProvider(config);
await provider.InitializeAsync();

var result = await provider.CompleteAsync("What is the capital of France?");
Console.WriteLine(result.Text);
```

### Local LLM Provider (Direct GGUF)

```csharp
var config = new LocalLlmConfiguration
{
    ModelPath = "path/to/model.gguf",
    Backend = LlmBackendType.Vulkan,
    GpuLayerCount = -1  // Offload all layers to GPU
};

await using var provider = new LocalLlmProvider(config);
await provider.InitializeAsync();

var result = await provider.CompleteAsync("What is the capital of France?");
Console.WriteLine(result.Text);
```

### Streaming

```csharp
await foreach (var token in provider.CompleteStreamingAsync("Tell me a story"))
{
    Console.Write(token);
}
```

### Custom Inference Options

```csharp
var options = new LlmInferenceOptions
{
    MaxTokens = 512,
    Temperature = 0.5f,
    TopP = 0.95f,
    RepeatPenalty = 1.2f
};

var result = await provider.CompleteAsync("Explain quantum computing", options);
```

### Multi-Turn Conversations

Use `ChatAsync` and `ChatStreamingAsync` for conversations with history:

```csharp
var history = new List<ChatMessage>
{
    new(ChatRole.System, "You are a helpful assistant."),
    new(ChatRole.User, "My name is Alice."),
    new(ChatRole.Assistant, "Hello Alice! How can I help you today?"),
    new(ChatRole.User, "What is my name?")
};

// Non-streaming
var result = await provider.ChatAsync(history);
Console.WriteLine(result.Text); // "Your name is Alice."

// Streaming
await foreach (var token in provider.ChatStreamingAsync(history))
{
    Console.Write(token);
}
```

### Cancellation

All async methods support cancellation:

```csharp
using var cts = new CancellationTokenSource();

// Cancel after 10 seconds
cts.CancelAfter(TimeSpan.FromSeconds(10));

try
{
    var result = await provider.CompleteAsync("Write a long essay", cancellationToken: cts.Token);
}
catch (OperationCanceledException)
{
    Console.WriteLine("Request was cancelled.");
}
```

### Model Discovery (Ollama/LM Studio)

Ollama and LM Studio providers implement `IModelListingProvider` for model discovery:

```csharp
var config = new OllamaConfiguration { Model = "placeholder" };
var provider = new OllamaProvider(config);

// Discover available models
var models = await provider.GetAvailableModelsAsync();
foreach (var model in models)
{
    Console.WriteLine($"{model.Id} (by {model.OwnedBy})");
}

// Select a model dynamically
provider.SetModel(models[0].Id);

// Now initialize and use
await provider.InitializeAsync();
var result = await provider.CompleteAsync("Hello!");
```

## Running Tests

```bash
dotnet test --verbosity normal
```

## Adding New Providers

Implement the `ILlmProvider` interface:

```csharp
public class MyCustomProvider : ILlmProvider
{
    public string ProviderName => "MyCustomProvider";
    public bool IsReady { get; private set; }

    public Task InitializeAsync(CancellationToken ct = default) { /* ... */ }
    public Task<LlmCompletionResult> CompleteAsync(string prompt, ...) { /* ... */ }
    public IAsyncEnumerable<string> CompleteStreamingAsync(string prompt, ...) { /* ... */ }
    public void Dispose() { /* ... */ }
    public ValueTask DisposeAsync() { /* ... */ }
}
```

## License

MIT License

