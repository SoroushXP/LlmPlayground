using FluentAssertions;
using LlmPlayground.Core;

namespace LlmPlayground.Core.Tests;

public class OllamaConfigurationTests
{
    [Fact]
    public void DefaultValues_ShouldBeSetCorrectly()
    {
        // Arrange & Act
        var config = new OllamaConfiguration();

        // Assert
        config.Host.Should().Be("localhost");
        config.Port.Should().Be(11434);
        config.Scheme.Should().Be("http");
        config.ApiPath.Should().Be("/v1");
        config.Model.Should().Be("llama3");
        config.SystemPrompt.Should().BeNull();
        config.TimeoutSeconds.Should().Be(300);
        config.BaseUrlOverride.Should().BeNull();
    }

    [Fact]
    public void BaseUrl_ShouldBeConstructedCorrectly()
    {
        // Arrange
        var config = new OllamaConfiguration
        {
            Host = "192.168.1.100",
            Port = 8080
        };

        // Act & Assert
        config.BaseUrl.Should().Be("http://192.168.1.100:8080/v1");
    }

    [Fact]
    public void BaseUrl_WithHttpsScheme_ShouldUseHttps()
    {
        // Arrange
        var config = new OllamaConfiguration
        {
            Host = "secure-ollama.example.com",
            Port = 443,
            Scheme = "https"
        };

        // Act & Assert
        config.BaseUrl.Should().Be("https://secure-ollama.example.com:443/v1");
    }

    [Fact]
    public void BaseUrl_WithCustomApiPath_ShouldUseCustomPath()
    {
        // Arrange
        var config = new OllamaConfiguration
        {
            Host = "localhost",
            Port = 11434,
            ApiPath = "/v2/openai"
        };

        // Act & Assert
        config.BaseUrl.Should().Be("http://localhost:11434/v2/openai");
    }

    [Fact]
    public void BaseUrl_WithOverride_ShouldIgnoreOtherProperties()
    {
        // Arrange
        var config = new OllamaConfiguration
        {
            Host = "ignored-host",
            Port = 9999,
            Scheme = "https",
            ApiPath = "/ignored",
            BaseUrlOverride = "http://custom.server:8080/custom/path"
        };

        // Act & Assert
        config.BaseUrl.Should().Be("http://custom.server:8080/custom/path");
    }

    [Fact]
    public void AllProperties_ShouldBeConfigurable()
    {
        // Arrange & Act
        var config = new OllamaConfiguration
        {
            Host = "ollama-server",
            Port = 9999,
            Scheme = "https",
            ApiPath = "/v2",
            Model = "mistral",
            SystemPrompt = "You are a coding assistant.",
            TimeoutSeconds = 600
        };

        // Assert
        config.Host.Should().Be("ollama-server");
        config.Port.Should().Be(9999);
        config.Scheme.Should().Be("https");
        config.ApiPath.Should().Be("/v2");
        config.Model.Should().Be("mistral");
        config.SystemPrompt.Should().Be("You are a coding assistant.");
        config.TimeoutSeconds.Should().Be(600);
    }
}

public class OllamaProviderTests
{
    [Fact]
    public void Constructor_WithNullConfiguration_ShouldThrowArgumentNullException()
    {
        // Arrange & Act
        var act = () => new OllamaProvider(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("configuration");
    }

    [Fact]
    public async Task InitializeAsync_WithEmptyModel_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var config = new OllamaConfiguration { Model = "" };
        using var provider = new OllamaProvider(config);

        // Act
        var act = () => provider.InitializeAsync();

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Model name is not set*");
    }

    [Fact]
    public void Constructor_WithValidConfiguration_ShouldNotThrow()
    {
        // Arrange
        var config = new OllamaConfiguration { Model = "llama3" };

        // Act
        var act = () => new OllamaProvider(config);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void ProviderName_ShouldContainModelName()
    {
        // Arrange
        var config = new OllamaConfiguration { Model = "codellama" };
        using var provider = new OllamaProvider(config);

        // Act
        var name = provider.ProviderName;

        // Assert
        name.Should().Contain("Ollama");
        name.Should().Contain("codellama");
    }

    [Fact]
    public void IsReady_BeforeInitialization_ShouldBeFalse()
    {
        // Arrange
        var config = new OllamaConfiguration();
        using var provider = new OllamaProvider(config);

        // Act & Assert
        provider.IsReady.Should().BeFalse();
    }

    [Fact]
    public async Task InitializeAsync_ShouldSetIsReadyToTrue()
    {
        // Arrange
        var config = new OllamaConfiguration();
        using var provider = new OllamaProvider(config);

        // Act
        await provider.InitializeAsync();

        // Assert
        provider.IsReady.Should().BeTrue();
    }

    [Fact]
    public void Dispose_ShouldNotThrow()
    {
        // Arrange
        var config = new OllamaConfiguration();
        var provider = new OllamaProvider(config);

        // Act
        var act = () => provider.Dispose();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void IsReady_AfterDispose_ShouldBeFalse()
    {
        // Arrange
        var config = new OllamaConfiguration();
        var provider = new OllamaProvider(config);
        provider.Dispose();

        // Act & Assert
        provider.IsReady.Should().BeFalse();
    }

    [Fact]
    public async Task CompleteAsync_AfterDispose_ShouldThrowObjectDisposedException()
    {
        // Arrange
        var config = new OllamaConfiguration();
        var provider = new OllamaProvider(config);
        provider.Dispose();

        // Act
        var act = async () => await provider.CompleteAsync("test");

        // Assert
        await act.Should().ThrowAsync<ObjectDisposedException>();
    }

    [Fact]
    public async Task CompleteAsync_WithoutInitialization_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var config = new OllamaConfiguration();
        using var provider = new OllamaProvider(config);

        // Act
        var act = async () => await provider.CompleteAsync("test");

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not initialized*");
    }

    // === IModelListingProvider Tests ===

    [Fact]
    public void CurrentModel_ShouldReturnConfiguredModel()
    {
        // Arrange
        var config = new OllamaConfiguration { Model = "mistral" };
        using var provider = new OllamaProvider(config);

        // Act & Assert
        provider.CurrentModel.Should().Be("mistral");
    }

    [Fact]
    public void SetModel_ShouldUpdateCurrentModel()
    {
        // Arrange
        var config = new OllamaConfiguration { Model = "llama3" };
        using var provider = new OllamaProvider(config);

        // Act
        provider.SetModel("codellama");

        // Assert
        provider.CurrentModel.Should().Be("codellama");
    }

    [Fact]
    public void SetModel_WithEmptyString_ShouldThrowArgumentException()
    {
        // Arrange
        var config = new OllamaConfiguration { Model = "llama3" };
        using var provider = new OllamaProvider(config);

        // Act
        var act = () => provider.SetModel("");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("modelId");
    }

    [Fact]
    public void SetModel_WithNull_ShouldThrowArgumentException()
    {
        // Arrange
        var config = new OllamaConfiguration { Model = "llama3" };
        using var provider = new OllamaProvider(config);

        // Act
        var act = () => provider.SetModel(null!);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Provider_ShouldImplementIModelListingProvider()
    {
        // Arrange
        var config = new OllamaConfiguration();
        using var provider = new OllamaProvider(config);

        // Assert
        provider.Should().BeAssignableTo<IModelListingProvider>();
    }

    [Fact]
    public void ProviderName_ShouldUpdateAfterSetModel()
    {
        // Arrange
        var config = new OllamaConfiguration { Model = "llama3" };
        using var provider = new OllamaProvider(config);

        // Act
        provider.SetModel("phi3");

        // Assert
        provider.ProviderName.Should().Contain("phi3");
    }

    [Fact]
    public async Task GetAvailableModelsAsync_WhenDisposed_ShouldThrowObjectDisposedException()
    {
        // Arrange
        var config = new OllamaConfiguration();
        var provider = new OllamaProvider(config);
        provider.Dispose();

        // Act
        var act = async () => await provider.GetAvailableModelsAsync();

        // Assert
        await act.Should().ThrowAsync<ObjectDisposedException>();
    }
}

