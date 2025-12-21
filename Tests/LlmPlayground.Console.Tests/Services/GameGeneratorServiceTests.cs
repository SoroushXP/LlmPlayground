using FluentAssertions;
using LlmPlayground.Console.Configuration;
using LlmPlayground.Console.Models;
using LlmPlayground.Console.Services;
using LlmPlayground.Services.Interfaces;
using LlmPlayground.Services.Models;
using LlmPlayground.Utilities.Validation;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace LlmPlayground.Console.Tests.Services;

public class GameGeneratorServiceTests
{
    private readonly ILlmService _llmService;
    private readonly IPrologService _prologService;
    private readonly IRequestValidator _requestValidator;
    private readonly IOptions<GameGenerationSettings> _settings;
    private readonly GameGeneratorService _sut;

    public GameGeneratorServiceTests()
    {
        _llmService = Substitute.For<ILlmService>();
        _prologService = Substitute.For<IPrologService>();
        _requestValidator = Substitute.For<IRequestValidator>();

        var settings = new GameGenerationSettings
        {
            GameIdeaSystemPrompt = "Test system prompt",
            GameIdeaUserPromptTemplate = "Generate game {ThemeSection} {DescriptionSection}",
            PrologCodeSystemPrompt = "Generate Prolog code",
            PrologCodeUserPromptTemplate = "Implement: {GameIdea}",
            DefaultGameIdeaMaxTokens = 1024,
            DefaultGameIdeaTemperature = 0.8f,
            DefaultPrologCodeMaxTokens = 2048,
            DefaultPrologCodeTemperature = 0.5f,
            DefaultPrologGoal = "main"
        };

        _settings = Options.Create(settings);
        _requestValidator.ValidatePrologQuery(Arg.Any<string>())
            .Returns(ValidationResult.Success());

        _sut = new GameGeneratorService(
            _llmService,
            _prologService,
            _requestValidator,
            _settings);
    }

    [Fact]
    public void Constructor_WithNullLlmService_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new GameGeneratorService(
            null!,
            _prologService,
            _requestValidator,
            _settings);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("llmService");
    }

    [Fact]
    public void Constructor_WithNullPrologService_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new GameGeneratorService(
            _llmService,
            null!,
            _requestValidator,
            _settings);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("prologService");
    }

    [Fact]
    public void Constructor_WithNullRequestValidator_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new GameGeneratorService(
            _llmService,
            _prologService,
            null!,
            _settings);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("requestValidator");
    }

    [Fact]
    public void Constructor_WithNullSettings_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new GameGeneratorService(
            _llmService,
            _prologService,
            _requestValidator,
            null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("settings");
    }

    [Fact]
    public async Task GenerateGameAsync_WithNullRequest_ThrowsArgumentNullException()
    {
        // Act
        var act = () => _sut.GenerateGameAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task GenerateGameAsync_WithValidRequest_ReturnsSuccessfulResponse()
    {
        // Arrange
        var request = new GameGenerationRequest
        {
            Theme = "mystery",
            ExecuteGame = false
        };

        _llmService.CurrentProvider.Returns("Ollama");
        _llmService.ChatAsync(Arg.Any<ChatRequest>(), Arg.Any<CancellationToken>())
            .Returns(
                new CompletionResponse { Text = "A mystery game about solving clues", TokensGenerated = 50 },
                new CompletionResponse { Text = "```prolog\nmain :- write('Hello').\n```", TokensGenerated = 30 });

        // Act
        var result = await _sut.GenerateGameAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        result.GameIdea.Should().NotBeNullOrEmpty();
        result.PrologCode.Should().NotBeNullOrEmpty();
        result.ProviderUsed.Should().Be("Ollama");
        result.Error.Should().BeNull();
    }

    [Fact]
    public async Task GenerateGameAsync_WithExecuteGame_ExecutesPrologCode()
    {
        // Arrange
        var request = new GameGenerationRequest
        {
            Theme = "puzzle",
            ExecuteGame = true,
            PrologGoal = "main"
        };

        _llmService.CurrentProvider.Returns("Ollama");
        _llmService.ChatAsync(Arg.Any<ChatRequest>(), Arg.Any<CancellationToken>())
            .Returns(
                new CompletionResponse { Text = "A puzzle game", TokensGenerated = 20 },
                new CompletionResponse { Text = "```prolog\nmain :- write('Solved!').\n```", TokensGenerated = 20 });

        _prologService.ExecuteFileAsync(Arg.Any<PrologFileRequest>(), Arg.Any<CancellationToken>())
            .Returns(new PrologResponse
            {
                Success = true,
                Output = "Solved!",
                ExitCode = 0
            });

        // Act
        var result = await _sut.GenerateGameAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        result.ExecutionSuccess.Should().BeTrue();
        result.ExecutionOutput.Should().Be("Solved!");
        result.Timings?.PrologExecution.Should().NotBeNull();

        await _prologService.Received(1)
            .ExecuteFileAsync(Arg.Any<PrologFileRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GenerateGameAsync_WhenLlmServiceThrows_ReturnsFailureResponse()
    {
        // Arrange
        var request = new GameGenerationRequest { ExecuteGame = false };

        _llmService.ChatAsync(Arg.Any<ChatRequest>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("LLM service unavailable"));

        // Act
        var result = await _sut.GenerateGameAsync(request);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Contain("LLM service unavailable");
    }

    [Fact]
    public async Task GenerateGameAsync_WhenPrologExecutionFails_ReturnsPartialSuccess()
    {
        // Arrange
        var request = new GameGenerationRequest
        {
            ExecuteGame = true
        };

        _llmService.CurrentProvider.Returns("Ollama");
        _llmService.ChatAsync(Arg.Any<ChatRequest>(), Arg.Any<CancellationToken>())
            .Returns(
                new CompletionResponse { Text = "Game idea", TokensGenerated = 10 },
                new CompletionResponse { Text = "```prolog\nmain.\n```", TokensGenerated = 10 });

        _prologService.ExecuteFileAsync(Arg.Any<PrologFileRequest>(), Arg.Any<CancellationToken>())
            .Returns(new PrologResponse
            {
                Success = false,
                Error = "Syntax error",
                ExitCode = 1
            });

        // Act
        var result = await _sut.GenerateGameAsync(request);

        // Assert
        result.Success.Should().BeTrue(); // Overall success because generation worked
        result.GameIdea.Should().NotBeNullOrEmpty();
        result.PrologCode.Should().NotBeNullOrEmpty();
        result.ExecutionSuccess.Should().BeFalse();
        result.ExecutionError.Should().Be("Syntax error");
    }

    [Fact]
    public async Task GenerateGameAsync_IncludesTimingInformation()
    {
        // Arrange
        var request = new GameGenerationRequest { ExecuteGame = true };

        _llmService.CurrentProvider.Returns("Ollama");
        _llmService.ChatAsync(Arg.Any<ChatRequest>(), Arg.Any<CancellationToken>())
            .Returns(
                new CompletionResponse { Text = "Game", TokensGenerated = 5 },
                new CompletionResponse { Text = "```prolog\nmain.\n```", TokensGenerated = 5 });

        _prologService.ExecuteFileAsync(Arg.Any<PrologFileRequest>(), Arg.Any<CancellationToken>())
            .Returns(new PrologResponse { Success = true, Output = "OK" });

        // Act
        var result = await _sut.GenerateGameAsync(request);

        // Assert
        result.Duration.Should().BeGreaterThan(TimeSpan.Zero);
        result.Timings.Should().NotBeNull();
        result.Timings!.GameIdeaGeneration.Should().BeGreaterThanOrEqualTo(TimeSpan.Zero);
        result.Timings.PrologCodeGeneration.Should().BeGreaterThanOrEqualTo(TimeSpan.Zero);
        result.Timings.PrologExecution.Should().NotBeNull();
    }

    [Fact]
    public async Task GenerateGameAsync_WithCancellation_ThrowsOperationCanceledException()
    {
        // Arrange
        var request = new GameGenerationRequest { ExecuteGame = false };
        var cts = new CancellationTokenSource();
        cts.Cancel();

        _llmService.ChatAsync(Arg.Any<ChatRequest>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new OperationCanceledException());

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => _sut.GenerateGameAsync(request, cts.Token));
    }

    [Fact]
    public async Task GenerateGameAsync_WhenExecuteGameFalse_DoesNotAttemptFixes()
    {
        // Arrange
        var request = new GameGenerationRequest { ExecuteGame = false };

        _llmService.CurrentProvider.Returns("Ollama");
        _llmService.ChatAsync(Arg.Any<ChatRequest>(), Arg.Any<CancellationToken>())
            .Returns(
                new CompletionResponse { Text = "Game idea", TokensGenerated = 10 },
                new CompletionResponse { Text = "```prolog\nmain :- broken.\n```", TokensGenerated = 10 });

        // Act
        var result = await _sut.GenerateGameAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        result.ExecutionSuccess.Should().BeNull();
        result.FixAttempts.Should().Be(0);

        // Should not have called Prolog service at all
        await _prologService.DidNotReceive()
            .ExecuteFileAsync(Arg.Any<PrologFileRequest>(), Arg.Any<CancellationToken>());
    }
}

