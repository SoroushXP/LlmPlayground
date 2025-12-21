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

                    if (executionResult.Success || string.IsNullOrWhiteSpace(executionResult.Error))
                    {
                        break;
                    }

                    if (attempt >= _settings.MaxFixRetries)
                    {
                        ConsoleStyles.Warning($"Prolog execution failed after {fixAttempts} fix attempts.");
                        break;
                    }

                    ConsoleStyles.Warning($"Execution failed, attempting to fix (attempt {attempt + 1}/{_settings.MaxFixRetries})...");

                    fixAttempts++;
                    var fixedCode = await FixPrologCodeAsync(currentCode, executionResult.Error, cancellationToken);

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
            Error = response.Error
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

