# LlmPlayground

A .NET 9 library for connecting to various LLM (Large Language Model) APIs with a unified interface.

## Features

- **OpenAI Support** - Connect to ChatGPT (GPT-4, GPT-4o, GPT-3.5-turbo, etc.)
- **Ollama Support** - Connect to locally running Ollama server
- **LM Studio Support** - Connect to LM Studio's local server
- **Local LLM Support** - Run GGUF models directly using LLamaSharp
- **Prolog Support** - Execute Prolog files using SWI-Prolog
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
├── LlmPlayground.Core/               # Class Library - LLM Providers
│   ├── ILlmProvider.cs               # Base interface for all LLM providers
│   ├── ILlmProviderFactory.cs        # Factory interface and LlmProviderType enum
│   ├── LlmProviderFactory.cs         # Factory implementation with DI support
│   ├── OpenAiCompatibleProviderBase.cs # Base class for OpenAI-compatible APIs
│   ├── OpenAiProvider.cs             # OpenAI/ChatGPT provider
│   ├── OllamaProvider.cs             # Ollama provider
│   ├── LmStudioProvider.cs           # LM Studio provider
│   ├── LocalLlmProvider.cs           # Local GGUF model provider
│   └── Extensions/
│       └── ServiceCollectionExtensions.cs  # DI registration extensions
├── LlmPlayground.Services/           # Service Layer (for API integration)
│   ├── Interfaces/                   # Service interfaces
│   ├── Services/                     # Service implementations
│   └── Models/                       # DTOs for requests/responses
├── LlmPlayground.Utilities/          # Validation, Sanitization & Logging
│   ├── Validation/                   # Request validation
│   ├── Sanitization/                 # Input sanitization
│   └── Logging/                      # Centralized logging system
├── LlmPlayground.PromptLab/          # Prompt Engineering Library
│   ├── PromptSession.cs              # Conversation session management
│   ├── PromptTemplate.cs             # Template and builder utilities
│   └── PromptLabFactory.cs           # Factory methods
├── LlmPlayground.Prolog/             # Class Library - Prolog Runner
│   ├── PrologRunner.cs               # Execute Prolog files and queries
│   └── README.md                     # Prolog-specific documentation
├── LlmPlayground.Console/            # Console Demo App
│   ├── Program.cs
│   ├── InteractiveSession.cs         # Interactive command handling
│   ├── Configuration/                # Game generation settings
│   ├── Services/                     # Game generation service
│   ├── Helpers/                      # Prompt building and code extraction
│   ├── Models/                       # Request/response models
│   └── appsettings.json
└── Tests/                            # Unit Tests
    ├── LlmPlayground.Core.Tests/
    ├── LlmPlayground.Console.Tests/
    ├── LlmPlayground.Services.Tests/
    ├── LlmPlayground.Utilities.Tests/
    ├── LlmPlayground.PromptLab.Tests/
    └── LlmPlayground.Prolog.Tests/
```

## Getting Started

### Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [SWI-Prolog](https://www.swi-prolog.org/Download.html) (optional, for Prolog support)

### Installation

```bash
cd LlmPlayground
dotnet restore
dotnet build
```

### Which Provider Should I Use?

| Provider | Best For | Requirements |
|----------|----------|--------------|
| **Ollama** | Easiest local setup | Install Ollama app, download a model |
| **LM Studio** | GUI-based local models | Install LM Studio app, download a model |
| **OpenAI** | Highest quality responses | OpenAI API key (paid) |
| **LocalLlm** | Direct model control, offline use | Download a GGUF model file |

---

## Quick Start Guides

### Option 1: Using Ollama (Recommended for Beginners)

Ollama is the easiest way to run LLMs locally. It handles model downloading and serving automatically.

**Step 1: Install Ollama**
- Download from [ollama.com](https://ollama.com/download)
- Install and run the Ollama application

**Step 2: Download a Model**
```bash
# Open a terminal and run:
ollama pull llama3
# Or for a smaller/faster model:
ollama pull phi3
```

**Step 3: Configure the App**

Edit `LlmPlayground.Console/appsettings.json`:
```json
{
  "Provider": "Ollama",
  "Ollama": {
    "Host": "localhost",
    "Port": 11434,
    "Model": "llama3"
  }
}
```

**Step 4: Run**
```bash
cd LlmPlayground.Console
dotnet run
```

The app will connect to Ollama, show available models, and let you start chatting!

---

### Option 2: Using LM Studio

LM Studio provides a user-friendly GUI for downloading and running local models.

**Step 1: Install LM Studio**
- Download from [lmstudio.ai](https://lmstudio.ai/)
- Install and launch the application

**Step 2: Download a Model**
- In LM Studio, go to the "Discover" tab
- Search for a model (e.g., "llama", "mistral", "phi")
- Click "Download" on your chosen model

**Step 3: Start the Server**
- Go to the "Local Server" tab in LM Studio
- Load your downloaded model
- Click "Start Server" (default port: 1234)

**Step 4: Configure the App**

Edit `LlmPlayground.Console/appsettings.json`:
```json
{
  "Provider": "LmStudio",
  "LmStudio": {
    "Host": "localhost",
    "Port": 1234,
    "Model": "local-model"
  }
}
```

**Step 5: Run**
```bash
cd LlmPlayground.Console
dotnet run
```

---

### Option 3: Using OpenAI API

Connect to OpenAI's cloud-based models (GPT-4, GPT-4o, etc.). Requires a paid API key.

**Step 1: Get an API Key**
- Sign up at [platform.openai.com](https://platform.openai.com/)
- Go to API Keys and create a new secret key
- Add billing information (API calls cost money)

**Step 2: Configure the App**

Edit `LlmPlayground.Console/appsettings.json`:
```json
{
  "Provider": "OpenAI",
  "OpenAI": {
    "ApiKey": "sk-your-api-key-here",
    "Model": "gpt-4o-mini",
    "SystemPrompt": "You are a helpful assistant."
  }
}
```

> ⚠️ **Security**: Never commit your API key to version control. Consider using environment variables or user secrets for production use.

**Step 3: Run**
```bash
cd LlmPlayground.Console
dotnet run
```

---

### Option 4: Using Local GGUF Models (Advanced)

Run models directly without any server. Best for offline use or when you want full control.

**What is a GGUF file?**
GGUF is a file format for storing LLM models. These files contain the model weights and can be run locally using libraries like LLamaSharp.

**Step 1: Download a GGUF Model**

Find models on [Hugging Face](https://huggingface.co/models?search=gguf). Popular options:
- [Llama 3 8B](https://huggingface.co/QuantFactory/Meta-Llama-3-8B-Instruct-GGUF)
- [Mistral 7B](https://huggingface.co/TheBloke/Mistral-7B-Instruct-v0.2-GGUF)
- [Phi-3 Mini](https://huggingface.co/microsoft/Phi-3-mini-4k-instruct-gguf)

**Choosing a Quantization:**
- `Q4_K_M` - Good balance of quality and speed (recommended)
- `Q5_K_M` - Higher quality, slower
- `Q8_0` - Highest quality, slowest, largest file

**Step 2: Configure the App**

Edit `LlmPlayground.Console/appsettings.json`:
```json
{
  "Provider": "LocalLlm",
  "LocalLlm": {
    "ModelPath": "C:\\models\\llama-3-8b-instruct.Q4_K_M.gguf",
    "Backend": "Cpu",
    "GpuLayerCount": 0,
    "ContextSize": 4096
  }
}
```

**Backend Options:**
| Backend | GPU Support | Notes |
|---------|-------------|-------|
| `Cpu` | None | Works everywhere, slowest |
| `Vulkan` | AMD, Intel, NVIDIA | Good cross-platform GPU support |
| `Cuda` | NVIDIA only | Fastest for NVIDIA GPUs |

**GPU Acceleration:**
```json
{
  "LocalLlm": {
    "ModelPath": "C:\\models\\your-model.gguf",
    "Backend": "Vulkan",
    "GpuLayerCount": -1
  }
}
```
- `GpuLayerCount: -1` = Offload all layers to GPU (fastest)
- `GpuLayerCount: 0` = CPU only
- `GpuLayerCount: 20` = Offload 20 layers to GPU (partial)

**Step 3: Run**
```bash
cd LlmPlayground.Console
dotnet run
```

> 💡 **Tip**: First run may take a minute as the model loads into memory.

---

## Provider Comparison

| Feature | OpenAI | Ollama | LM Studio | LocalLlm |
|---------|--------|--------|-----------|----------|
| Internet Required | ✅ Yes | ❌ No | ❌ No | ❌ No |
| Cost | 💰 Paid | 🆓 Free | 🆓 Free | 🆓 Free |
| Setup Difficulty | Easy | Easy | Easy | Medium |
| Model Selection | Limited | Many | Many | Any GGUF |
| GPU Support | N/A | Automatic | Automatic | Manual config |
| Response Quality | Highest | Good | Good | Varies |

---

## Configuration Reference

### OpenAI Configuration

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `ApiKey` | string | *(required)* | OpenAI API key |
| `Model` | string | `gpt-4o-mini` | Model to use (gpt-4, gpt-4o, gpt-3.5-turbo, o1-preview, etc.) |
| `SystemPrompt` | string | `null` | Optional system prompt to set assistant behavior |
| `BaseUrlOverride` | string | `null` | Custom API endpoint (for Azure OpenAI or proxies) |
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
    "BaseUrlOverride": "https://your-resource.openai.azure.com"
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
    "BaseUrlOverride": "",
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
  },
  "GameGeneration": {
    "DefaultGameIdeaMaxTokens": 1024,
    "DefaultGameIdeaTemperature": 0.8,
    "DefaultPrologCodeMaxTokens": 2048,
    "DefaultPrologCodeTemperature": 0.5,
    "DefaultPrologGoal": "main",
    "MaxFixRetries": 3,
    "OutputDirectory": ""
  },
  "Prolog": {
    "SwiPrologPath": "swipl"
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
| `game` | Generate a Prolog-based logic game using LLM (with self-healing code) |

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

## Prolog Support

The `LlmPlayground.Prolog` library allows you to execute Prolog files and queries using SWI-Prolog.

### Installing SWI-Prolog

**Windows (winget)**
```powershell
winget install --id SWI-Prolog.SWI-Prolog -e
```

**macOS (Homebrew)**
```bash
brew install swi-prolog
```

**Linux (Snap)**
```bash
sudo snap install swi-prolog
```

### Using the Prolog Runner

```csharp
using LlmPlayground.Prolog;

var runner = new PrologRunner();

// Check if Prolog is available
if (await runner.IsPrologAvailableAsync())
{
    // Run a Prolog file with a goal
    var result = await runner.RunFileAsync("program.pl", "main");
    Console.WriteLine(result.Output);
    
    // Or run a query directly
    var queryResult = await runner.RunQueryAsync("X is 2 + 2, format('Result: ~w', [X])");
    Console.WriteLine(queryResult.Output); // "Result: 4"
}
```

For more details, see the [LlmPlayground.Prolog README](LlmPlayground.Prolog/README.md).

## Self-Healing Code Generation

The **Console app** includes a **self-healing retry loop** for Prolog code generation. When generated code fails to execute, the system automatically sends the errors back to the LLM to fix the code and retries execution.

### How It Works

1. **Generate** - The LLM generates Prolog code based on your game concept
2. **Execute** - The code is executed using SWI-Prolog
3. **Detect Errors** - If execution fails, errors are captured
4. **Fix** - The original code and errors are sent back to the LLM with a specialized fix prompt
5. **Retry** - The fixed code is saved and executed again
6. **Repeat** - Steps 3-5 repeat until success or max retries are exhausted

### Using in Console App

Use the `game` command in the interactive console to generate and execute Prolog games:

```
> game
Enter game theme (or press Enter for random): mystery
Enter additional requirements (or press Enter to skip): Include a detective character
Execute the generated game? (y/n): y

★ Generating game idea...
★ Generating Prolog code...
★ Executing Prolog code...
✓ Game executed successfully!
```

The console app automatically retries failed code with LLM-generated fixes.

### Configuration

Configure the retry behavior in `appsettings.json`:

```json
{
  "GameGeneration": {
    "MaxFixRetries": 3,
    "PrologFixSystemPrompt": "You are an expert SWI-Prolog debugger...",
    "PrologFixUserPromptTemplate": "The following Prolog code has errors..."
  }
}
```

| Setting | Default | Description |
|---------|---------|-------------|
| `MaxFixRetries` | `3` | Maximum number of fix attempts before giving up |
| `PrologFixSystemPrompt` | *(built-in)* | System prompt for the fix request |
| `PrologFixUserPromptTemplate` | *(built-in)* | User prompt template with `{PrologCode}` and `{Errors}` placeholders |

### Response Fields

The `GameGenerationResponse` includes information about fix attempts:

```json
{
  "success": true,
  "prologCode": "main :- write('Hello World!').",
  "executionSuccess": true,
  "executionOutput": "Hello World!",
  "fixAttempts": 2,
  "generatedFilePath": "C:\\temp\\game_20241221_143022.pl"
}
```

- `fixAttempts` - Number of times the LLM was asked to fix the code (0 if first attempt succeeded)
- `executionSuccess` - Whether the final execution was successful
- `executionError` - Error message if execution still failed after all retries

### Early Termination

The retry loop stops early in these cases:
- **Success** - Code executes without errors
- **No Error** - Execution returns no error message to fix
- **Same Code** - LLM returns identical code (prevents infinite loops)
- **Max Retries** - Configured maximum attempts reached

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

## Dependency Injection Support

The `LlmPlayground.Core` library provides full dependency injection support for ASP.NET Core and other DI-enabled applications.

### Registration

```csharp
using LlmPlayground.Core.Extensions;

// Register all providers from configuration
builder.Services.AddLlmProviders(builder.Configuration);

// Or register individual providers
builder.Services.AddOllamaProvider(options =>
{
    options.Host = "localhost";
    options.Port = 11434;
    options.Model = "llama3";
});

builder.Services.AddOpenAiProvider(options =>
{
    options.ApiKey = "sk-your-key";
    options.Model = "gpt-4o-mini";
});
```

### Using the Factory

```csharp
public class MyService
{
    private readonly ILlmProviderFactory _factory;

    public MyService(ILlmProviderFactory factory)
    {
        _factory = factory;
    }

    public async Task<string> GetResponseAsync(string prompt)
    {
        // Create a provider using the factory
        using var provider = _factory.CreateProvider(LlmProviderType.Ollama);
        await provider.InitializeAsync();
        
        var result = await provider.CompleteAsync(prompt);
        return result.Text;
    }
}
```

### Configuration from appsettings.json

```json
{
  "Ollama": {
    "Host": "localhost",
    "Port": 11434,
    "Model": "llama3"
  },
  "OpenAI": {
    "ApiKey": "your-api-key",
    "Model": "gpt-4o-mini"
  }
}
```

The `ILlmProviderFactory` is registered as a singleton and manages `HttpClient` instances through `IHttpClientFactory` for proper resource management.

## License

MIT License

