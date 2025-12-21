using FluentAssertions;
using LlmPlayground.PromptLab;

namespace LlmPlayground.PromptLab.Tests;

public class PromptBuilderTests
{
    #region Basic Append Tests

    [Fact]
    public void Append_WithText_ShouldAddToPrompt()
    {
        // Arrange
        var builder = new PromptBuilder();

        // Act
        var result = builder.Append("Hello").Build();

        // Assert
        result.Should().Be("Hello");
    }

    [Fact]
    public void Append_MultipleTimes_ShouldConcatenate()
    {
        // Arrange
        var builder = new PromptBuilder();

        // Act
        var result = builder
            .Append("Hello")
            .Append(", ")
            .Append("World!")
            .Build();

        // Assert
        result.Should().Be("Hello, World!");
    }

    [Fact]
    public void AppendLine_ShouldAddNewline()
    {
        // Arrange
        var builder = new PromptBuilder();

        // Act
        var result = builder
            .AppendLine("Line 1")
            .AppendLine("Line 2")
            .Build();

        // Assert
        result.Should().Be($"Line 1{Environment.NewLine}Line 2{Environment.NewLine}");
    }

    [Fact]
    public void AppendLine_WithNoArgs_ShouldAddEmptyLine()
    {
        // Arrange
        var builder = new PromptBuilder();

        // Act
        var result = builder
            .Append("Before")
            .AppendLine()
            .Append("After")
            .Build();

        // Assert
        result.Should().Be($"Before{Environment.NewLine}After");
    }

    #endregion

    #region Code Block Tests

    [Fact]
    public void AppendCodeBlock_WithLanguage_ShouldFormatCorrectly()
    {
        // Arrange
        var builder = new PromptBuilder();
        var code = "Console.WriteLine(\"Hello\");";

        // Act
        var result = builder.AppendCodeBlock(code, "csharp").Build();

        // Assert
        result.Should().Contain("```csharp");
        result.Should().Contain(code);
        result.Should().EndWith($"```{Environment.NewLine}");
    }

    [Fact]
    public void AppendCodeBlock_WithoutLanguage_ShouldFormatCorrectly()
    {
        // Arrange
        var builder = new PromptBuilder();
        var code = "some code";

        // Act
        var result = builder.AppendCodeBlock(code).Build();

        // Assert
        result.Should().StartWith("```" + Environment.NewLine);
        result.Should().Contain(code);
    }

    #endregion

    #region List Tests

    [Fact]
    public void AppendList_ShouldFormatWithPrefix()
    {
        // Arrange
        var builder = new PromptBuilder();
        var items = new[] { "Item 1", "Item 2", "Item 3" };

        // Act
        var result = builder.AppendList(items).Build();

        // Assert
        result.Should().Contain("- Item 1");
        result.Should().Contain("- Item 2");
        result.Should().Contain("- Item 3");
    }

    [Fact]
    public void AppendList_WithCustomPrefix_ShouldUsePrefix()
    {
        // Arrange
        var builder = new PromptBuilder();
        var items = new[] { "One", "Two" };

        // Act
        var result = builder.AppendList(items, "* ").Build();

        // Assert
        result.Should().Contain("* One");
        result.Should().Contain("* Two");
    }

    [Fact]
    public void AppendNumberedList_ShouldNumberItems()
    {
        // Arrange
        var builder = new PromptBuilder();
        var items = new[] { "First", "Second", "Third" };

        // Act
        var result = builder.AppendNumberedList(items).Build();

        // Assert
        result.Should().Contain("1. First");
        result.Should().Contain("2. Second");
        result.Should().Contain("3. Third");
    }

    #endregion

    #region Conditional Append Tests

    [Fact]
    public void AppendIf_WhenTrue_ShouldAppend()
    {
        // Arrange
        var builder = new PromptBuilder();

        // Act
        var result = builder
            .Append("Always")
            .AppendIf(true, " Sometimes")
            .Build();

        // Assert
        result.Should().Be("Always Sometimes");
    }

    [Fact]
    public void AppendIf_WhenFalse_ShouldNotAppend()
    {
        // Arrange
        var builder = new PromptBuilder();

        // Act
        var result = builder
            .Append("Always")
            .AppendIf(false, " Never")
            .Build();

        // Assert
        result.Should().Be("Always");
    }

    [Fact]
    public void AppendIf_WithFactory_WhenTrue_ShouldInvokeFactory()
    {
        // Arrange
        var builder = new PromptBuilder();
        var factoryInvoked = false;

        // Act
        var result = builder
            .AppendIf(true, () =>
            {
                factoryInvoked = true;
                return "From Factory";
            })
            .Build();

        // Assert
        factoryInvoked.Should().BeTrue();
        result.Should().Be("From Factory");
    }

    [Fact]
    public void AppendIf_WithFactory_WhenFalse_ShouldNotInvokeFactory()
    {
        // Arrange
        var builder = new PromptBuilder();
        var factoryInvoked = false;

        // Act
        var result = builder
            .AppendIf(false, () =>
            {
                factoryInvoked = true;
                return "Never";
            })
            .Build();

        // Assert
        factoryInvoked.Should().BeFalse();
        result.Should().BeEmpty();
    }

    #endregion

    #region System Prompt Tests

    [Fact]
    public void WithSystem_ShouldSetSystemPrompt()
    {
        // Arrange
        var builder = new PromptBuilder();

        // Act
        builder.WithSystem("You are a helpful assistant.");

        // Assert
        builder.SystemPrompt.Should().Be("You are a helpful assistant.");
    }

    [Fact]
    public void SystemPrompt_WhenNotSet_ShouldBeNull()
    {
        // Arrange
        var builder = new PromptBuilder();

        // Assert
        builder.SystemPrompt.Should().BeNull();
    }

    #endregion

    #region Fluent API Tests

    [Fact]
    public void FluentApi_ShouldChainCorrectly()
    {
        // Arrange & Act
        var result = new PromptBuilder()
            .WithSystem("Be helpful")
            .AppendLine("Please analyze this code:")
            .AppendCodeBlock("var x = 1;", "csharp")
            .AppendLine("Consider:")
            .AppendNumberedList(["Performance", "Readability"])
            .Build();

        // Assert
        result.Should().Contain("Please analyze this code:");
        result.Should().Contain("```csharp");
        result.Should().Contain("var x = 1;");
        result.Should().Contain("1. Performance");
        result.Should().Contain("2. Readability");
    }

    [Fact]
    public void ToString_ShouldReturnSameAsBuild()
    {
        // Arrange
        var builder = new PromptBuilder().Append("Test");

        // Act & Assert
        builder.ToString().Should().Be(builder.Build());
    }

    #endregion
}

