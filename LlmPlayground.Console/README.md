# LlmPlayground.Console

Interactive command-line application for chatting with LLM providers and generating Prolog-based logic games.

## Overview

This console application provides an interactive chat interface that connects to various LLM backends (Ollama, LM Studio, OpenAI, or local GGUF models). It features conversation history, streaming responses, configurable inference settings, and a unique game generation feature that creates Prolog logic games using AI.

## Features

- **Multi-Provider Support** - Connect to Ollama, LM Studio, OpenAI, or local GGUF models
- **Interactive Chat** - Full conversation history with streaming or batch responses
- **Prolog Game Generation** - AI-powered generation of Prolog logic games
- **Customizable Theming** - Configurable console colors and emoji support
- **Persistent Preferences** - User settings are saved between sessions
- **Single Prompt Mode** - Execute one query and exit with `--prompt`
- **Cancellation Support** - Press Ctrl+C to cancel any running generation

## Running the Application

```bash
# Interactive mode
dotnet run --project LlmPlayground.Console

# Single prompt mode
dotnet run --project LlmPlayground.Console -- --prompt "Explain async/await"

# Silent mode (skip interactive model selection)
dotnet run --project LlmPlayground.Console -- --silent
```

## Interactive Commands

| Command | Description |
|---------|-------------|
| `help` | Show available commands |
| `exit` | Exit the application |
| `stream` | Toggle streaming mode on/off |
| `settings` | Configure inference parameters |
| `reset` | Reset all saved preferences |
| `clear` | Clear conversation history |
| `history` | Show conversation history |
| `game` | Generate a Prolog-based logic game |

## Configuration

Configure the application via `appsettings.json`:

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
    "MaxTokens": 24500,
    "Temperature": 0.7,
    "TopP": 0.9,
    "RepeatPenalty": 1.1
  },
  "Console": {
    "Theme": "Vibrant",
    "ShowEmoji": true
  },
  "GameGeneration": {
    "MaxFixRetries": 10,
    "PrologOutputDirectory": "GeneratedGames"
  }
}
```

### Provider Configuration

Set `"Provider"` to one of: `"Ollama"`, `"LmStudio"`, `"OpenAI"`, or `"Local"`.

### Console Theming

Customize colors in the `"Console"` section:
- `Theme` - `"Vibrant"` or `"Minimal"`
- `ShowEmoji` - Enable/disable emoji in output
- Individual color settings for titles, responses, prompts, etc.

## Game Generation

The `game` command generates **solvable logic puzzle games** using AI:

1. Enter an optional theme (e.g., "pirate treasure", "detective mystery")
2. Provide additional requirements (optional)
3. Choose whether to execute the generated game
4. The AI generates a puzzle concept with clues and a hidden solution
5. Self-healing: If execution fails, the AI automatically fixes the code (up to 10 retries)
6. After successful execution, you can play interactively in the Prolog interpreter

### Playing the Game

Once a game is generated and validated, you'll be dropped into an interactive Prolog session:

```prolog
?- show_clues.     % Display all puzzle clues
?- hint(1).        % Get hint #1
?- hint(2).        % Get hint #2
?- solve(X).       % Find the answer (spoiler!)
?- check_answer(butler).  % Verify your guess
?- halt.           % Exit the game
```

### Puzzle Structure

Generated puzzles include:
- **A hidden secret** - Who did it? Where is the treasure? Which box has the prize?
- **Clues** - Logical constraints that lead to exactly one solution
- **solve/1** - Predicate that deduces the answer from clues
- **check_answer/1** - Verify your guess without spoilers
- **hint/1** - Progressive hints if you're stuck

Generated games are saved to the `GeneratedGames` directory.

## Project Structure

```
LlmPlayground.Console/
├── Program.cs                  # Application entry point
├── InteractiveSession.cs       # Chat loop and command handling
├── CommandLineOptions.cs       # CLI argument parsing
├── ConsoleStyles.cs            # Colored output formatting
├── ConsoleTheme.cs             # Theme configuration
├── ModelSelector.cs            # Interactive model selection
├── ServiceInitializer.cs       # Provider initialization
├── UserPreferences.cs          # Persistent user settings
├── Configuration/
│   ├── DefaultPrompts.cs       # Game generation prompts
│   └── GameGenerationSettings.cs
├── Helpers/
│   ├── PrologCodeExtractor.cs  # Extract Prolog from LLM output
│   └── PromptBuilder.cs        # Build prompts for game generation
├── Models/
│   └── GameGenerationModels.cs # DTOs for game generation
├── Services/
│   ├── IGameGeneratorService.cs
│   └── GameGeneratorService.cs # Game generation orchestration
└── appsettings.json            # Default configuration
```

## Dependencies

### Project References
- `LlmPlayground.Core` - LLM provider implementations
- `LlmPlayground.Services` - Service layer abstractions (includes Prolog execution via transitive reference)
- `LlmPlayground.Utilities` - Input validation and logging

### NuGet Packages
- `Microsoft.Extensions.Configuration.Binder`
- `Microsoft.Extensions.Configuration.Json`
- `Microsoft.Extensions.DependencyInjection`
- `Microsoft.Extensions.Logging`
- `Microsoft.Extensions.Logging.Console`

