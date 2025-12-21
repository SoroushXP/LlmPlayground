# LlmPlayground.Services.Tests

Unit tests for the LlmPlayground.Services project.

## Test Coverage

### LlmServiceTests
- Constructor validation (null checks)
- Provider listing and defaults
- Model management
- Disposed state handling

### PrologServiceTests
- Constructor validation
- Query execution validation
- File execution validation
- Syntax validation
- Availability checking

### PromptLabServiceTests
- Constructor validation
- Session management (create, get, close)
- Prompt sending validation
- Template rendering
- Variable extraction
- Disposed state handling

### ModelsTests
- DTO creation and defaults
- Calculated properties (TokensPerSecond)
- Enum values

### ServiceCollectionExtensionsTests
- Service registration
- Singleton lifetime verification
- Fluent API (method chaining)

## Running Tests

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test class
dotnet test --filter "FullyQualifiedName~LlmServiceTests"
```

## Test Dependencies

- **xUnit** - Test framework
- **FluentAssertions** - Fluent assertion library
- **NSubstitute** - Mocking framework
- **Microsoft.NET.Test.Sdk** - Test infrastructure

