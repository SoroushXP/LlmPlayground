using System.Diagnostics;
using LlmPlayground.Api.Configuration;
using LlmPlayground.Api.Helpers;
using LlmPlayground.Api.Models;
using LlmPlayground.Core;
using LlmPlayground.Services.Interfaces;
using LlmPlayground.Services.Models;
using LlmPlayground.Utilities.Validation;
using Microsoft.Extensions.Options;

namespace LlmPlayground.Api.Services;

/// <summary>
/// Service implementation for generating Prolog-based logic games using LLM providers.
/// </summary>
public sealed class GameGeneratorService : IGameGeneratorService
{
    private readonly ILlmService _llmService;
    private readonly IPrologService _prologService;
    private readonly IRequestValidator _requestValidator;
    private readonly GameGenerationSettings _settings;
    private readonly ILogger<GameGeneratorService> _logger;

    public GameGeneratorService(
        ILlmService llmService,
        IPrologService prologService,
        IRequestValidator requestValidator,
        IOptions<GameGenerationSettings> settings,
        ILogger<GameGeneratorService> logger)
    {
        _llmService = llmService ?? throw new ArgumentNullException(nameof(llmService));
        _prologService = prologService ?? throw new ArgumentNullException(nameof(prologService));
        _requestValidator = requestValidator ?? throw new ArgumentNullException(nameof(requestValidator));
        _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
            // Step 1: Set up the LLM provider
            var providerType = DetermineProvider(request.Provider);
            await _llmService.SetProviderAsync(providerType, cancellationToken);
            var providerUsed = _llmService.CurrentProvider;

            _logger.LogInformation(
                "Starting game generation with provider {Provider}",
                providerUsed);

            // Step 2: Generate game idea
            _logger.LogDebug("Generating game idea...");
            timings.StartPhase("GameIdea");
            
            var gameIdea = await GenerateGameIdeaAsync(request, cancellationToken);
            
            timings.EndPhase("GameIdea");
            _logger.LogDebug("Game idea generated successfully");

            // Step 3: Generate Prolog code from the idea
            _logger.LogDebug("Generating Prolog code...");
            timings.StartPhase("PrologCode");
            
            var prologCode = await GeneratePrologCodeAsync(gameIdea, cancellationToken);
            
            timings.EndPhase("PrologCode");
            _logger.LogDebug("Prolog code generated successfully");

            // Validate and sanitize the Prolog code
            var validationResult = _requestValidator.ValidatePrologQuery(prologCode);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning(
                    "Generated Prolog code contains potentially unsafe constructs: {Errors}",
                    validationResult.GetErrorSummary());

                // Sanitize the code by commenting out unsafe predicates
                prologCode = PrologCodeExtractor.SanitizePrologCode(prologCode);
            }

            // Step 4: Save the Prolog code to file
            string? executionOutput = null;
            bool? executionSuccess = null;
            string? executionError = null;
            int fixAttempts = 0;
            
            var generatedFilePath = await SavePrologCodeAsync(prologCode, cancellationToken);
            _logger.LogInformation("Generated Prolog file: {Path}", generatedFilePath);

            // Step 5: Execute the Prolog code (if requested) with retry loop for fixing errors
            if (request.ExecuteGame)
            {
                timings.StartPhase("Execution");
                var currentCode = prologCode;
                var goal = request.PrologGoal ?? _settings.DefaultPrologGoal;

                for (int attempt = 0; attempt <= _settings.MaxFixRetries; attempt++)
                {
                    _logger.LogDebug("Executing Prolog code (attempt {Attempt})...", attempt + 1);

                    var executionResult = await ExecutePrologCodeAsync(generatedFilePath, goal, cancellationToken);

                    executionOutput = executionResult.Output;
                    executionSuccess = executionResult.Success;
                    executionError = executionResult.Error;

                    // If successful or no error to fix, we're done
                    if (executionResult.Success || string.IsNullOrWhiteSpace(executionResult.Error))
                    {
                        _logger.LogDebug("Prolog execution succeeded on attempt {Attempt}", attempt + 1);
                        break;
                    }

                    // If we've exhausted retries, stop
                    if (attempt >= _settings.MaxFixRetries)
                    {
                        _logger.LogWarning(
                            "Prolog execution failed after {Attempts} fix attempts. Last error: {Error}",
                            fixAttempts, executionResult.Error);
                        break;
                    }

                    // Try to fix the code using LLM
                    _logger.LogInformation(
                        "Prolog execution failed, attempting to fix (attempt {Attempt}/{Max}). Error: {Error}",
                        attempt + 1, _settings.MaxFixRetries, executionResult.Error);

                    fixAttempts++;
                    var fixedCode = await FixPrologCodeAsync(currentCode, executionResult.Error, cancellationToken);

                    if (fixedCode == currentCode)
                    {
                        _logger.LogWarning("LLM returned the same code, stopping fix attempts");
                        break;
                    }

                    currentCode = fixedCode;
                    prologCode = fixedCode;

                    // Save the fixed code to file
                    generatedFilePath = await SavePrologCodeAsync(fixedCode, cancellationToken);
                    _logger.LogInformation("Saved fixed Prolog file: {Path}", generatedFilePath);
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
            _logger.LogError(ex, "Game generation failed");

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
            request);

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

        // Extract the Prolog code from the response (may be in markdown code blocks)
        return PrologCodeExtractor.ExtractPrologCode(response.Text);
    }

    private async Task<string> FixPrologCodeAsync(
        string prologCode,
        string errors,
        CancellationToken cancellationToken)
    {
        var userPrompt = _settings.PrologFixUserPromptTemplate
            .Replace("{PrologCode}", prologCode)
            .Replace("{Errors}", errors);

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
                Temperature = 0.3f // Lower temperature for more deterministic fixes
            }
        };

        var response = await _llmService.ChatAsync(chatRequest, cancellationToken);

        // Extract the Prolog code from the response
        return PrologCodeExtractor.ExtractPrologCode(response.Text);
    }

    private async Task<string> SavePrologCodeAsync(
        string prologCode,
        CancellationToken cancellationToken)
    {
        var prologFile = GetPrologFilePath();

        // Ensure directory exists
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
            
            // If relative path, make it relative to the current directory
            if (!Path.IsPathRooted(outputDir))
            {
                outputDir = Path.Combine(AppContext.BaseDirectory, outputDir);
            }

            return Path.Combine(outputDir, fileName);
        }

        // Fall back to temp directory
        return Path.Combine(Path.GetTempPath(), fileName);
    }

    private LlmProviderType DetermineProvider(string? requestedProvider)
    {
        var providerString = requestedProvider ?? _settings.DefaultProviderString;

        if (Enum.TryParse<LlmProviderType>(providerString, ignoreCase: true, out var providerType))
        {
            return providerType;
        }

        _logger.LogWarning(
            "Unknown provider '{Provider}', falling back to Ollama",
            providerString);

        return LlmProviderType.Ollama;
    }

    /// <summary>
    /// Helper record for Prolog execution results.
    /// </summary>
    private sealed record PrologExecutionResult
    {
        public bool Success { get; init; }
        public string Output { get; init; } = string.Empty;
        public string? Error { get; init; }
    }

    /// <summary>
    /// Helper class for collecting timing information.
    /// </summary>
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

