using FluentAssertions;
using LlmPlayground.Core;
using LlmPlayground.PromptLab;
using Microsoft.Extensions.Configuration;
using NSubstitute;

namespace LlmPlayground.PromptLab.Tests;

public class PromptLabFactoryTests
{
    #region CreateProvider Tests

    [Theory]
    [InlineData("ollama")]
    [InlineData("Ollama")]
    [InlineData("OLLAMA")]
    public void CreateProvider_WithOllama_ShouldCreateOllamaProvider(string providerName)
    {
        // Arrange
        var config = CreateConfiguration(new Dictionary<string, string?>
        {
            ["Ollama:Host"] = "localhost",
            ["Ollama:Port"] = "11434"
        });

        // Act
        var provider = PromptLabFactory.CreateProvider(providerName, config);

        // Assert
        provider.Should().BeOfType<OllamaProvider>();
    }

    [Theory]
    [InlineData("lmstudio")]
    [InlineData("LmStudio")]
    [InlineData("lm-studio")]
    public void CreateProvider_WithLmStudio_ShouldCreateLmStudioProvider(string providerName)
    {
        // Arrange
        var config = CreateConfiguration(new Dictionary<string, string?>
        {
            ["LmStudio:Host"] = "localhost",
            ["LmStudio:Port"] = "1234"
        });

        // Act
        var provider = PromptLabFactory.CreateProvider(providerName, config);

        // Assert
        provider.Should().BeOfType<LmStudioProvider>();
    }

    [Fact]
    public void CreateProvider_WithOpenAI_ShouldCreateOpenAiProvider()
    {
        // Arrange
        var config = CreateConfiguration(new Dictionary<string, string?>
        {
            ["OpenAI:ApiKey"] = "test-key",
            ["OpenAI:Model"] = "gpt-4"
        });

        // Act
        var provider = PromptLabFactory.CreateProvider("openai", config);

        // Assert
        provider.Should().BeOfType<OpenAiProvider>();
    }

    [Fact]
    public void CreateProvider_WithUnknownProvider_ShouldThrow()
    {
        // Arrange
        var config = CreateConfiguration(new Dictionary<string, string?>());

        // Act
        var act = () => PromptLabFactory.CreateProvider("unknown", config);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Unknown provider*");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateProvider_WithInvalidProviderName_ShouldThrow(string? providerName)
    {
        // Arrange
        var config = CreateConfiguration(new Dictionary<string, string?>());

        // Act
        var act = () => PromptLabFactory.CreateProvider(providerName!, config);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CreateProvider_WithNullConfig_ShouldThrow()
    {
        // Act
        var act = () => PromptLabFactory.CreateProvider("ollama", null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region CreateOllamaProvider Tests

    [Fact]
    public void CreateOllamaProvider_WithConfiguration_ShouldUseConfigValues()
    {
        // Arrange
        var config = CreateConfiguration(new Dictionary<string, string?>
        {
            ["Ollama:Host"] = "custom-host",
            ["Ollama:Port"] = "9999",
            ["Ollama:Model"] = "custom-model",
            ["Ollama:TimeoutSeconds"] = "600"
        });

        // Act
        var provider = PromptLabFactory.CreateOllamaProvider(config);

        // Assert
        provider.Should().NotBeNull();
        provider.ProviderName.Should().Contain("custom-model");
    }

    [Fact]
    public void CreateOllamaProvider_WithDefaults_ShouldUseDefaultValues()
    {
        // Arrange
        var config = CreateConfiguration(new Dictionary<string, string?>());

        // Act
        var provider = PromptLabFactory.CreateOllamaProvider(config);

        // Assert
        provider.Should().NotBeNull();
        provider.ProviderName.Should().Contain("llama3");
    }

    #endregion

    #region CreateLmStudioProvider Tests

    [Fact]
    public void CreateLmStudioProvider_WithConfiguration_ShouldUseConfigValues()
    {
        // Arrange
        var config = CreateConfiguration(new Dictionary<string, string?>
        {
            ["LmStudio:Host"] = "custom-host",
            ["LmStudio:Port"] = "5678",
            ["LmStudio:Model"] = "my-model"
        });

        // Act
        var provider = PromptLabFactory.CreateLmStudioProvider(config);

        // Assert
        provider.Should().NotBeNull();
        provider.ProviderName.Should().Contain("my-model");
    }

    #endregion

    #region CreateOpenAiProvider Tests

    [Fact]
    public void CreateOpenAiProvider_WithValidApiKey_ShouldCreateProvider()
    {
        // Arrange
        var config = CreateConfiguration(new Dictionary<string, string?>
        {
            ["OpenAI:ApiKey"] = "sk-test123",
            ["OpenAI:Model"] = "gpt-4o"
        });

        // Act
        var provider = PromptLabFactory.CreateOpenAiProvider(config);

        // Assert
        provider.Should().NotBeNull();
        provider.ProviderName.Should().Contain("gpt-4o");
    }

    [Fact]
    public void CreateOpenAiProvider_WithMissingApiKey_ShouldThrow()
    {
        // Arrange
        var config = CreateConfiguration(new Dictionary<string, string?>
        {
            ["OpenAI:Model"] = "gpt-4"
        });

        // Act
        var act = () => PromptLabFactory.CreateOpenAiProvider(config);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*API key is required*");
    }

    #endregion

    #region CreateSession Tests

    [Fact]
    public void CreateSession_WithProvider_ShouldCreateSession()
    {
        // Arrange
        var mockProvider = Substitute.For<ILlmProvider>();

        // Act
        var session = PromptLabFactory.CreateSession(mockProvider);

        // Assert
        session.Should().NotBeNull();
        session.Provider.Should().Be(mockProvider);
    }

    [Fact]
    public void CreateSession_WithSystemPrompt_ShouldSetSystemPrompt()
    {
        // Arrange
        var mockProvider = Substitute.For<ILlmProvider>();

        // Act
        var session = PromptLabFactory.CreateSession(mockProvider, "Be helpful");

        // Assert
        session.SystemPrompt.Should().Be("Be helpful");
    }

    [Fact]
    public void CreateSession_WithOptions_ShouldSetOptions()
    {
        // Arrange
        var mockProvider = Substitute.For<ILlmProvider>();
        var options = new LlmInferenceOptions { Temperature = 0.5f };

        // Act
        var session = PromptLabFactory.CreateSession(mockProvider, options: options);

        // Assert
        session.Options.Temperature.Should().Be(0.5f);
    }

    #endregion

    #region CreateInferenceOptions Tests

    [Fact]
    public void CreateInferenceOptions_WithConfiguration_ShouldUseConfigValues()
    {
        // Arrange
        var config = CreateConfiguration(new Dictionary<string, string?>
        {
            ["Inference:MaxTokens"] = "2048",
            ["Inference:Temperature"] = "0.5",
            ["Inference:TopP"] = "0.95",
            ["Inference:RepeatPenalty"] = "1.2"
        });

        // Act
        var options = PromptLabFactory.CreateInferenceOptions(config);

        // Assert
        options.MaxTokens.Should().Be(2048);
        options.Temperature.Should().Be(0.5f);
        options.TopP.Should().Be(0.95f);
        options.RepeatPenalty.Should().Be(1.2f);
    }

    [Fact]
    public void CreateInferenceOptions_WithDefaults_ShouldUseDefaultValues()
    {
        // Arrange
        var config = CreateConfiguration(new Dictionary<string, string?>());

        // Act
        var options = PromptLabFactory.CreateInferenceOptions(config);

        // Assert
        options.MaxTokens.Should().Be(4096);
        options.Temperature.Should().Be(0.7f);
        options.TopP.Should().Be(0.9f);
        options.RepeatPenalty.Should().Be(1.1f);
    }

    #endregion

    #region Helper Methods

    private static IConfiguration CreateConfiguration(Dictionary<string, string?> values)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }

    #endregion
}

