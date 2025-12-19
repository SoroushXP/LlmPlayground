using FluentAssertions;
using LlmPlayground.Core;

namespace LlmPlayground.Core.Tests;

public class LlmInferenceOptionsTests
{
    [Fact]
    public void DefaultValues_ShouldBeSetCorrectly()
    {
        // Arrange & Act
        var options = new LlmInferenceOptions();

        // Assert
        options.MaxTokens.Should().Be(256);
        options.Temperature.Should().Be(0.7f);
        options.TopP.Should().Be(0.9f);
        options.RepeatPenalty.Should().Be(1.1f);
    }

    [Fact]
    public void WithCustomValues_ShouldOverrideDefaults()
    {
        // Arrange & Act
        var options = new LlmInferenceOptions
        {
            MaxTokens = 512,
            Temperature = 0.5f,
            TopP = 0.8f,
            RepeatPenalty = 1.2f
        };

        // Assert
        options.MaxTokens.Should().Be(512);
        options.Temperature.Should().Be(0.5f);
        options.TopP.Should().Be(0.8f);
        options.RepeatPenalty.Should().Be(1.2f);
    }

    [Fact]
    public void Record_ShouldSupportWithExpression()
    {
        // Arrange
        var original = new LlmInferenceOptions { MaxTokens = 100 };

        // Act
        var modified = original with { Temperature = 0.3f };

        // Assert
        modified.MaxTokens.Should().Be(100);
        modified.Temperature.Should().Be(0.3f);
        original.Temperature.Should().Be(0.7f); // Original unchanged
    }

    [Fact]
    public void Record_ShouldHaveValueEquality()
    {
        // Arrange
        var options1 = new LlmInferenceOptions { MaxTokens = 256, Temperature = 0.7f };
        var options2 = new LlmInferenceOptions { MaxTokens = 256, Temperature = 0.7f };

        // Assert
        options1.Should().Be(options2);
        options1.GetHashCode().Should().Be(options2.GetHashCode());
    }
}

