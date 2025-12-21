# LlmPlayground.Prolog

A .NET 9 class library for executing Prolog files using SWI-Prolog. This library provides a simple interface to run Prolog programs, execute queries, and integrate Prolog logic into your .NET applications.

## Features

- **Run Prolog Files** - Execute `.pl` files with optional goals
- **Direct Query Execution** - Run Prolog queries without loading files
- **Interactive Sessions** - Start interactive Prolog sessions programmatically
- **Async/Await Support** - All operations are fully asynchronous
- **Cancellation Support** - Cancel long-running operations with `CancellationToken`
- **Prolog Availability Check** - Verify SWI-Prolog is installed before running

## Prerequisites

### SWI-Prolog Installation

This library requires [SWI-Prolog](https://www.swi-prolog.org/) to be installed and available in your system PATH.

#### Windows

**Option 1: Using winget (Recommended)**
```powershell
winget install --id SWI-Prolog.SWI-Prolog -e
```

**Option 2: Using Chocolatey**
```powershell
choco install swi-prolog
```

**Option 3: Manual Installation**
1. Download the installer from [SWI-Prolog Downloads](https://www.swi-prolog.org/Download.html)
2. Run the installer and follow the prompts
3. **Important**: During installation, ensure "Add swipl to PATH" is checked

#### macOS

**Using Homebrew**
```bash
brew install swi-prolog
```

#### Linux (Ubuntu/Debian)

**Using Snap**
```bash
sudo snap install swi-prolog
```

**Using APT**
```bash
sudo apt-add-repository ppa:swi-prolog/stable
sudo apt update
sudo apt install swi-prolog
```

### Verify Installation

After installation, verify SWI-Prolog is accessible:

```bash
swipl --version
```

Expected output:
```
SWI-Prolog version 9.x.x or 10.x.x
```

If you see "command not found", you may need to:
- **Windows**: Restart your terminal or add SWI-Prolog to PATH manually
- **macOS/Linux**: Restart your terminal or run `source ~/.bashrc`

## Installation

Add a reference to this project in your `.csproj`:

```xml
<ProjectReference Include="..\LlmPlayground.Prolog\LlmPlayground.Prolog.csproj" />
```

Or if published as a NuGet package:
```bash
dotnet add package LlmPlayground.Prolog
```

## Usage

### Basic Usage

```csharp
using LlmPlayground.Prolog;

// Create a runner (uses 'swipl' by default)
var runner = new PrologRunner();

// Check if Prolog is available
if (!await runner.IsPrologAvailableAsync())
{
    Console.WriteLine("SWI-Prolog is not installed!");
    return;
}

// Run a Prolog file
var result = await runner.RunFileAsync("path/to/program.pl", "main");

if (result.Success)
{
    Console.WriteLine(result.Output);
}
else
{
    Console.WriteLine($"Error: {result.Error}");
}
```

### Running a Prolog File with a Goal

```csharp
// Load file and execute a specific goal
var result = await runner.RunFileAsync("family.pl", "find_parents(john, X)");

Console.WriteLine($"Success: {result.Success}");
Console.WriteLine($"Output: {result.Output}");
Console.WriteLine($"Exit Code: {result.ExitCode}");
```

### Executing Direct Queries

```csharp
// Run a query without loading a file
var result = await runner.RunQueryAsync("X is 2 + 2, format('Result: ~w~n', [X])");

// Output: "Result: 4"
Console.WriteLine(result.Output);
```

### Using a Custom Prolog Path

```csharp
// Use a specific Prolog executable
var runner = new PrologRunner(@"C:\Program Files\swipl\bin\swipl.exe");

// Or on Linux/macOS
var runner = new PrologRunner("/usr/local/bin/swipl");
```

### Handling Cancellation

```csharp
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

try
{
    var result = await runner.RunFileAsync("long_computation.pl", "compute", cts.Token);
}
catch (OperationCanceledException)
{
    Console.WriteLine("Operation was cancelled");
}
```

### Starting an Interactive Session

```csharp
// Start an interactive Prolog session
using var process = runner.StartInteractiveSession("knowledge_base.pl");

// Write queries to stdin
await process.StandardInput.WriteLineAsync("parent(X, john).");

// Read output from stdout
var output = await process.StandardOutput.ReadLineAsync();
```

## API Reference

### PrologRunner Class

| Method | Description |
|--------|-------------|
| `RunFileAsync(filePath, goal?, cancellationToken?)` | Executes a Prolog file with an optional goal |
| `RunQueryAsync(query, cancellationToken?)` | Executes a Prolog query directly |
| `StartInteractiveSession(filePath?)` | Starts an interactive Prolog process |
| `IsPrologAvailableAsync()` | Checks if SWI-Prolog is installed and accessible |

### PrologResult Class

| Property | Type | Description |
|----------|------|-------------|
| `Success` | `bool` | Whether the execution completed successfully |
| `Output` | `string` | Standard output from the Prolog interpreter |
| `Error` | `string` | Error output or error message |
| `ExitCode` | `int` | Process exit code (0 = success) |

## Example Prolog Files

### Hello World (`hello.pl`)

```prolog
:- initialization(main).

main :-
    write('Hello, World!'), nl.
```

Run with:
```csharp
await runner.RunFileAsync("hello.pl");
```

### Arithmetic Operations (`arithmetic.pl`)

```prolog
factorial(0, 1) :- !.
factorial(N, F) :-
    N > 0,
    N1 is N - 1,
    factorial(N1, F1),
    F is N * F1.

run :-
    factorial(5, F),
    format('5! = ~w~n', [F]).
```

Run with:
```csharp
var result = await runner.RunFileAsync("arithmetic.pl", "run");
// Output: "5! = 120"
```

### Family Relationships (`family.pl`)

```prolog
parent(tom, mary).
parent(tom, john).
parent(mary, ann).

grandparent(X, Z) :- parent(X, Y), parent(Y, Z).

find_grandparents :-
    forall(grandparent(GP, GC), 
           format('~w is grandparent of ~w~n', [GP, GC])).
```

Run with:
```csharp
var result = await runner.RunFileAsync("family.pl", "find_grandparents");
// Output: "tom is grandparent of ann"
```

## Uninstalling SWI-Prolog

### Windows

**Using winget**
```powershell
winget uninstall --id SWI-Prolog.SWI-Prolog
```

**Using Chocolatey**
```powershell
choco uninstall swi-prolog
```

**Manual Uninstall**
1. Open Settings → Apps → Installed Apps
2. Search for "SWI-Prolog"
3. Click Uninstall

### macOS

```bash
brew uninstall swi-prolog
```

### Linux

**Using Snap**
```bash
sudo snap remove swi-prolog
```

**Using APT**
```bash
sudo apt remove swi-prolog
```

## Troubleshooting

### "swipl" is not recognized

**Windows**: 
- Restart your terminal after installation
- Or add `C:\Program Files\swipl\bin` to your PATH environment variable

**macOS/Linux**:
- Run `which swipl` to check if it's in PATH
- Try restarting your terminal

### Tests are skipped

Tests that require SWI-Prolog use `[SkippableFact]` and will be skipped if Prolog isn't available. Install SWI-Prolog and ensure `swipl` is in PATH to run all tests.

### Syntax errors in Prolog files

Check the `PrologResult.Error` property for detailed error messages from the Prolog interpreter. Common issues:
- Missing period at end of clause
- Unmatched parentheses
- Undefined predicates

## Additional Resources

- [SWI-Prolog Documentation](https://www.swi-prolog.org/pldoc/doc_for?object=manual)
- [Learn Prolog Now!](https://www.learnprolognow.org/) - Free online tutorial
- [SWI-Prolog Downloads](https://www.swi-prolog.org/Download.html)

## License

MIT License


