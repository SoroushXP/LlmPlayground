# LlmPlayground.Utilities

Utility library providing validation, sanitization, and security helpers for the LlmPlayground API layer.

## Overview

This project provides reusable utilities that protect the core services from potentially harmful or malformed input. It's designed to be used by API controllers to validate and sanitize requests before they reach the Core, Prolog, or PromptLab projects.

## Project Structure

```
LlmPlayground.Utilities/
├── Extensions/
│   └── ServiceCollectionExtensions.cs   # DI registration extensions
├── Logging/
│   ├── ConsoleInterceptor.cs            # Console output capture
│   ├── ILogFormatter.cs                 # Log formatting interfaces
│   ├── ILoggerService.cs                # Logger service interface
│   ├── ILogSink.cs                      # Sink abstraction
│   ├── LogEntry.cs                      # Log entry models
│   ├── LoggerService.cs                 # Main logger implementation
│   ├── LoggingConfiguration.cs          # Configuration & builder
│   ├── LoggingExtensions.cs             # Extension methods
│   ├── LogLevel.cs                      # Log level enum
│   └── Sinks/
│       ├── ConsoleSink.cs               # Console output sink
│       └── FileSink.cs                  # File sink with rolling
├── Sanitization/
│   └── InputSanitizer.cs                # Input cleaning utilities
└── Validation/
    ├── IRequestValidator.cs             # Validator interface
    ├── RequestValidator.cs              # Validator implementation
    └── ValidationResult.cs              # Validation result models
```

## Features

### Request Validation

The `IRequestValidator` interface provides methods to validate various types of input:

```csharp
public interface IRequestValidator
{
    ValidationResult ValidatePrompt(string? prompt, int maxLength = 100_000);
    ValidationResult ValidateFilePath(string? filePath, string? allowedBasePath = null, IEnumerable<string>? allowedExtensions = null);
    ValidationResult ValidatePrologQuery(string? query);
    ValidationResult ValidateChatMessage(string? role, string? content);
    ValidationResult ValidateInferenceOptions(int? maxTokens, float? temperature, float? topP, float? repeatPenalty);
}
```

#### Prompt Validation
- Checks for null/empty input
- Enforces maximum length limits
- Detects null bytes (binary injection attempts)
- Warns about excessive control characters

#### File Path Validation
- Prevents path traversal attacks (`../`, `..\`)
- Validates against allowed base paths
- Restricts to allowed file extensions
- Blocks dangerous Windows paths (system directories)

#### Prolog Query Validation
- Blocks dangerous predicates: `shell`, `system`, `exec`, `popen`, `process_create`
- Blocks file operations: `open`, `close`, `read`, `write`, `delete_file`
- Prevents system modification: `halt`, `abort`, `getenv`, `setenv`

#### Chat Message Validation
- Validates role is one of: `system`, `user`, `assistant`
- Applies prompt validation to content

#### Inference Options Validation
- MaxTokens: 1 - 100,000
- Temperature: 0 - 2.0 (warning above 2.0)
- TopP: 0 (exclusive) - 1.0 (inclusive)
- RepeatPenalty: 0 - 5.0 (warning above 5.0)

### Input Sanitization

The `InputSanitizer` static class provides methods to clean input:

```csharp
// General text sanitization
string clean = InputSanitizer.Sanitize(input, SanitizationOptions.Strict);

// File path sanitization
string safePath = InputSanitizer.SanitizeFilePath(userPath);

// Safe logging (masks sensitive data)
string logSafe = InputSanitizer.SanitizeForLogging(sensitiveInput);

// Format-specific escaping
string urlEncoded = InputSanitizer.UrlEncode(text);
string jsonSafe = InputSanitizer.EscapeForJson(text);
string noHtml = InputSanitizer.StripHtml(htmlContent);
```

#### Sanitization Options

```csharp
// Default options
SanitizationOptions.Default  // Remove null bytes, control chars, normalize line endings, trim

// Minimal (for preserving formatting)
SanitizationOptions.Minimal  // Only removes null bytes

// Strict (for untrusted input)
SanitizationOptions.Strict   // All cleaning + collapse whitespace + 50k max length
```

### Validation Results

Results include detailed error information with severity levels:

```csharp
var result = validator.ValidatePrompt(input);

if (!result.IsValid)
{
    foreach (var error in result.Errors)
    {
        // error.Field - which field failed
        // error.Message - what went wrong
        // error.Severity - Warning, Error, or Critical
    }
    
    // Or get a summary
    string summary = result.GetErrorSummary();
}
```

#### Severity Levels

- **Warning** - Input is acceptable but suboptimal
- **Error** - Input is invalid and should be rejected
- **Critical** - Security issue that must be blocked

## Usage Examples

### In an API Controller

```csharp
[ApiController]
[Route("api/[controller]")]
public class LlmController : ControllerBase
{
    private readonly IRequestValidator _validator;
    private readonly ILlmService _llmService;

    public LlmController(IRequestValidator validator, ILlmService llmService)
    {
        _validator = validator;
        _llmService = llmService;
    }

    [HttpPost("complete")]
    public async Task<IActionResult> Complete(CompletionRequest request)
    {
        // Validate prompt
        var validation = _validator.ValidatePrompt(request.Prompt);
        if (!validation.IsValid)
        {
            return BadRequest(new { errors = validation.Errors });
        }

        // Validate options if provided
        if (request.Options is not null)
        {
            var optionsValidation = _validator.ValidateInferenceOptions(
                request.Options.MaxTokens,
                request.Options.Temperature,
                request.Options.TopP,
                request.Options.RepeatPenalty);
            
            if (!optionsValidation.IsValid)
            {
                return BadRequest(new { errors = optionsValidation.Errors });
            }
        }

        // Safe to process
        var response = await _llmService.CompleteAsync(request);
        return Ok(response);
    }
}
```

### Validating Prolog Requests

```csharp
[HttpPost("prolog/execute")]
public async Task<IActionResult> ExecuteProlog(PrologFileRequest request)
{
    // Validate file path with restrictions
    var pathValidation = _validator.ValidateFilePath(
        request.FilePath,
        allowedBasePath: _prologFilesPath,
        allowedExtensions: new[] { ".pl" });
    
    if (!pathValidation.IsValid)
    {
        _logger.LogWarning("Blocked Prolog request: {Errors}", pathValidation.GetErrorSummary());
        return BadRequest(new { errors = pathValidation.Errors });
    }

    // Validate goal if provided
    if (request.Goal is not null)
    {
        var goalValidation = _validator.ValidatePrologQuery(request.Goal);
        if (!goalValidation.IsValid)
        {
            return BadRequest(new { errors = goalValidation.Errors });
        }
    }

    return Ok(await _prologService.ExecuteFileAsync(request));
}
```

### Sanitizing for Logging

```csharp
public async Task<IActionResult> Chat(ChatRequest request)
{
    // Safe to log - masks API keys, emails, passwords
    _logger.LogInformation("Chat request: {Request}", 
        InputSanitizer.SanitizeForLogging(request.ToString()));
    
    // ...
}
```

## Dependency Injection

Register all utilities with a single extension method:

```csharp
using LlmPlayground.Utilities.Extensions;

// In Program.cs or Startup.cs
builder.Services.AddLlmPlaygroundUtilities();
```

This registers:
- `IRequestValidator` as `RequestValidator` (singleton)

## Security Considerations

### What This Library Protects Against

1. **Path Traversal** - Attempts to access files outside allowed directories
2. **Code Injection** - Dangerous Prolog predicates that could execute system commands
3. **Binary Injection** - Null bytes and control characters in text input
4. **Resource Exhaustion** - Extremely large inputs or unreasonable parameter values
5. **Information Leakage** - Sensitive data in logs (API keys, passwords, emails)

### What This Library Does NOT Protect Against

1. **Prompt Injection** - LLM-specific attacks should be handled at the application level
2. **Authentication/Authorization** - Use ASP.NET Core Identity or similar
3. **Rate Limiting** - Use middleware like AspNetCoreRateLimit
4. **SQL Injection** - Use parameterized queries in your data layer

## Centralized Logging System

The utilities library includes a comprehensive, professional logging system with console output interception.

### Features

- **Multiple Sinks** - Log to console and/or files simultaneously
- **Rolling File Support** - Daily or size-based file rotation with retention policies
- **Console Interception** - Automatically capture all `Console.Write`/`WriteLine` calls
- **Structured Logging** - Rich log entries with properties, correlation IDs, and context
- **Async Support** - Non-blocking file writes with configurable buffering
- **DI Integration** - Full Microsoft.Extensions.DependencyInjection support

### Quick Start

```csharp
using LlmPlayground.Utilities.Extensions;
using LlmPlayground.Utilities.Logging;

// Option 1: Default configuration
services.AddLlmPlaygroundLogging();

// Option 2: Custom configuration
services.AddLlmPlaygroundLogging(new LoggingConfiguration
{
    MinimumLevel = LogLevel.Debug,
    EnableConsoleSink = true,
    EnableFileSink = true,
    FileDirectory = "logs",
    InterceptConsoleOutput = true
});

// Option 3: Builder pattern
services.AddLlmPlaygroundLogging(builder => builder
    .WithMinimumLevel(LogLevel.Debug)
    .WithConsoleSink(LogLevel.Information, useColors: true)
    .WithFileSink("logs", LogLevel.Debug, RollingPolicy.Daily, retainDays: 7)
    .WithConsoleInterception());

// Option 4: Full utilities with logging
services.AddLlmPlaygroundFullUtilities(
    logDirectory: "logs",
    interceptConsole: true);
```

### Using the Logger

```csharp
public class MyService
{
    private readonly ILoggerService _logger;
    private readonly ILoggerScope _log;

    public MyService(ILoggerService logger)
    {
        _logger = logger;
        _log = logger.CreateScope("MyService");
    }

    public void DoWork()
    {
        _log.Information("Starting work...");
        
        try
        {
            // Work here
            _log.Debug("Processing item");
        }
        catch (Exception ex)
        {
            _log.Error("Failed to complete work", ex);
            throw;
        }
    }
}
```

### Extension Methods

```csharp
// Auto-detect source from caller info
_logger.LogInfo("Message"); // Source: ClassName.MethodName

// Timed operations
await _logger.LogTimedAsync("Database query", async () =>
{
    await db.ExecuteAsync();
}); // Output: "Database query completed in 45ms"

// Structured logging with properties
_logger.LogStructured(LogLevel.Information, "User action", new Dictionary<string, object?>
{
    ["UserId"] = userId,
    ["Action"] = "Login"
});
```

### Console Interception

When enabled, all console output is automatically routed through the logging system:

```csharp
var logger = new LoggerBuilder()
    .WithFileSink("logs")
    .WithConsoleInterception()
    .Build();

// These now get logged to file as well:
Console.WriteLine("This message appears in logs!");
Console.Error.WriteLine("Errors logged at Warning level");
```

### Correlation IDs for Distributed Tracing

```csharp
using (logger.BeginCorrelationScope("request-12345"))
{
    logger.Information("Processing request");
    // All logs in this scope include the correlation ID
    await ProcessRequestAsync();
}
```

### Log Formatters

```csharp
// Default formatter with options
var formatter = new DefaultLogFormatter(new LogFormatterOptions
{
    IncludeTimestamp = true,
    UseUtcTimestamp = true,
    TimestampFormat = "yyyy-MM-dd HH:mm:ss.fff",
    UseShortLevelNames = true,  // INF, WRN, ERR
    IncludeThreadId = true,
    IncludeCorrelationId = true,
    IncludeSource = true,
    IncludeProperties = true,
    IncludeFullException = true
});

// JSON formatter for structured log analysis
var jsonFormatter = new JsonLogFormatter();
```

### File Sink Configuration

```csharp
var fileSink = new FileSink(new FileSinkOptions
{
    Directory = "logs",
    FileNamePrefix = "app_",
    RollingPolicy = RollingPolicy.Daily,  // New file each day (or Size, Never)
    MaxFileSizeBytes = 10 * 1024 * 1024,  // 10 MB (for Size policy)
    RetainDays = 7,                        // Auto-delete files older than 7 days
    BufferSize = 100,                      // Entries before flush
    FlushIntervalSeconds = 5
});
```

### Log Levels

| Level | Description |
|-------|-------------|
| `Trace` | Detailed tracing for development |
| `Debug` | Debugging information |
| `Information` | General application flow |
| `Console` | Captured console output |
| `Warning` | Potentially harmful situations |
| `Error` | Errors that allow continued operation |
| `Critical` | Application failures |
| `None` | Disables logging (filter only) |

## Project Dependencies

- `Microsoft.Extensions.DependencyInjection.Abstractions` - DI registration
- `Microsoft.Extensions.Logging.Abstractions` - Optional logging support

