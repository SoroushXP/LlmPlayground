# LlmPlayground.Api

A RESTful API for generating Prolog-based logic games using LLM providers. This API combines the power of large language models with Prolog's logical programming capabilities to create and execute educational logic games.

## Features

- **Game Generation**: Generate creative logic game concepts using LLM
- **Prolog Code Generation**: Automatically generate SWI-Prolog code from game concepts
- **Safe Execution**: Execute generated Prolog code with safety validations
- **Multiple LLM Providers**: Support for Ollama, LM Studio, and OpenAI
- **Configurable Prompts**: All prompts are stored in configuration files for easy customization

## Getting Started

### Prerequisites

- .NET 9.0 SDK or later
- SWI-Prolog installed on the system (for code execution)
- One of the following LLM providers:
  - [Ollama](https://ollama.ai/) running locally
  - [LM Studio](https://lmstudio.ai/) running locally
  - OpenAI API key

### Running the API

```bash
cd LlmPlayground.Api
dotnet run
```

The API will start at `https://localhost:5001` (or `http://localhost:5000`).

### Swagger UI

Access the interactive API documentation at:
```
https://localhost:5001/swagger
```

## API Endpoints

### Generate Game

**POST** `/api/gamegenerator/generate`

Generates a Prolog-based logic game using LLM.

#### Request Body

```json
{
  "theme": "mystery",
  "description": "A detective game with clues",
  "provider": "LmStudio",
  "executeGame": true,
  "prologGoal": "main"
}
```

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `theme` | string | No | Theme for the game (e.g., "mystery", "adventure") |
| `description` | string | No | Additional requirements or context |
| `provider` | string | No | LLM provider: "Ollama", "LmStudio", or "OpenAI" |
| `executeGame` | boolean | No | Whether to execute the generated Prolog code (default: true) |
| `prologGoal` | string | No | Prolog goal to execute (default: "main") |

#### Response

```json
{
  "success": true,
  "gameIdea": "A mystery game where players solve clues...",
  "prologCode": "% Mystery Game\nmain :- solve_mystery...",
  "executionOutput": "Clue found: The butler did it!\n",
  "executionSuccess": true,
  "executionError": null,
  "providerUsed": "LmStudio",
  "duration": "00:00:15.234",
  "timings": {
    "gameIdeaGeneration": "00:00:05.123",
    "prologCodeGeneration": "00:00:08.456",
    "prologExecution": "00:00:01.655"
  }
}
```

### Health Check

**GET** `/api/gamegenerator/health`

Returns the health status of the API.

#### Response

```json
{
  "status": "healthy",
  "timestamp": "2024-12-21T10:30:00Z"
}
```

## Configuration

### appsettings.json

All prompts and settings are configurable via `appsettings.json`. This allows customization without recompiling the application.

#### Game Generation Settings

```json
{
  "GameGeneration": {
    "DefaultProviderString": "LmStudio",
    "GameIdeaSystemPrompt": "You are a game designer...",
    "GameIdeaUserPromptTemplate": "Generate a game concept...",
    "PrologCodeSystemPrompt": "You are an expert Prolog programmer...",
    "PrologCodeUserPromptTemplate": "Based on this concept...",
    "DefaultGameIdeaMaxTokens": 1024,
    "DefaultGameIdeaTemperature": 0.8,
    "DefaultPrologCodeMaxTokens": 2048,
    "DefaultPrologCodeTemperature": 0.5,
    "DefaultPrologGoal": "main"
  }
}
```

#### Prompt Templates

The prompts use placeholder syntax for dynamic content:

**Game Idea Prompt Template:**
- `{ThemeSection}` - Replaced with theme information
- `{DescriptionSection}` - Replaced with description/requirements

**Prolog Code Prompt Template:**
- `{GameIdea}` - Replaced with the generated game concept

#### LLM Provider Settings

Configure each provider in their respective sections:

```json
{
  "Ollama": {
    "Host": "localhost",
    "Port": 11434,
    "Model": "llama3"
  },
  "LmStudio": {
    "Host": "localhost",
    "Port": 1234,
    "Model": "your-model-name"
  },
  "OpenAI": {
    "ApiKey": "your-api-key",
    "Model": "gpt-4o-mini"
  }
}
```

#### Prolog Settings

```json
{
  "Prolog": {
    "ExecutablePath": "",
    "WorkingDirectory": ""
  }
}
```

Leave `ExecutablePath` empty to auto-detect SWI-Prolog installation.

## Security

The API includes several security measures:

1. **Input Validation**: All inputs are validated using the `IRequestValidator` from `LlmPlayground.Utilities`
2. **Input Sanitization**: User inputs are sanitized to remove potentially harmful content
3. **Prolog Code Safety**: Generated Prolog code is checked for dangerous predicates like `shell`, `system`, `exec`, etc.
4. **Temporary File Cleanup**: Prolog files are created in temp directories and cleaned up after execution

## Project Structure

```
LlmPlayground.Api/
├── Configuration/
│   ├── DefaultPrompts.cs        # Default prompt templates
│   └── GameGenerationSettings.cs # Configuration binding class
├── Controllers/
│   └── GameGeneratorController.cs # API controller
├── Helpers/
│   ├── PromptBuilder.cs         # Prompt template rendering
│   └── PrologCodeExtractor.cs   # Code extraction from LLM responses
├── Models/
│   ├── GameGenerationRequest.cs  # Request model
│   └── GameGenerationResponse.cs # Response model
├── Services/
│   ├── IGameGeneratorService.cs  # Service interface
│   └── GameGeneratorService.cs   # Service implementation
├── appsettings.json              # Main configuration
├── appsettings.Development.json  # Development settings
└── Program.cs                    # Application entry point
```

## Dependencies

This API uses the following LlmPlayground libraries:

- **LlmPlayground.Core**: LLM provider abstractions and implementations
- **LlmPlayground.Services**: Service layer for LLM and Prolog operations
- **LlmPlayground.Prolog**: Prolog execution functionality
- **LlmPlayground.PromptLab**: Prompt engineering utilities
- **LlmPlayground.Utilities**: Validation and sanitization utilities

## Testing

Run the unit tests:

```bash
cd Tests/LlmPlayground.Api.Tests
dotnet test
```

The tests cover:
- Controller input validation
- Service logic
- Helper utilities
- Error handling

## Examples

### Simple Game Generation

```bash
curl -X POST https://localhost:5001/api/gamegenerator/generate \
  -H "Content-Type: application/json" \
  -d '{"theme": "animals"}'
```

### Game with Custom Description

```bash
curl -X POST https://localhost:5001/api/gamegenerator/generate \
  -H "Content-Type: application/json" \
  -d '{
    "theme": "space",
    "description": "A game about classifying planets based on their properties"
  }'
```

### Generate Without Execution

```bash
curl -X POST https://localhost:5001/api/gamegenerator/generate \
  -H "Content-Type: application/json" \
  -d '{
    "theme": "chess",
    "executeGame": false
  }'
```

## Troubleshooting

### LLM Provider Not Responding

1. Ensure the LLM provider is running
2. Check the host and port configuration in `appsettings.json`
3. For OpenAI, verify your API key is set correctly

### Prolog Execution Fails

1. Verify SWI-Prolog is installed: `swipl --version`
2. Check the `Prolog:ExecutablePath` setting if auto-detection fails
3. Review the generated code for syntax errors

### Request Validation Errors

1. Theme and description have maximum lengths (200 and 1000 characters)
2. Provider must be one of: "Ollama", "LmStudio", "OpenAI"
3. Prolog goal cannot contain dangerous predicates

## License

This project is part of the LlmPlayground solution. See the root LICENSE file for details.

