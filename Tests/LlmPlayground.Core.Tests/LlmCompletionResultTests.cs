using FluentAssertions;
using LlmPlayground.Core;

namespace LlmPlayground.Core.Tests;

public class LlmCompletionResultTests
{
    [Fact]
    public void Constructor_ShouldSetAllProperties()
    {
        // Arrange
        var text = "Hello, world!";
        var tokensGenerated = 5;
        var duration = TimeSpan.FromMilliseconds(150);

        // Act
        var result = new LlmCompletionResult(text, tokensGenerated, duration);

        // Assert
        result.Text.Should().Be(text);
        result.TokensGenerated.Should().Be(tokensGenerated);
        result.Duration.Should().Be(duration);
    }

    [Fact]
    public void Record_ShouldHaveValueEquality()
    {
        // Arrange
        var duration = TimeSpan.FromSeconds(1);
        var result1 = new LlmCompletionResult("test", 10, duration);
        var result2 = new LlmCompletionResult("test", 10, duration);

        // Assert
        result1.Should().Be(result2);
        result1.GetHashCode().Should().Be(result2.GetHashCode());
    }

    [Fact]
    public void Record_ShouldSupportDeconstruction()
    {
        // Arrange
        var result = new LlmCompletionResult("output", 3, TimeSpan.FromSeconds(2));

        // Act
        var (text, tokens, duration) = result;

        // Assert
        text.Should().Be("output");
        tokens.Should().Be(3);
        duration.Should().Be(TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void Record_ShouldSupportWithExpression()
    {
        // Arrange
        var original = new LlmCompletionResult("original", 5, TimeSpan.FromSeconds(1));

        // Act
        var modified = original with { Text = "modified" };

        // Assert
        modified.Text.Should().Be("modified");
        modified.TokensGenerated.Should().Be(5);
        original.Text.Should().Be("original"); // Original unchanged
    }

    [Fact]
    public void EmptyResult_ShouldBeValid()
    {
        // Arrange & Act
        var result = new LlmCompletionResult("", 0, TimeSpan.Zero);

        // Assert
        result.Text.Should().BeEmpty();
        result.TokensGenerated.Should().Be(0);
        result.Duration.Should().Be(TimeSpan.Zero);
    }
}

