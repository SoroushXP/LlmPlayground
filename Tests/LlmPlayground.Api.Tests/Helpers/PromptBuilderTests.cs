using FluentAssertions;
using LlmPlayground.Api.Helpers;
using LlmPlayground.Api.Models;
using Xunit;

namespace LlmPlayground.Api.Tests.Helpers;

public class PromptBuilderTests
{
    [Fact]
    public void BuildGameIdeaPrompt_WithThemeAndDescription_ReplacesPlaceholders()
    {
        // Arrange
        var template = "Create a game. {ThemeSection} {DescriptionSection}";
        var request = new GameGenerationRequest
        {
            Theme = "mystery",
            Description = "Include puzzles"
        };

        // Act
        var result = PromptBuilder.BuildGameIdeaPrompt(template, request);

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
        var request = new GameGenerationRequest
        {
            Theme = null,
            Description = "Test description"
        };

        // Act
        var result = PromptBuilder.BuildGameIdeaPrompt(template, request);

        // Assert
        result.Should().NotContain("{ThemeSection}");
        result.Should().Contain("Test description");
    }

    [Fact]
    public void BuildGameIdeaPrompt_WithNoDescription_RemovesDescriptionPlaceholder()
    {
        // Arrange
        var template = "Create a game. {ThemeSection} {DescriptionSection}";
        var request = new GameGenerationRequest
        {
            Theme = "adventure",
            Description = null
        };

        // Act
        var result = PromptBuilder.BuildGameIdeaPrompt(template, request);

        // Assert
        result.Should().Contain("adventure");
        result.Should().NotContain("{DescriptionSection}");
    }

    [Fact]
    public void BuildGameIdeaPrompt_WithBothEmpty_RemovesBothPlaceholders()
    {
        // Arrange
        var template = "Create a game. {ThemeSection} {DescriptionSection}";
        var request = new GameGenerationRequest();

        // Act
        var result = PromptBuilder.BuildGameIdeaPrompt(template, request);

        // Assert
        result.Should().NotContain("{ThemeSection}");
        result.Should().NotContain("{DescriptionSection}");
    }

    [Fact]
    public void BuildGameIdeaPrompt_TrimsResult()
    {
        // Arrange
        var template = "  Create game {ThemeSection}  ";
        var request = new GameGenerationRequest { Theme = "test" };

        // Act
        var result = PromptBuilder.BuildGameIdeaPrompt(template, request);

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
    public void ValidateTemplate_WithAllPlaceholders_ReturnsEmpty()
    {
        // Arrange
        var template = "Hello {Name}, welcome to {Place}!";

        // Act
        var missing = PromptBuilder.ValidateTemplate(template, "Name", "Place");

        // Assert
        missing.Should().BeEmpty();
    }

    [Fact]
    public void ValidateTemplate_WithMissingPlaceholder_ReturnsMissing()
    {
        // Arrange
        var template = "Hello {Name}!";

        // Act
        var missing = PromptBuilder.ValidateTemplate(template, "Name", "Place");

        // Assert
        missing.Should().ContainSingle("Place");
    }

    [Fact]
    public void ValidateTemplate_WithAllMissing_ReturnsAll()
    {
        // Arrange
        var template = "Plain text";

        // Act
        var missing = PromptBuilder.ValidateTemplate(template, "A", "B", "C");

        // Assert
        missing.Should().HaveCount(3);
        missing.Should().Contain("A");
        missing.Should().Contain("B");
        missing.Should().Contain("C");
    }

    [Fact]
    public void ValidateTemplate_IsCaseInsensitive()
    {
        // Arrange
        var template = "Hello {NAME}!";

        // Act
        var missing = PromptBuilder.ValidateTemplate(template, "name");

        // Assert
        missing.Should().BeEmpty();
    }
}

