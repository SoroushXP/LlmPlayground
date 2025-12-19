using FluentAssertions;
using LlmPlayground.Core;

namespace LlmPlayground.Core.Tests;

public class OpenAiProviderTests
{
    private const string TestApiKey = "sk-test-key-12345";

    [Fact]
    public void Constructor_WithNullConfiguration_ShouldThrowArgumentNullException()
    {
        // Arrange & Act
        var act = () => new OpenAiProvider(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("configuration");
    }

    [Fact]
    public void Constructor_WithEmptyApiKey_ShouldThrowArgumentException()
    {
        // Arrange
        var config = new OpenAiConfiguration { ApiKey = "" };

        // Act
        var act = () => new OpenAiProvider(config);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("configuration")
            .WithMessage("*API key cannot be empty*");
    }

    [Fact]
    public void Constructor_WithWhitespaceApiKey_ShouldThrowArgumentException()
    {
        // Arrange
        var config = new OpenAiConfiguration { ApiKey = "   " };

        // Act
        var act = () => new OpenAiProvider(config);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("configuration");
    }

    [Fact]
    public void Constructor_WithValidConfiguration_ShouldNotThrow()
    {
        // Arrange
        var config = new OpenAiConfiguration { ApiKey = TestApiKey };

        // Act
        var act = () => new OpenAiProvider(config);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void ProviderName_ShouldContainModelName()
    {
        // Arrange
        var config = new OpenAiConfiguration
        {
            ApiKey = TestApiKey,
            Model = "gpt-4o"
        };
        using var provider = new OpenAiProvider(config);

        // Act
        var name = provider.ProviderName;

        // Assert
        name.Should().Contain("OpenAI");
        name.Should().Contain("gpt-4o");
    }

    [Fact]
    public void IsReady_BeforeInitialization_ShouldBeFalse()
    {
        // Arrange
        var config = new OpenAiConfiguration { ApiKey = TestApiKey };
        using var provider = new OpenAiProvider(config);

        // Act & Assert
        provider.IsReady.Should().BeFalse();
    }

    [Fact]
    public async Task InitializeAsync_ShouldSetIsReadyToTrue()
    {
        // Arrange
        var config = new OpenAiConfiguration { ApiKey = TestApiKey };
        using var provider = new OpenAiProvider(config);

        // Act
        await provider.InitializeAsync();

        // Assert
        provider.IsReady.Should().BeTrue();
    }

    [Fact]
    public void Dispose_ShouldNotThrow()
    {
        // Arrange
        var config = new OpenAiConfiguration { ApiKey = TestApiKey };
        var provider = new OpenAiProvider(config);

        // Act
        var act = () => provider.Dispose();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Dispose_CalledMultipleTimes_ShouldNotThrow()
    {
        // Arrange
        var config = new OpenAiConfiguration { ApiKey = TestApiKey };
        var provider = new OpenAiProvider(config);

        // Act
        var act = () =>
        {
            provider.Dispose();
            provider.Dispose();
            provider.Dispose();
        };

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public async Task DisposeAsync_ShouldNotThrow()
    {
        // Arrange
        var config = new OpenAiConfiguration { ApiKey = TestApiKey };
        var provider = new OpenAiProvider(config);

        // Act
        var act = async () => await provider.DisposeAsync();

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public void IsReady_AfterDispose_ShouldBeFalse()
    {
        // Arrange
        var config = new OpenAiConfiguration { ApiKey = TestApiKey };
        var provider = new OpenAiProvider(config);
        provider.Dispose();

        // Act & Assert
        provider.IsReady.Should().BeFalse();
    }

    [Fact]
    public async Task CompleteAsync_AfterDispose_ShouldThrowObjectDisposedException()
    {
        // Arrange
        var config = new OpenAiConfiguration { ApiKey = TestApiKey };
        var provider = new OpenAiProvider(config);
        provider.Dispose();

        // Act
        var act = async () => await provider.CompleteAsync("test");

        // Assert
        await act.Should().ThrowAsync<ObjectDisposedException>();
    }

    [Fact]
    public async Task InitializeAsync_AfterDispose_ShouldThrowObjectDisposedException()
    {
        // Arrange
        var config = new OpenAiConfiguration { ApiKey = TestApiKey };
        var provider = new OpenAiProvider(config);
        provider.Dispose();

        // Act
        var act = async () => await provider.InitializeAsync();

        // Assert
        await act.Should().ThrowAsync<ObjectDisposedException>();
    }

    [Fact]
    public async Task CompleteAsync_WithoutInitialization_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var config = new OpenAiConfiguration { ApiKey = TestApiKey };
        using var provider = new OpenAiProvider(config);

        // Act
        var act = async () => await provider.CompleteAsync("test");

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not initialized*");
    }

    [Fact]
    public async Task InitializeAsync_CalledMultipleTimes_ShouldNotThrow()
    {
        // Arrange
        var config = new OpenAiConfiguration { ApiKey = TestApiKey };
        using var provider = new OpenAiProvider(config);

        // Act
        await provider.InitializeAsync();
        var act = async () => await provider.InitializeAsync();

        // Assert
        await act.Should().NotThrowAsync();
        provider.IsReady.Should().BeTrue();
    }
}

