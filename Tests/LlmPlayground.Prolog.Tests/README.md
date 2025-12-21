# LlmPlayground.Prolog.Tests

Unit tests for the `LlmPlayground.Prolog` library.

## Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [SWI-Prolog](https://www.swi-prolog.org/) (optional, but required for integration tests)

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

## Running Tests

### Run All Tests

```bash
cd Tests/LlmPlayground.Prolog.Tests
dotnet test
```

### Run with Verbose Output

```bash
dotnet test --verbosity normal
```

### Run Specific Test Class

```bash
dotnet test --filter "FullyQualifiedName~PrologRunnerTests"
```

### Run Specific Test

```bash
dotnet test --filter "FullyQualifiedName~RunFileAsync_WithHelloWorld_ShouldProduceExpectedOutput"
```

## Test Structure

### Test Files

Sample Prolog files are located in `TestFiles/`:

| File | Description |
|------|-------------|
| `hello.pl` | Simple hello world program |
| `arithmetic.pl` | Factorial and list sum calculations |
| `family.pl` | Family relationship rules and queries |
| `syntax_error.pl` | Intentionally broken file for error handling tests |

### Test Classes

| Class | Description |
|-------|-------------|
| `PrologRunnerTests` | Tests for `PrologRunner` execution methods |
| `PrologResultTests` | Tests for `PrologResult` data class |

## Skippable Tests

Tests that require SWI-Prolog are marked with `[SkippableFact]`. These tests will:
- **Run** if SWI-Prolog is installed and in PATH
- **Skip** (not fail) if SWI-Prolog is unavailable

This allows CI/CD pipelines to pass even without Prolog installed.

### Example Output (Prolog not installed)

```
Passed:  PrologRunnerTests.Constructor_WithDefaultPath_ShouldUseSwipl
Passed:  PrologRunnerTests.RunFileAsync_WithNonExistentFile_ShouldReturnFailure
Passed:  PrologResultTests.DefaultValues_ShouldBeSetCorrectly
Skipped: PrologRunnerTests.RunFileAsync_WithHelloWorld_ShouldProduceExpectedOutput
         SWI-Prolog is not installed
```

### Example Output (Prolog installed)

```
Passed:  PrologRunnerTests.Constructor_WithDefaultPath_ShouldUseSwipl
Passed:  PrologRunnerTests.RunFileAsync_WithNonExistentFile_ShouldReturnFailure
Passed:  PrologRunnerTests.RunFileAsync_WithHelloWorld_ShouldProduceExpectedOutput
Passed:  PrologRunnerTests.RunFileAsync_WithArithmetic_ShouldCalculateCorrectly
Passed:  PrologResultTests.DefaultValues_ShouldBeSetCorrectly
```

## Test Coverage

| Area | Coverage |
|------|----------|
| File execution | ✅ Success, failure, syntax errors |
| Query execution | ✅ Simple queries, arithmetic |
| Error handling | ✅ Missing files, invalid paths |
| Cancellation | ✅ CancellationToken support |
| Result parsing | ✅ All PrologResult properties |

## Adding New Tests

1. Create test Prolog files in `TestFiles/`
2. Add test methods using `[SkippableFact]` for Prolog-dependent tests
3. Use `Skip.IfNot(await _runner.IsPrologAvailableAsync(), "message")` to skip gracefully

Example:

```csharp
[SkippableFact]
public async Task MyNewTest_ShouldWork()
{
    Skip.IfNot(await _runner.IsPrologAvailableAsync(), "SWI-Prolog is not installed");
    
    var result = await _runner.RunFileAsync(
        Path.Combine(_testFilesPath, "my_test.pl"), 
        "my_goal");
    
    result.Success.Should().BeTrue();
    result.Output.Should().Contain("expected output");
}
```

## Troubleshooting

### All integration tests are skipped

Ensure SWI-Prolog is installed and `swipl` is in your PATH:

```bash
swipl --version
```

### Test files not found

Test files are copied to the output directory. Ensure your `.csproj` includes:

```xml
<ItemGroup>
  <None Update="TestFiles\**\*">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </None>
</ItemGroup>
```

### Tests fail on CI/CD

If your CI environment doesn't have SWI-Prolog, tests will skip automatically. To install Prolog in CI:

**GitHub Actions (Ubuntu)**
```yaml
- name: Install SWI-Prolog
  run: sudo snap install swi-prolog
```

**GitHub Actions (Windows)**
```yaml
- name: Install SWI-Prolog
  run: winget install --id SWI-Prolog.SWI-Prolog -e --accept-source-agreements --accept-package-agreements
```


