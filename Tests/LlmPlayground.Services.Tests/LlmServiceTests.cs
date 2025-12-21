using FluentAssertions;
using LlmPlayground.Core;
using LlmPlayground.Services.Interfaces;
using LlmPlayground.Services.Models;
using LlmPlayground.Services.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace LlmPlayground.Services.Tests;

public class LlmServiceTests : IDisposable
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<LlmService> _logger;
    private readonly LlmService _sut;

    public LlmServiceTests()
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

        _logger = Substitute.For<ILogger<LlmService>>();
        _sut = new LlmService(_configuration, _logger);
    }

    [Fact]
    public void Constructor_WithNullConfiguration_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new LlmService(null!, _logger);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("configuration");
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new LlmService(_configuration, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public void GetAvailableProviders_ReturnsAllProviders()
    {
        // Act
        var providers = _sut.GetAvailableProviders();

        // Assert
        providers.Should().HaveCount(3);
        providers.Should().Contain("Ollama");
        providers.Should().Contain("LmStudio");
        providers.Should().Contain("OpenAI");
    }

    [Fact]
    public void CurrentProvider_DefaultsToOllama()
    {
        // Act & Assert
        _sut.CurrentProvider.Should().Be("Ollama");
    }

    [Fact]
    public void IsReady_WhenNoProviderInitialized_ReturnsFalse()
    {
        // Act & Assert
        _sut.IsReady.Should().BeFalse();
    }

    [Fact]
    public void CurrentModel_WhenNoProviderInitialized_ReturnsNull()
    {
        // Act & Assert
        _sut.CurrentModel.Should().BeNull();
    }

    [Fact]
    public void SetModel_WhenNoProviderInitialized_ThrowsInvalidOperationException()
    {
        // Act
        var act = () => _sut.SetModel("test-model");

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void SetModel_WithNullModelId_ThrowsArgumentException()
    {
        // Act
        var act = () => _sut.SetModel(null!);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void SetModel_WithEmptyModelId_ThrowsArgumentException()
    {
        // Act
        var act = () => _sut.SetModel("");

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public async Task CompleteAsync_WithNullRequest_ThrowsArgumentNullException()
    {
        // Act
        var act = () => _sut.CompleteAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task ChatAsync_WithNullRequest_ThrowsArgumentNullException()
    {
        // Act
        var act = () => _sut.ChatAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task GetAvailableModelsAsync_WhenDisposed_ThrowsObjectDisposedException()
    {
        // Arrange
        _sut.Dispose();

        // Act
        var act = () => _sut.GetAvailableModelsAsync();

        // Assert
        await act.Should().ThrowAsync<ObjectDisposedException>();
    }

    [Fact]
    public async Task SetProviderAsync_WhenDisposed_ThrowsObjectDisposedException()
    {
        // Arrange
        _sut.Dispose();

        // Act
        var act = () => _sut.SetProviderAsync(LlmProviderType.Ollama);

        // Assert
        await act.Should().ThrowAsync<ObjectDisposedException>();
    }

    public void Dispose()
    {
        _sut.Dispose();
    }
}

