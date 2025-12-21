using System.Diagnostics;
using LlmPlayground.Console.Configuration;
using LlmPlayground.Console.Helpers;
using LlmPlayground.Console.Models;
using LlmPlayground.Services.Interfaces;
using LlmPlayground.Services.Models;
using LlmPlayground.Utilities.Validation;
using Microsoft.Extensions.Options;

namespace LlmPlayground.Console.Services;

/// <summary>
/// Service implementation for generating Prolog-based logic games using LLM providers.
/// </summary>
public sealed class GameGeneratorService : IGameGeneratorService
{
    private readonly ILlmService _llmService;
    private readonly IPrologService _prologService;
    private readonly IRequestValidator _requestValidator;
    private readonly GameGenerationSettings _settings;

    public GameGeneratorService(
        ILlmService llmService,
        IPrologService prologService,
        IRequestValidator requestValidator,
        IOptions<GameGenerationSettings> settings)
    {
        _llmService = llmService ?? throw new ArgumentNullException(nameof(llmService));
        _prologService = prologService ?? throw new ArgumentNullException(nameof(prologService));
        _requestValidator = requestValidator ?? throw new ArgumentNullException(nameof(requestValidator));
        _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
    }

    /// <inheritdoc />
    public async Task<GameGenerationResponse> GenerateGameAsync(
        GameGenerationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var totalStopwatch = Stopwatch.StartNew();
        var timings = new TimingCollector();

        try
        {
            var providerUsed = _llmService.CurrentProvider;

            // Step 1: Generate game idea
            ConsoleStyles.Muted("Generating game idea...");
            timings.StartPhase("GameIdea");
            
            var gameIdea = await GenerateGameIdeaAsync(request, cancellationToken);
            
            timings.EndPhase("GameIdea");

            // Step 2: Generate Prolog code from the idea
            ConsoleStyles.Muted("Generating Prolog code...");
            timings.StartPhase("PrologCode");
            
            var prologCode = await GeneratePrologCodeAsync(gameIdea, cancellationToken);
            
            timings.EndPhase("PrologCode");

            // Validate and sanitize the Prolog code
            var validationResult = _requestValidator.ValidatePrologQuery(prologCode);
            if (!validationResult.IsValid)
            {
                ConsoleStyles.Warning("Generated code contains potentially unsafe constructs, sanitizing...");
                prologCode = PrologCodeExtractor.SanitizePrologCode(prologCode);
            }

            // Step 3: Save the Prolog code to file
            string? executionOutput = null;
            bool? executionSuccess = null;
            string? executionError = null;
            int fixAttempts = 0;
            
            var generatedFilePath = await SavePrologCodeAsync(prologCode, cancellationToken);
            ConsoleStyles.Muted($"Generated Prolog file: {generatedFilePath}");

            // Step 4: Execute the Prolog code (if requested) with retry loop for fixing errors
            if (request.ExecuteGame)
            {
                timings.StartPhase("Execution");
                var currentCode = prologCode;
                var goal = request.PrologGoal ?? _settings.DefaultPrologGoal;

                for (int attempt = 0; attempt <= _settings.MaxFixRetries; attempt++)
                {
                    ConsoleStyles.Muted($"Executing Prolog code (attempt {attempt + 1})...");

                    var executionResult = await ExecutePrologCodeAsync(generatedFilePath, goal, cancellationToken);

                    executionOutput = executionResult.Output;
                    executionSuccess = executionResult.Success;
                    executionError = executionResult.Error;

                    // Validate output is meaningful - a proper game demo should have substantial output
                    var outputValidation = ValidateGameOutput(executionResult.Output);
                    if (!outputValidation.IsValid)
                    {
                        executionSuccess = false;
                        executionError = outputValidation.Error;
                    }

                    // If main/0 passed, also validate that solve(X) works
                    if (executionResult.Success && outputValidation.IsValid)
                    {
                        ConsoleStyles.Muted("Validating puzzle logic (testing solve(X))...");
                        var solveValidation = await ValidateSolvePredicateAsync(generatedFilePath, currentCode, cancellationToken);
                        if (!solveValidation.IsValid)
                        {
                            executionSuccess = false;
                            executionError = solveValidation.Error;
                            outputValidation = (false, solveValidation.Error);
                        }
                    }

                    // Only stop if execution was truly successful with meaningful output and valid solve/1
                    if (executionResult.Success && outputValidation.IsValid)
                    {
                        break;
                    }

                    if (attempt >= _settings.MaxFixRetries)
                    {
                        ConsoleStyles.Warning($"Prolog execution failed after {fixAttempts} fix attempts.");
                        break;
                    }

                    // Build error message for the LLM
                    string errorForFix;

                    if (!outputValidation.IsValid && executionResult.Success)
                    {
                        // Code ran but output was insufficient - tell LLM exactly what's wrong
                        errorForFix = outputValidation.Error!;
                        if (!string.IsNullOrWhiteSpace(executionResult.Output))
                        {
                            errorForFix += $"\n\nCurrent output produced:\n{executionResult.Output}";
                        }
                    }
                    else if (string.IsNullOrWhiteSpace(executionResult.Error))
                    {
                        // Prolog failed silently (main predicate returned false or exited with error code)
                        errorForFix = $"The Prolog code failed to execute successfully. Exit code: {executionResult.ExitCode}. " +
                                      "The main/0 predicate likely failed (returned false) due to logical errors in the rules or facts. " +
                                      "Check that all predicates are correctly defined, all variables are properly bound, and the logical constraints are satisfiable.";
                        if (!string.IsNullOrWhiteSpace(executionResult.Output))
                        {
                            errorForFix += $"\n\nPartial output before failure:\n{executionResult.Output}";
                        }
                    }
                    else
                    {
                        // Actual error from Prolog
                        errorForFix = executionResult.Error;
                    }

                    // Update executionError for display purposes
                    executionError = errorForFix;

                    ConsoleStyles.Warning($"Execution failed, attempting to fix (attempt {attempt + 1}/{_settings.MaxFixRetries})...");

                    fixAttempts++;
                    var fixedCode = await FixPrologCodeAsync(currentCode, errorForFix, cancellationToken);

                    if (fixedCode == currentCode)
                    {
                        ConsoleStyles.Warning("LLM returned the same code, stopping fix attempts.");
                        break;
                    }

                    currentCode = fixedCode;
                    prologCode = fixedCode;

                    generatedFilePath = await SavePrologCodeAsync(fixedCode, cancellationToken);
                    ConsoleStyles.Muted($"Saved fixed Prolog file: {generatedFilePath}");
                }

                timings.EndPhase("Execution");
            }

            totalStopwatch.Stop();

            return new GameGenerationResponse
            {
                Success = true,
                GameIdea = gameIdea,
                PrologCode = prologCode,
                ExecutionOutput = executionOutput,
                ExecutionSuccess = executionSuccess,
                ExecutionError = executionError,
                GeneratedFilePath = generatedFilePath,
                FixAttempts = fixAttempts,
                ProviderUsed = providerUsed,
                Duration = totalStopwatch.Elapsed,
                Timings = new GenerationTimings
                {
                    GameIdeaGeneration = timings.GetDuration("GameIdea"),
                    PrologCodeGeneration = timings.GetDuration("PrologCode"),
                    PrologExecution = request.ExecuteGame ? timings.GetDuration("Execution") : null
                }
            };
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            totalStopwatch.Stop();
            return new GameGenerationResponse
            {
                Success = false,
                Error = ex.Message,
                Duration = totalStopwatch.Elapsed
            };
        }
    }

    private async Task<string> GenerateGameIdeaAsync(
        GameGenerationRequest request,
        CancellationToken cancellationToken)
    {
        var userPrompt = PromptBuilder.BuildGameIdeaPrompt(
            _settings.GameIdeaUserPromptTemplate,
            request.Theme,
            request.Description);

        var chatRequest = new ChatRequest
        {
            Messages =
            [
                new ChatMessageDto { Role = "system", Content = _settings.GameIdeaSystemPrompt },
                new ChatMessageDto { Role = "user", Content = userPrompt }
            ],
            Options = new InferenceOptionsDto
            {
                MaxTokens = _settings.DefaultGameIdeaMaxTokens,
                Temperature = _settings.DefaultGameIdeaTemperature
            }
        };

        var response = await _llmService.ChatAsync(chatRequest, cancellationToken);
        return response.Text;
    }

    private async Task<string> GeneratePrologCodeAsync(
        string gameIdea,
        CancellationToken cancellationToken)
    {
        var userPrompt = PromptBuilder.BuildPrologCodePrompt(
            _settings.PrologCodeUserPromptTemplate,
            gameIdea);

        var chatRequest = new ChatRequest
        {
            Messages =
            [
                new ChatMessageDto { Role = "system", Content = _settings.PrologCodeSystemPrompt },
                new ChatMessageDto { Role = "user", Content = userPrompt }
            ],
            Options = new InferenceOptionsDto
            {
                MaxTokens = _settings.DefaultPrologCodeMaxTokens,
                Temperature = _settings.DefaultPrologCodeTemperature
            }
        };

        var response = await _llmService.ChatAsync(chatRequest, cancellationToken);
        return PrologCodeExtractor.ExtractPrologCode(response.Text);
    }

    private async Task<string> FixPrologCodeAsync(
        string prologCode,
        string errors,
        CancellationToken cancellationToken)
    {
        var userPrompt = PromptBuilder.BuildPrologFixPrompt(
            _settings.PrologFixUserPromptTemplate,
            prologCode,
            errors);

        var chatRequest = new ChatRequest
        {
            Messages =
            [
                new ChatMessageDto { Role = "system", Content = _settings.PrologFixSystemPrompt },
                new ChatMessageDto { Role = "user", Content = userPrompt }
            ],
            Options = new InferenceOptionsDto
            {
                MaxTokens = _settings.DefaultPrologCodeMaxTokens,
                Temperature = 0.3f
            }
        };

        var response = await _llmService.ChatAsync(chatRequest, cancellationToken);
        return PrologCodeExtractor.ExtractPrologCode(response.Text);
    }

    private async Task<string> SavePrologCodeAsync(
        string prologCode,
        CancellationToken cancellationToken)
    {
        var prologFile = GetPrologFilePath();

        var directory = Path.GetDirectoryName(prologFile);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await File.WriteAllTextAsync(prologFile, prologCode, cancellationToken);
        return prologFile;
    }

    private async Task<PrologExecutionResult> ExecutePrologCodeAsync(
        string prologFilePath,
        string goal,
        CancellationToken cancellationToken)
    {
        var request = new PrologFileRequest
        {
            FilePath = prologFilePath,
            Goal = goal
        };

        var response = await _prologService.ExecuteFileAsync(request, cancellationToken);

        return new PrologExecutionResult
        {
            Success = response.Success,
            Output = response.Output,
            Error = response.Error,
            ExitCode = response.ExitCode
        };
    }

    private string GetPrologFilePath()
    {
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var fileName = $"game_{timestamp}.pl";

        if (!string.IsNullOrWhiteSpace(_settings.PrologOutputDirectory))
        {
            var outputDir = _settings.PrologOutputDirectory;

            if (!Path.IsPathRooted(outputDir))
            {
                outputDir = Path.Combine(AppContext.BaseDirectory, outputDir);
            }

            return Path.Combine(outputDir, fileName);
        }

        return Path.Combine(Path.GetTempPath(), fileName);
    }

    private sealed record PrologExecutionResult
    {
        public bool Success { get; init; }
        public string Output { get; init; } = string.Empty;
        public string? Error { get; init; }
        public int ExitCode { get; init; }
    }

    private static (bool IsValid, string? Error) ValidateGameOutput(string output)
    {
        if (string.IsNullOrWhiteSpace(output))
        {
            return (false, "The main/0 predicate produced no output. It should demonstrate the game by running queries and displaying results using write/1 and nl/0.");
        }

        // Count meaningful lines (non-empty, non-whitespace-only)
        var lines = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
                          .Where(line => !string.IsNullOrWhiteSpace(line))
                          .ToList();

        // A proper game demonstration should have at least 5 lines of output
        // (title, some facts/rules demonstration, query results, etc.)
        const int MinimumLines = 5;
        if (lines.Count < MinimumLines)
        {
            return (false, $"The main/0 predicate only produced {lines.Count} line(s) of output. A proper game demonstration should show the game logic in action with example queries, their results, and explanations. It needs at least {MinimumLines} lines demonstrating facts, rules, and query results.");
        }

        // Check if output appears to show actual query/logic demonstration
        // Look for patterns that suggest actual game logic is being demonstrated
        var hasQueryResults = lines.Any(line =>
            line.Contains("=") ||           // Prolog unification results
            line.Contains("true") ||         // Boolean results
            line.Contains("false") ||
            line.Contains("yes") ||
            line.Contains("no") ||
            line.Contains(":") ||            // Relationships like "X : Y" or labels
            line.Contains("->") ||           // Implications
            line.Contains("is in") ||        // Common game output
            line.Contains("located") ||
            line.Contains("found") ||
            line.Contains("solution") ||
            line.Contains("answer") ||
            line.Contains("result"));

        if (!hasQueryResults)
        {
            return (false, "The main/0 predicate output doesn't appear to demonstrate actual game logic. It should run example queries showing relationships, deductions, or solutions - not just print static messages.");
        }

        return (true, null);
    }

    private async Task<(bool IsValid, string? Error)> ValidateSolvePredicateAsync(
        string prologFilePath,
        string prologCode,
        CancellationToken cancellationToken)
    {
        // Check if solve/2 exists instead of solve/1 - this is a common mistake
        var hasSolve2 = System.Text.RegularExpressions.Regex.IsMatch(prologCode, @"solve\s*\(\s*\w+\s*,\s*\w+\s*\)");
        var hasSolve1 = System.Text.RegularExpressions.Regex.IsMatch(prologCode, @"solve\s*\(\s*\w+\s*\)\s*:-");

        if (hasSolve2 && !hasSolve1)
        {
            return (false, "You defined solve/2 (two arguments) but the game requires solve/1 (one argument). " +
                "The solve/1 predicate must return just ONE answer - the culprit/solution. " +
                "Example: solve(X) :- suspect(X), was_at_scene(X), has_motive(X). " +
                "If you need to return multiple values, use solve(answer(Suspect, Item)) or just solve(Suspect).");
        }

        // Run solve(X) and capture the result
        var solveResult = await ExecutePrologCodeAsync(prologFilePath, "solve(X), format('SOLUTION:~w~n', [X])", cancellationToken);

        if (!solveResult.Success)
        {
            var errorMsg = solveResult.Error ?? "";
            if (errorMsg.Contains("solve/2"))
            {
                return (false, "You defined solve/2 (two arguments) but the game requires solve/1 (one argument). " +
                    "Change solve(X, Y) to solve(X) where X is the single answer. " +
                    "Example: solve(X) :- suspect(X), was_at_scene(X), has_motive(X).");
            }

            // Provide detailed diagnosis for silent failures
            var diagnosis = BuildSolveDiagnosis(prologCode, errorMsg);
            return (false, diagnosis);
        }

        // Check if solve(X) returned a result
        if (string.IsNullOrWhiteSpace(solveResult.Output) || !solveResult.Output.Contains("SOLUTION:"))
        {
            var diagnosis = BuildSolveDiagnosis(prologCode, "solve(X) returned false (no solution found)");
            return (false, diagnosis);
        }

        // Extract the solution
        var solutionMatch = System.Text.RegularExpressions.Regex.Match(solveResult.Output, @"SOLUTION:(\w+)");
        if (!solutionMatch.Success)
        {
            return (false, "The solve(X) predicate returned an invalid answer format. Expected an atom like 'frank' or 'bob'.");
        }

        var solution = solutionMatch.Groups[1].Value;

        // Check if solve/1 is hardcoded (just returns a fact without deduction)
        // Look for patterns like "solve(frank)." or "solve(X) :- X = frank."
        var hardcodedPatterns = new[]
        {
            $@"solve\s*\(\s*{solution}\s*\)\s*\.",  // solve(frank).
            $@"solve\s*\(\s*_?\w*\s*\)\s*:-\s*_?\w*\s*=\s*{solution}\s*\.",  // solve(X) :- X = frank.
            $@"solve\s*\(\s*X\s*\)\s*:-\s*solution\s*\(\s*X\s*\)\s*\."  // solve(X) :- solution(X). with solution(frank).
        };

        var hasHardcodedSolve = hardcodedPatterns.Any(pattern =>
            System.Text.RegularExpressions.Regex.IsMatch(prologCode, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase));

        // Also check if there's a simple "solution(answer)." fact that solve/1 just returns
        var hasSolutionFact = System.Text.RegularExpressions.Regex.IsMatch(prologCode, $@"solution\s*\(\s*{solution}\s*\)\s*\.");
        var solveJustCallsSolution = System.Text.RegularExpressions.Regex.IsMatch(prologCode, @"solve\s*\(\s*X\s*\)\s*:-\s*solution\s*\(\s*X\s*\)\s*\.");

        if (hasSolutionFact && solveJustCallsSolution)
        {
            return (false, $"The solve(X) predicate just returns a hardcoded answer from solution({solution}). It must DEDUCE the answer using logical rules that query the puzzle facts. Example: solve(X) :- suspect(X), was_at_scene(X), has_motive(X), \\+ has_alibi(X).");
        }

        if (hasHardcodedSolve)
        {
            return (false, $"The solve(X) predicate appears to be hardcoded to return '{solution}' without logical deduction. Rewrite solve/1 to deduce the answer from the clues using Prolog rules.");
        }

        return (true, null);
    }

    /// <summary>
    /// Builds a detailed diagnosis message when solve/1 fails, helping the LLM understand exactly what went wrong.
    /// </summary>
    private static string BuildSolveDiagnosis(string prologCode, string originalError)
    {
        var diagnosis = new System.Text.StringBuilder();
        diagnosis.AppendLine("*** solve(X) FAILED - DETAILED DIAGNOSIS ***");
        diagnosis.AppendLine();

        // Try to find the solve/1 definition
        var solveMatch = System.Text.RegularExpressions.Regex.Match(
            prologCode,
            @"solve\s*\(\s*\w+\s*\)\s*:-\s*([^.]+)\.",
            System.Text.RegularExpressions.RegexOptions.Singleline);

        if (solveMatch.Success)
        {
            var solveBody = solveMatch.Groups[1].Value.Trim();
            diagnosis.AppendLine($"Your solve/1 definition: solve(X) :- {solveBody}.");
            diagnosis.AppendLine();

            // Extract predicates called by solve/1
            var predicateCalls = System.Text.RegularExpressions.Regex.Matches(
                solveBody,
                @"(\w+)\s*\([^)]+\)");

            if (predicateCalls.Count > 0)
            {
                diagnosis.AppendLine("Predicates called by solve/1:");
                foreach (System.Text.RegularExpressions.Match call in predicateCalls)
                {
                    var predCall = call.Value;
                    diagnosis.AppendLine($"  - {predCall}");

                    // Check if there are any matching facts for this predicate
                    var predName = call.Groups[1].Value;
                    var factPattern = $@"^{predName}\s*\([^)]+\)\s*\.";
                    var hasFacts = System.Text.RegularExpressions.Regex.IsMatch(
                        prologCode,
                        factPattern,
                        System.Text.RegularExpressions.RegexOptions.Multiline);

                    if (!hasFacts)
                    {
                        diagnosis.AppendLine($"    ^^^ WARNING: No facts found for {predName}/N!");
                    }
                }
                diagnosis.AppendLine();
            }
        }
        else
        {
            diagnosis.AppendLine("Could not find solve/1 definition in the code!");
            diagnosis.AppendLine("Make sure you have: solve(X) :- <body>.");
            diagnosis.AppendLine();
        }

        diagnosis.AppendLine("MOST LIKELY PROBLEM:");
        diagnosis.AppendLine("solve/1 queries predicates that have NO MATCHING FACTS.");
        diagnosis.AppendLine();
        diagnosis.AppendLine("Example of this bug:");
        diagnosis.AppendLine("  % Facts defined:");
        diagnosis.AppendLine("  at(alice, kitchen, noon).");
        diagnosis.AppendLine("  at(bob, garden, evening).");
        diagnosis.AppendLine("  % solve/1 requires:");
        diagnosis.AppendLine("  solve(X) :- at(X, gallery, morning).  % FAILS! No one matches!");
        diagnosis.AppendLine();
        diagnosis.AppendLine("HOW TO FIX:");
        diagnosis.AppendLine("Option A: Add the missing fact that makes one suspect match:");
        diagnosis.AppendLine("  at(carol, gallery, morning).  % Now solve(X) returns carol");
        diagnosis.AppendLine();
        diagnosis.AppendLine("Option B (RECOMMENDED): Use elimination pattern:");
        diagnosis.AppendLine("  suspect(alice). suspect(bob). suspect(carol).");
        diagnosis.AppendLine("  has_alibi(alice).  % Alice was elsewhere");
        diagnosis.AppendLine("  has_alibi(bob).    % Bob was at party");
        diagnosis.AppendLine("  % Carol has NO alibi - she's guilty!");
        diagnosis.AppendLine("  solve(X) :- suspect(X), \\+ has_alibi(X).  % Returns carol");
        diagnosis.AppendLine();

        if (!string.IsNullOrWhiteSpace(originalError))
        {
            diagnosis.AppendLine($"Original error: {originalError}");
        }

        return diagnosis.ToString();
    }

    private sealed class TimingCollector
    {
        private readonly Dictionary<string, Stopwatch> _stopwatches = new();

        public void StartPhase(string name)
        {
            _stopwatches[name] = Stopwatch.StartNew();
        }

        public void EndPhase(string name)
        {
            if (_stopwatches.TryGetValue(name, out var sw))
            {
                sw.Stop();
            }
        }

        public TimeSpan GetDuration(string name)
        {
            return _stopwatches.TryGetValue(name, out var sw)
                ? sw.Elapsed
                : TimeSpan.Zero;
        }
    }
}

