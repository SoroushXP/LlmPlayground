using FluentAssertions;
using LlmPlayground.Core;

namespace LlmPlayground.Core.Tests;

public class LmStudioConfigurationTests
{
    [Fact]
    public void DefaultValues_ShouldBeSetCorrectly()
    {
        // Arrange & Act
        var config = new LmStudioConfiguration();

        // Assert
        config.Host.Should().Be("localhost");
        config.Port.Should().Be(1234);
        config.Scheme.Should().Be("http");
        config.ApiPath.Should().Be("/v1");
        config.Model.Should().Be("local-model");
        config.SystemPrompt.Should().BeNull();
        config.TimeoutSeconds.Should().Be(300);
        config.BaseUrlOverride.Should().BeNull();
    }

    [Fact]
    public void BaseUrl_ShouldBeConstructedCorrectly()
    {
        // Arrange
        var config = new LmStudioConfiguration
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
        var config = new LmStudioConfiguration
        {
            Host = "secure-lmstudio.example.com",
            Port = 443,
            Scheme = "https"
        };

        // Act & Assert
        config.BaseUrl.Should().Be("https://secure-lmstudio.example.com:443/v1");
    }

    [Fact]
    public void BaseUrl_WithCustomApiPath_ShouldUseCustomPath()
    {
        // Arrange
        var config = new LmStudioConfiguration
        {
            Host = "localhost",
            Port = 1234,
            ApiPath = "/v2/openai"
        };

        // Act & Assert
        config.BaseUrl.Should().Be("http://localhost:1234/v2/openai");
    }

    [Fact]
    public void BaseUrl_WithOverride_ShouldIgnoreOtherProperties()
    {
        // Arrange
        var config = new LmStudioConfiguration
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
        var config = new LmStudioConfiguration
        {
            Host = "lmstudio-server",
            Port = 5678,
            Scheme = "https",
            ApiPath = "/v2",
            Model = "my-model",
            SystemPrompt = "You are a coding assistant.",
            TimeoutSeconds = 600
        };

        // Assert
        config.Host.Should().Be("lmstudio-server");
        config.Port.Should().Be(5678);
        config.Scheme.Should().Be("https");
        config.ApiPath.Should().Be("/v2");
        config.Model.Should().Be("my-model");
        config.SystemPrompt.Should().Be("You are a coding assistant.");
        config.TimeoutSeconds.Should().Be(600);
    }
}

public class LmStudioProviderTests
{
    [Fact]
    public void Constructor_WithNullConfiguration_ShouldThrowArgumentNullException()
    {
        // Arrange & Act
        var act = () => new LmStudioProvider(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("configuration");
    }

    [Fact]
    public void Constructor_WithValidConfiguration_ShouldNotThrow()
    {
        // Arrange
        var config = new LmStudioConfiguration();

        // Act
        var act = () => new LmStudioProvider(config);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void ProviderName_ShouldContainModelName()
    {
        // Arrange
        var config = new LmStudioConfiguration { Model = "llama-2-7b" };
        using var provider = new LmStudioProvider(config);

        // Act
        var name = provider.ProviderName;

        // Assert
        name.Should().Contain("LM Studio");
        name.Should().Contain("llama-2-7b");
    }

    [Fact]
    public void IsReady_BeforeInitialization_ShouldBeFalse()
    {
        // Arrange
        var config = new LmStudioConfiguration();
        using var provider = new LmStudioProvider(config);

        // Act & Assert
        provider.IsReady.Should().BeFalse();
    }

    [Fact]
    public async Task InitializeAsync_ShouldSetIsReadyToTrue()
    {
        // Arrange
        var config = new LmStudioConfiguration();
        using var provider = new LmStudioProvider(config);

        // Act
        await provider.InitializeAsync();

        // Assert
        provider.IsReady.Should().BeTrue();
    }

    [Fact]
    public void Dispose_ShouldNotThrow()
    {
        // Arrange
        var config = new LmStudioConfiguration();
        var provider = new LmStudioProvider(config);

        // Act
        var act = () => provider.Dispose();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void IsReady_AfterDispose_ShouldBeFalse()
    {
        // Arrange
        var config = new LmStudioConfiguration();
        var provider = new LmStudioProvider(config);
        provider.Dispose();

        // Act & Assert
        provider.IsReady.Should().BeFalse();
    }

    [Fact]
    public async Task CompleteAsync_AfterDispose_ShouldThrowObjectDisposedException()
    {
        // Arrange
        var config = new LmStudioConfiguration();
        var provider = new LmStudioProvider(config);
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
        var config = new LmStudioConfiguration();
        using var provider = new LmStudioProvider(config);

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
        var config = new LmStudioConfiguration { Model = "mistral-7b" };
        using var provider = new LmStudioProvider(config);

        // Act & Assert
        provider.CurrentModel.Should().Be("mistral-7b");
    }

    [Fact]
    public void SetModel_ShouldUpdateCurrentModel()
    {
        // Arrange
        var config = new LmStudioConfiguration { Model = "llama-2-7b" };
        using var provider = new LmStudioProvider(config);

        // Act
        provider.SetModel("codellama-13b");

        // Assert
        provider.CurrentModel.Should().Be("codellama-13b");
    }

    [Fact]
    public void SetModel_WithEmptyString_ShouldThrowArgumentException()
    {
        // Arrange
        var config = new LmStudioConfiguration { Model = "llama-2-7b" };
        using var provider = new LmStudioProvider(config);

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
        var config = new LmStudioConfiguration { Model = "llama-2-7b" };
        using var provider = new LmStudioProvider(config);

        // Act
        var act = () => provider.SetModel(null!);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Provider_ShouldImplementIModelListingProvider()
    {
        // Arrange
        var config = new LmStudioConfiguration();
        using var provider = new LmStudioProvider(config);

        // Assert
        provider.Should().BeAssignableTo<IModelListingProvider>();
    }

    [Fact]
    public void ProviderName_ShouldUpdateAfterSetModel()
    {
        // Arrange
        var config = new LmStudioConfiguration { Model = "llama-2-7b" };
        using var provider = new LmStudioProvider(config);

        // Act
        provider.SetModel("phi-2");

        // Assert
        provider.ProviderName.Should().Contain("phi-2");
    }

    [Fact]
    public async Task GetAvailableModelsAsync_WhenDisposed_ShouldThrowObjectDisposedException()
    {
        // Arrange
        var config = new LmStudioConfiguration();
        var provider = new LmStudioProvider(config);
        provider.Dispose();

        // Act
        var act = async () => await provider.GetAvailableModelsAsync();

        // Assert
        await act.Should().ThrowAsync<ObjectDisposedException>();
    }

    [Fact]
    public async Task InitializeAsync_WithEmptyModel_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var config = new LmStudioConfiguration { Model = "" };
        using var provider = new LmStudioProvider(config);

        // Act
        var act = () => provider.InitializeAsync();

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Model name is not set*");
    }
}

