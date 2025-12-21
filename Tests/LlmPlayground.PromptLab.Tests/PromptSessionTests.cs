using FluentAssertions;
using LlmPlayground.Core;
using LlmPlayground.PromptLab;
using NSubstitute;

namespace LlmPlayground.PromptLab.Tests;

public class PromptSessionTests
{
    private readonly ILlmProvider _mockProvider;

    public PromptSessionTests()
    {
        _mockProvider = Substitute.For<ILlmProvider>();
        _mockProvider.ProviderName.Returns("MockProvider");
        _mockProvider.IsReady.Returns(true);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidProvider_ShouldCreateSession()
    {
        // Act
        var session = new PromptSession(_mockProvider);

        // Assert
        session.Should().NotBeNull();
        session.Provider.Should().Be(_mockProvider);
        session.History.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_WithNullProvider_ShouldThrow()
    {
        // Act
        var act = () => new PromptSession(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_WithSystemPrompt_ShouldSetSystemPrompt()
    {
        // Act
        var session = new PromptSession(_mockProvider, "You are helpful.");

        // Assert
        session.SystemPrompt.Should().Be("You are helpful.");
    }

    [Fact]
    public void Constructor_WithOptions_ShouldSetOptions()
    {
        // Arrange
        var options = new LlmInferenceOptions { MaxTokens = 1000 };

        // Act
        var session = new PromptSession(_mockProvider, options: options);

        // Assert
        session.Options.MaxTokens.Should().Be(1000);
    }

    #endregion

    #region SendAsync Tests

    [Fact]
    public async Task SendAsync_WithValidPrompt_ShouldReturnResult()
    {
        // Arrange
        var expectedResponse = "Hello! How can I help?";
        _mockProvider.ChatAsync(
            Arg.Any<IReadOnlyList<ChatMessage>>(),
            Arg.Any<LlmInferenceOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(new LlmCompletionResult(expectedResponse, 10, TimeSpan.FromSeconds(1)));

        var session = new PromptSession(_mockProvider);

        // Act
        var result = await session.SendAsync("Hello");

        // Assert
        result.Should().NotBeNull();
        result.Response.Should().Be(expectedResponse);
        result.Prompt.Should().Be("Hello");
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task SendAsync_ShouldAddToHistory()
    {
        // Arrange
        _mockProvider.ChatAsync(
            Arg.Any<IReadOnlyList<ChatMessage>>(),
            Arg.Any<LlmInferenceOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(new LlmCompletionResult("Response", 5, TimeSpan.FromMilliseconds(500)));

        var session = new PromptSession(_mockProvider);

        // Act
        await session.SendAsync("Test prompt");

        // Assert
        session.History.Should().HaveCount(1);
        session.History[0].Prompt.Should().Be("Test prompt");
        session.History[0].Response.Should().Be("Response");
    }

    [Fact]
    public async Task SendAsync_WithSystemPrompt_ShouldIncludeInMessages()
    {
        // Arrange
        IReadOnlyList<ChatMessage>? capturedMessages = null;
        _mockProvider.ChatAsync(
            Arg.Do<IReadOnlyList<ChatMessage>>(m => capturedMessages = m),
            Arg.Any<LlmInferenceOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(new LlmCompletionResult("Response", 5, TimeSpan.FromMilliseconds(100)));

        var session = new PromptSession(_mockProvider, "Be concise.");

        // Act
        await session.SendAsync("Hello");

        // Assert
        capturedMessages.Should().NotBeNull();
        capturedMessages.Should().HaveCount(2);
        capturedMessages![0].Role.Should().Be(ChatRole.System);
        capturedMessages[0].Content.Should().Be("Be concise.");
        capturedMessages[1].Role.Should().Be(ChatRole.User);
        capturedMessages[1].Content.Should().Be("Hello");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SendAsync_WithInvalidPrompt_ShouldThrow(string? prompt)
    {
        // Arrange
        var session = new PromptSession(_mockProvider);

        // Act
        var act = () => session.SendAsync(prompt!);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task SendAsync_AfterDispose_ShouldThrow()
    {
        // Arrange
        var session = new PromptSession(_mockProvider);
        session.Dispose();

        // Act
        var act = () => session.SendAsync("Hello");

        // Assert
        await act.Should().ThrowAsync<ObjectDisposedException>();
    }

    #endregion

    #region Conversation History Tests

    [Fact]
    public async Task SendAsync_WithHistory_ShouldIncludePreviousMessages()
    {
        // Arrange
        IReadOnlyList<ChatMessage>? capturedMessages = null;
        var callCount = 0;
        _mockProvider.ChatAsync(
            Arg.Do<IReadOnlyList<ChatMessage>>(m => 
            {
                callCount++;
                if (callCount == 2) capturedMessages = m;
            }),
            Arg.Any<LlmInferenceOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(x => new LlmCompletionResult($"Response {callCount}", 5, TimeSpan.FromMilliseconds(100)));

        var session = new PromptSession(_mockProvider);

        // Act
        await session.SendAsync("First message");
        await session.SendAsync("Second message");

        // Assert - Second call should include: first user + first assistant + second user = 3 messages
        capturedMessages.Should().NotBeNull();
        capturedMessages.Should().HaveCount(3);
        capturedMessages![0].Role.Should().Be(ChatRole.User);
        capturedMessages[0].Content.Should().Be("First message");
        capturedMessages[1].Role.Should().Be(ChatRole.Assistant);
        capturedMessages[1].Content.Should().Be("Response 1");
        capturedMessages[2].Role.Should().Be(ChatRole.User);
        capturedMessages[2].Content.Should().Be("Second message");
    }

    #endregion

    #region ClearHistory Tests

    [Fact]
    public async Task ClearHistory_ShouldEmptyHistory()
    {
        // Arrange
        _mockProvider.ChatAsync(
            Arg.Any<IReadOnlyList<ChatMessage>>(),
            Arg.Any<LlmInferenceOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(new LlmCompletionResult("Response", 5, TimeSpan.FromMilliseconds(100)));

        var session = new PromptSession(_mockProvider);
        await session.SendAsync("Test");

        // Act
        session.ClearHistory();

        // Assert
        session.History.Should().BeEmpty();
    }

    #endregion

    #region RetryLastAsync Tests

    [Fact]
    public async Task RetryLastAsync_WithHistory_ShouldRetryLastPrompt()
    {
        // Arrange
        var responseCount = 0;
        _mockProvider.ChatAsync(
            Arg.Any<IReadOnlyList<ChatMessage>>(),
            Arg.Any<LlmInferenceOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(_ => new LlmCompletionResult($"Response {++responseCount}", 5, TimeSpan.FromMilliseconds(100)));

        var session = new PromptSession(_mockProvider);
        await session.SendAsync("Original prompt");

        // Act
        var result = await session.RetryLastAsync();

        // Assert
        result.Prompt.Should().Be("Original prompt");
        session.History.Should().HaveCount(2);
    }

    [Fact]
    public async Task RetryLastAsync_WithNoHistory_ShouldThrow()
    {
        // Arrange
        var session = new PromptSession(_mockProvider);

        // Act
        var act = () => session.RetryLastAsync();

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*No previous prompt*");
    }

    #endregion

    #region LastExchange Tests

    [Fact]
    public void LastExchange_WithNoHistory_ShouldBeNull()
    {
        // Arrange
        var session = new PromptSession(_mockProvider);

        // Assert
        session.LastExchange.Should().BeNull();
    }

    [Fact]
    public async Task LastExchange_WithHistory_ShouldReturnLast()
    {
        // Arrange
        _mockProvider.ChatAsync(
            Arg.Any<IReadOnlyList<ChatMessage>>(),
            Arg.Any<LlmInferenceOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(new LlmCompletionResult("Last response", 5, TimeSpan.FromMilliseconds(100)));

        var session = new PromptSession(_mockProvider);
        await session.SendAsync("Last prompt");

        // Assert
        session.LastExchange.Should().NotBeNull();
        session.LastExchange!.Prompt.Should().Be("Last prompt");
        session.LastExchange.Response.Should().Be("Last response");
    }

    #endregion

    #region PromptResult Tests

    [Fact]
    public void PromptResult_TokensPerSecond_ShouldCalculateCorrectly()
    {
        // Arrange
        var result = new PromptResult
        {
            Prompt = "Test",
            Response = "Response",
            TokensGenerated = 100,
            Duration = TimeSpan.FromSeconds(2),
            Success = true
        };

        // Assert
        result.TokensPerSecond.Should().Be(50);
    }

    [Fact]
    public void PromptResult_TokensPerSecond_WithZeroDuration_ShouldReturnZero()
    {
        // Arrange
        var result = new PromptResult
        {
            Prompt = "Test",
            Response = "Response",
            TokensGenerated = 100,
            Duration = TimeSpan.Zero,
            Success = true
        };

        // Assert
        result.TokensPerSecond.Should().Be(0);
    }

    #endregion
}

