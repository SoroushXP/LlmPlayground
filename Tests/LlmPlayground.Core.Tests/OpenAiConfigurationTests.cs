using FluentAssertions;
using LlmPlayground.Core;

namespace LlmPlayground.Core.Tests;

public class OpenAiConfigurationTests
{
    [Fact]
    public void DefaultValues_ShouldBeSetCorrectly()
    {
        // Arrange & Act
        var config = new OpenAiConfiguration
        {
            ApiKey = "test-api-key"
        };

        // Assert
        config.Model.Should().Be("gpt-4o-mini");
        config.SystemPrompt.Should().BeNull();
        config.BaseUrl.Should().BeNull();
        config.TimeoutSeconds.Should().Be(120);
    }

    [Fact]
    public void ApiKey_ShouldBeRequired()
    {
        // Arrange & Act
        var config = new OpenAiConfiguration
        {
            ApiKey = "sk-test123"
        };

        // Assert
        config.ApiKey.Should().Be("sk-test123");
    }

    [Fact]
    public void AllProperties_ShouldBeConfigurable()
    {
        // Arrange & Act
        var config = new OpenAiConfiguration
        {
            ApiKey = "sk-key",
            Model = "gpt-4",
            SystemPrompt = "You are a helpful assistant.",
            BaseUrl = "https://custom.openai.azure.com",
            TimeoutSeconds = 60
        };

        // Assert
        config.ApiKey.Should().Be("sk-key");
        config.Model.Should().Be("gpt-4");
        config.SystemPrompt.Should().Be("You are a helpful assistant.");
        config.BaseUrl.Should().Be("https://custom.openai.azure.com");
        config.TimeoutSeconds.Should().Be(60);
    }

    [Fact]
    public void Record_ShouldSupportWithExpression()
    {
        // Arrange
        var original = new OpenAiConfiguration
        {
            ApiKey = "key1",
            Model = "gpt-3.5-turbo"
        };

        // Act
        var modified = original with { Model = "gpt-4o" };

        // Assert
        modified.Model.Should().Be("gpt-4o");
        modified.ApiKey.Should().Be("key1");
        original.Model.Should().Be("gpt-3.5-turbo"); // Original unchanged
    }

    [Fact]
    public void Record_ShouldHaveValueEquality()
    {
        // Arrange
        var config1 = new OpenAiConfiguration
        {
            ApiKey = "key",
            Model = "gpt-4"
        };
        var config2 = new OpenAiConfiguration
        {
            ApiKey = "key",
            Model = "gpt-4"
        };

        // Assert
        config1.Should().Be(config2);
        config1.GetHashCode().Should().Be(config2.GetHashCode());
    }

    [Theory]
    [InlineData("gpt-4")]
    [InlineData("gpt-4o")]
    [InlineData("gpt-4o-mini")]
    [InlineData("gpt-3.5-turbo")]
    [InlineData("o1-preview")]
    public void Model_ShouldAcceptVariousModelNames(string modelName)
    {
        // Arrange & Act
        var config = new OpenAiConfiguration
        {
            ApiKey = "key",
            Model = modelName
        };

        // Assert
        config.Model.Should().Be(modelName);
    }
}

