using FluentAssertions;
using LlmPlayground.Services.Interfaces;
using LlmPlayground.Services.Models;
using LlmPlayground.Services.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace LlmPlayground.Services.Tests;

public class PromptLabServiceTests : IDisposable
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<PromptLabService> _logger;
    private readonly PromptLabService _sut;

    public PromptLabServiceTests()
    {
        var configData = new Dictionary<string, string?>
        {
            ["Ollama:Host"] = "localhost",
            ["Ollama:Port"] = "11434",
            ["Ollama:Model"] = "llama3",
            ["LmStudio:Host"] = "localhost",
            ["LmStudio:Port"] = "1234",
            ["OpenAI:ApiKey"] = "test-key",
            ["OpenAI:Model"] = "gpt-4o-mini"
        };

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        _logger = Substitute.For<ILogger<PromptLabService>>();
        _sut = new PromptLabService(_configuration, _logger);
    }

    [Fact]
    public void Constructor_WithNullConfiguration_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new PromptLabService(null!, _logger);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("configuration");
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new PromptLabService(_configuration, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public void GetActiveSessions_Initially_ReturnsEmptyList()
    {
        // Act
        var sessions = _sut.GetActiveSessions();

        // Assert
        sessions.Should().BeEmpty();
    }

    [Fact]
    public void GetSession_WithNonExistentSessionId_ReturnsNull()
    {
        // Act
        var session = _sut.GetSession("nonexistent");

        // Assert
        session.Should().BeNull();
    }

    [Fact]
    public void GetSession_WithNullSessionId_ThrowsArgumentException()
    {
        // Act
        var act = () => _sut.GetSession(null!);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void GetSession_WithEmptySessionId_ThrowsArgumentException()
    {
        // Act
        var act = () => _sut.GetSession("");

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public async Task CreateSessionAsync_WithNullRequest_ThrowsArgumentNullException()
    {
        // Act
        var act = () => _sut.CreateSessionAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task SendPromptAsync_WithNullSessionId_ThrowsArgumentException()
    {
        // Arrange
        var request = new SendPromptRequest { Prompt = "Hello" };

        // Act
        var act = () => _sut.SendPromptAsync(null!, request);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task SendPromptAsync_WithNullRequest_ThrowsArgumentNullException()
    {
        // Act
        var act = () => _sut.SendPromptAsync("session-id", null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task SendPromptAsync_WithNonExistentSession_ThrowsKeyNotFoundException()
    {
        // Arrange
        var request = new SendPromptRequest { Prompt = "Hello" };

        // Act
        var act = () => _sut.SendPromptAsync("nonexistent", request);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task RetryLastAsync_WithNonExistentSession_ThrowsKeyNotFoundException()
    {
        // Act
        var act = () => _sut.RetryLastAsync("nonexistent");

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public void ClearSessionHistory_WithNonExistentSession_ReturnsFalse()
    {
        // Act
        var result = _sut.ClearSessionHistory("nonexistent");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ClearSessionHistory_WithNullSessionId_ThrowsArgumentException()
    {
        // Act
        var act = () => _sut.ClearSessionHistory(null!);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CloseSession_WithNonExistentSession_ReturnsFalse()
    {
        // Act
        var result = _sut.CloseSession("nonexistent");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void CloseSession_WithNullSessionId_ThrowsArgumentException()
    {
        // Act
        var act = () => _sut.CloseSession(null!);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void RenderTemplate_WithValidTemplate_ReturnsRenderedPrompt()
    {
        // Arrange
        var request = new RenderTemplateRequest
        {
            Template = "Hello {{name}}, welcome to {{place}}!",
            Variables = new Dictionary<string, string>
            {
                ["name"] = "World",
                ["place"] = "Earth"
            }
        };

        // Act
        var result = _sut.RenderTemplate(request);

        // Assert
        result.RenderedPrompt.Should().Be("Hello World, welcome to Earth!");
        result.Variables.Should().Contain("name");
        result.Variables.Should().Contain("place");
        result.MissingVariables.Should().BeEmpty();
    }

    [Fact]
    public void RenderTemplate_WithMissingVariables_ReportsMissingVariables()
    {
        // Arrange
        var request = new RenderTemplateRequest
        {
            Template = "Hello {{name}}, welcome to {{place}}!",
            Variables = new Dictionary<string, string>
            {
                ["name"] = "World"
            }
        };

        // Act
        var result = _sut.RenderTemplate(request);

        // Assert
        result.MissingVariables.Should().Contain("place");
        result.RenderedPrompt.Should().Contain("{{place}}");
    }

    [Fact]
    public void RenderTemplate_WithNullRequest_ThrowsArgumentNullException()
    {
        // Act
        var act = () => _sut.RenderTemplate(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void RenderTemplate_WithEmptyTemplate_ThrowsArgumentException()
    {
        // Arrange
        var request = new RenderTemplateRequest
        {
            Template = "",
            Variables = new Dictionary<string, string>()
        };

        // Act
        var act = () => _sut.RenderTemplate(request);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void GetTemplateVariables_WithValidTemplate_ReturnsVariables()
    {
        // Arrange
        var template = "Hello {{name}}, your code is {{code}}!";

        // Act
        var variables = _sut.GetTemplateVariables(template);

        // Assert
        variables.Should().HaveCount(2);
        variables.Should().Contain("name");
        variables.Should().Contain("code");
    }

    [Fact]
    public void GetTemplateVariables_WithNoVariables_ReturnsEmptyList()
    {
        // Arrange
        var template = "Hello World!";

        // Act
        var variables = _sut.GetTemplateVariables(template);

        // Assert
        variables.Should().BeEmpty();
    }

    [Fact]
    public void GetTemplateVariables_WithNullTemplate_ThrowsArgumentException()
    {
        // Act
        var act = () => _sut.GetTemplateVariables(null!);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void GetTemplateVariables_WithEmptyTemplate_ThrowsArgumentException()
    {
        // Act
        var act = () => _sut.GetTemplateVariables("");

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void GetActiveSessions_WhenDisposed_ThrowsObjectDisposedException()
    {
        // Arrange
        _sut.Dispose();

        // Act
        var act = () => _sut.GetActiveSessions();

        // Assert
        act.Should().Throw<ObjectDisposedException>();
    }

    public void Dispose()
    {
        _sut.Dispose();
    }
}

