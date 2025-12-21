using FluentAssertions;
using LlmPlayground.Api.Controllers;
using LlmPlayground.Api.Models;
using LlmPlayground.Api.Services;
using LlmPlayground.Utilities.Validation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace LlmPlayground.Api.Tests.Controllers;

public class GameGeneratorControllerTests
{
    private readonly IGameGeneratorService _gameGeneratorService;
    private readonly IRequestValidator _requestValidator;
    private readonly ILogger<GameGeneratorController> _logger;
    private readonly GameGeneratorController _sut;

    public GameGeneratorControllerTests()
    {
        _gameGeneratorService = Substitute.For<IGameGeneratorService>();
        _requestValidator = Substitute.For<IRequestValidator>();
        _logger = Substitute.For<ILogger<GameGeneratorController>>();

        // Default to successful validation
        _requestValidator.ValidatePrompt(Arg.Any<string>(), Arg.Any<int>())
            .Returns(ValidationResult.Success());
        _requestValidator.ValidatePrologQuery(Arg.Any<string>())
            .Returns(ValidationResult.Success());

        _sut = new GameGeneratorController(
            _gameGeneratorService,
            _requestValidator,
            _logger);
    }

    [Fact]
    public void Constructor_WithNullGameGeneratorService_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new GameGeneratorController(
            null!,
            _requestValidator,
            _logger);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("gameGeneratorService");
    }

    [Fact]
    public void Constructor_WithNullRequestValidator_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new GameGeneratorController(
            _gameGeneratorService,
            null!,
            _logger);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("requestValidator");
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new GameGeneratorController(
            _gameGeneratorService,
            _requestValidator,
            null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public async Task GenerateGame_WithValidRequest_ReturnsOkResult()
    {
        // Arrange
        var request = new GameGenerationRequest
        {
            Theme = "adventure",
            ExecuteGame = true
        };

        var expectedResponse = new GameGenerationResponse
        {
            Success = true,
            GameIdea = "An adventure game",
            PrologCode = "main :- write('Adventure!').",
            ExecutionOutput = "Adventure!",
            ExecutionSuccess = true,
            ProviderUsed = "Ollama",
            Duration = TimeSpan.FromSeconds(5)
        };

        _gameGeneratorService.GenerateGameAsync(Arg.Any<GameGenerationRequest>(), Arg.Any<CancellationToken>())
            .Returns(expectedResponse);

        // Act
        var result = await _sut.GenerateGame(request, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task GenerateGame_WithInvalidTheme_ReturnsBadRequest()
    {
        // Arrange
        var request = new GameGenerationRequest
        {
            Theme = new string('x', 300) // Exceeds max length
        };

        _requestValidator.ValidatePrompt(request.Theme, 200)
            .Returns(ValidationResult.Failure("Theme", "Theme exceeds maximum length"));

        // Act
        var result = await _sut.GenerateGame(request, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
        var badRequest = result.Result as BadRequestObjectResult;
        var errorResponse = badRequest!.Value as ErrorResponse;
        errorResponse!.Error.Should().Be("Validation failed");
        errorResponse.Details.Should().Contain(d => d.Contains("Theme"));
    }

    [Fact]
    public async Task GenerateGame_WithInvalidProvider_ReturnsBadRequest()
    {
        // Arrange
        var request = new GameGenerationRequest
        {
            Provider = "InvalidProvider"
        };

        // Act
        var result = await _sut.GenerateGame(request, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
        var badRequest = result.Result as BadRequestObjectResult;
        var errorResponse = badRequest!.Value as ErrorResponse;
        errorResponse!.Details.Should().Contain(d => d.Contains("Invalid provider"));
    }

    [Fact]
    public async Task GenerateGame_WithUnsafePrologGoal_ReturnsBadRequest()
    {
        // Arrange
        var request = new GameGenerationRequest
        {
            PrologGoal = "shell('rm -rf /')"
        };

        _requestValidator.ValidatePrologQuery(request.PrologGoal)
            .Returns(ValidationResult.Failure("Query", "Contains dangerous predicate: shell"));

        // Act
        var result = await _sut.GenerateGame(request, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GenerateGame_WhenServiceThrows_ReturnsInternalServerError()
    {
        // Arrange
        var request = new GameGenerationRequest();

        _gameGeneratorService.GenerateGameAsync(Arg.Any<GameGenerationRequest>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception("Unexpected error"));

        // Act
        var result = await _sut.GenerateGame(request, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<ObjectResult>();
        var objectResult = result.Result as ObjectResult;
        objectResult!.StatusCode.Should().Be(500);
        var errorResponse = objectResult.Value as ErrorResponse;
        errorResponse!.Error.Should().Contain("unexpected error");
    }

    [Fact]
    public async Task GenerateGame_WhenCancelled_ThrowsOperationCanceledException()
    {
        // Arrange
        var request = new GameGenerationRequest();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        _gameGeneratorService.GenerateGameAsync(Arg.Any<GameGenerationRequest>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new OperationCanceledException());

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => _sut.GenerateGame(request, cts.Token));
    }

    [Fact]
    public async Task GenerateGame_SanitizesInput()
    {
        // Arrange
        var request = new GameGenerationRequest
        {
            Theme = "  adventure\0  ", // With null byte and extra spaces
            Description = "Test\x00description"
        };

        _gameGeneratorService.GenerateGameAsync(Arg.Any<GameGenerationRequest>(), Arg.Any<CancellationToken>())
            .Returns(new GameGenerationResponse { Success = true });

        // Act
        await _sut.GenerateGame(request, CancellationToken.None);

        // Assert
        await _gameGeneratorService.Received(1)
            .GenerateGameAsync(
                Arg.Is<GameGenerationRequest>(r =>
                    !r.Theme!.Contains('\0') &&
                    !r.Description!.Contains('\0')),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public void GetHealth_ReturnsOkWithHealthyStatus()
    {
        // Act
        var result = _sut.GetHealth();

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var health = okResult!.Value as HealthResponse;
        health!.Status.Should().Be("healthy");
        health.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Theory]
    [InlineData("Ollama")]
    [InlineData("LmStudio")]
    [InlineData("OpenAI")]
    [InlineData("ollama")] // Case insensitive
    [InlineData("LMSTUDIO")]
    public async Task GenerateGame_WithValidProvider_AcceptsRequest(string provider)
    {
        // Arrange
        var request = new GameGenerationRequest { Provider = provider };

        _gameGeneratorService.GenerateGameAsync(Arg.Any<GameGenerationRequest>(), Arg.Any<CancellationToken>())
            .Returns(new GameGenerationResponse { Success = true });

        // Act
        var result = await _sut.GenerateGame(request, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GenerateGame_WithEmptyRequest_AcceptsAndProcesses()
    {
        // Arrange
        var request = new GameGenerationRequest();

        _gameGeneratorService.GenerateGameAsync(Arg.Any<GameGenerationRequest>(), Arg.Any<CancellationToken>())
            .Returns(new GameGenerationResponse { Success = true });

        // Act
        var result = await _sut.GenerateGame(request, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GenerateGame_WithFailedGeneration_ReturnsOkWithErrorDetails()
    {
        // Arrange
        var request = new GameGenerationRequest();

        var failedResponse = new GameGenerationResponse
        {
            Success = false,
            Error = "LLM provider connection failed"
        };

        _gameGeneratorService.GenerateGameAsync(Arg.Any<GameGenerationRequest>(), Arg.Any<CancellationToken>())
            .Returns(failedResponse);

        // Act
        var result = await _sut.GenerateGame(request, CancellationToken.None);

        // Assert
        // The endpoint returns OK even for failed generations (the response indicates failure)
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var response = okResult!.Value as GameGenerationResponse;
        response!.Success.Should().BeFalse();
        response.Error.Should().Be("LLM provider connection failed");
    }
}

