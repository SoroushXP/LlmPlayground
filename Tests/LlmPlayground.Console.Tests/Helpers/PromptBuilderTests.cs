using FluentAssertions;
using LlmPlayground.Console.Helpers;
using Xunit;

namespace LlmPlayground.Console.Tests.Helpers;

public class PromptBuilderTests
{
    [Fact]
    public void BuildGameIdeaPrompt_WithThemeAndDescription_ReplacesPlaceholders()
    {
        // Arrange
        var template = "Create a game. {ThemeSection} {DescriptionSection}";

        // Act
        var result = PromptBuilder.BuildGameIdeaPrompt(template, "mystery", "Include puzzles");

        // Assert
        result.Should().Contain("mystery");
        result.Should().Contain("Include puzzles");
        result.Should().NotContain("{ThemeSection}");
        result.Should().NotContain("{DescriptionSection}");
    }

    [Fact]
    public void BuildGameIdeaPrompt_WithNoTheme_RemovesThemePlaceholder()
    {
        // Arrange
        var template = "Create a game. {ThemeSection} {DescriptionSection}";

        // Act
        var result = PromptBuilder.BuildGameIdeaPrompt(template, null, "Test description");

        // Assert
        result.Should().NotContain("{ThemeSection}");
        result.Should().Contain("Test description");
    }

    [Fact]
    public void BuildGameIdeaPrompt_WithNoDescription_RemovesDescriptionPlaceholder()
    {
        // Arrange
        var template = "Create a game. {ThemeSection} {DescriptionSection}";

        // Act
        var result = PromptBuilder.BuildGameIdeaPrompt(template, "adventure", null);

        // Assert
        result.Should().Contain("adventure");
        result.Should().NotContain("{DescriptionSection}");
    }

    [Fact]
    public void BuildGameIdeaPrompt_WithBothEmpty_RemovesBothPlaceholders()
    {
        // Arrange
        var template = "Create a game. {ThemeSection} {DescriptionSection}";

        // Act
        var result = PromptBuilder.BuildGameIdeaPrompt(template, null, null);

        // Assert
        result.Should().NotContain("{ThemeSection}");
        result.Should().NotContain("{DescriptionSection}");
    }

    [Fact]
    public void BuildGameIdeaPrompt_TrimsResult()
    {
        // Arrange
        var template = "  Create game {ThemeSection}  ";

        // Act
        var result = PromptBuilder.BuildGameIdeaPrompt(template, "test", null);

        // Assert
        result.Should().NotStartWith(" ");
        result.Should().NotEndWith(" ");
    }

    [Fact]
    public void BuildPrologCodePrompt_ReplacesGameIdea()
    {
        // Arrange
        var template = "Implement this: {GameIdea}";
        var gameIdea = "A puzzle about colors";

        // Act
        var result = PromptBuilder.BuildPrologCodePrompt(template, gameIdea);

        // Assert
        result.Should().Contain("A puzzle about colors");
        result.Should().NotContain("{GameIdea}");
    }

    [Fact]
    public void BuildPrologCodePrompt_TrimsResult()
    {
        // Arrange
        var template = "  {GameIdea}  ";
        var gameIdea = "test";

        // Act
        var result = PromptBuilder.BuildPrologCodePrompt(template, gameIdea);

        // Assert
        result.Should().Be("test");
    }

    [Fact]
    public void BuildPrologFixPrompt_ReplacesAllPlaceholders()
    {
        // Arrange
        var template = "Fix this code: {PrologCode} Errors: {Errors}";
        var prologCode = "main :- undefined.";
        var errors = "Undefined predicate";

        // Act
        var result = PromptBuilder.BuildPrologFixPrompt(template, prologCode, errors);

        // Assert
        result.Should().Contain("main :- undefined.");
        result.Should().Contain("Undefined predicate");
        result.Should().NotContain("{PrologCode}");
        result.Should().NotContain("{Errors}");
    }

    [Fact]
    public void BuildPrologFixPrompt_TrimsResult()
    {
        // Arrange
        var template = "  {PrologCode} {Errors}  ";

        // Act
        var result = PromptBuilder.BuildPrologFixPrompt(template, "code", "error");

        // Assert
        result.Should().NotStartWith(" ");
        result.Should().NotEndWith(" ");
    }
}

