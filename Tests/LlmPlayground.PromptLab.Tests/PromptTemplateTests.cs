using FluentAssertions;
using LlmPlayground.PromptLab;

namespace LlmPlayground.PromptLab.Tests;

public class PromptTemplateTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidTemplate_ShouldCreateInstance()
    {
        // Arrange & Act
        var template = new PromptTemplate("Hello, {{name}}!");

        // Assert
        template.Should().NotBeNull();
        template.Template.Should().Be("Hello, {{name}}!");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidTemplate_ShouldThrow(string? template)
    {
        // Act
        var act = () => new PromptTemplate(template!);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    #endregion

    #region Variable Extraction Tests

    [Fact]
    public void Variables_WithSingleVariable_ShouldExtractCorrectly()
    {
        // Arrange
        var template = new PromptTemplate("Hello, {{name}}!");

        // Assert
        template.Variables.Should().ContainSingle()
            .Which.Should().Be("name");
    }

    [Fact]
    public void Variables_WithMultipleVariables_ShouldExtractAll()
    {
        // Arrange
        var template = new PromptTemplate("{{greeting}}, {{name}}! Welcome to {{place}}.");

        // Assert
        template.Variables.Should().HaveCount(3);
        template.Variables.Should().Contain(["greeting", "name", "place"]);
    }

    [Fact]
    public void Variables_WithNoVariables_ShouldReturnEmpty()
    {
        // Arrange
        var template = new PromptTemplate("Hello, World!");

        // Assert
        template.Variables.Should().BeEmpty();
    }

    [Fact]
    public void Variables_WithDuplicateVariables_ShouldReturnUnique()
    {
        // Arrange
        var template = new PromptTemplate("{{name}} said hello to {{name}}");

        // Assert
        template.Variables.Should().ContainSingle()
            .Which.Should().Be("name");
    }

    #endregion

    #region Render Tests

    [Fact]
    public void Render_WithDictionary_ShouldSubstituteVariables()
    {
        // Arrange
        var template = new PromptTemplate("Hello, {{name}}! You are {{age}} years old.");
        var variables = new Dictionary<string, string>
        {
            ["name"] = "Alice",
            ["age"] = "30"
        };

        // Act
        var result = template.Render(variables);

        // Assert
        result.Should().Be("Hello, Alice! You are 30 years old.");
    }

    [Fact]
    public void Render_WithAnonymousObject_ShouldSubstituteVariables()
    {
        // Arrange
        var template = new PromptTemplate("Explain {{concept}} in {{language}}");

        // Act
        var result = template.Render(new { concept = "recursion", language = "simple terms" });

        // Assert
        result.Should().Be("Explain recursion in simple terms");
    }

    [Fact]
    public void Render_WithMissingVariable_ShouldLeavePlaceholder()
    {
        // Arrange
        var template = new PromptTemplate("Hello, {{name}}! Your id is {{id}}.");
        var variables = new Dictionary<string, string>
        {
            ["name"] = "Bob"
        };

        // Act
        var result = template.Render(variables);

        // Assert
        result.Should().Be("Hello, Bob! Your id is {{id}}.");
    }

    [Fact]
    public void Render_WithExtraVariables_ShouldIgnoreExtras()
    {
        // Arrange
        var template = new PromptTemplate("Hello, {{name}}!");
        var variables = new Dictionary<string, string>
        {
            ["name"] = "Charlie",
            ["unused"] = "value"
        };

        // Act
        var result = template.Render(variables);

        // Assert
        result.Should().Be("Hello, Charlie!");
    }

    [Fact]
    public void Render_WithEmptyValue_ShouldReplaceWithEmpty()
    {
        // Arrange
        var template = new PromptTemplate("Hello, {{name}}!");
        var variables = new Dictionary<string, string>
        {
            ["name"] = ""
        };

        // Act
        var result = template.Render(variables);

        // Assert
        result.Should().Be("Hello, !");
    }

    #endregion

    #region GetMissingVariables Tests

    [Fact]
    public void GetMissingVariables_WithAllProvided_ShouldReturnEmpty()
    {
        // Arrange
        var template = new PromptTemplate("{{a}} and {{b}}");
        var variables = new Dictionary<string, string>
        {
            ["a"] = "1",
            ["b"] = "2"
        };

        // Act
        var missing = template.GetMissingVariables(variables);

        // Assert
        missing.Should().BeEmpty();
    }

    [Fact]
    public void GetMissingVariables_WithSomeMissing_ShouldReturnMissing()
    {
        // Arrange
        var template = new PromptTemplate("{{a}}, {{b}}, {{c}}");
        var variables = new Dictionary<string, string>
        {
            ["a"] = "1"
        };

        // Act
        var missing = template.GetMissingVariables(variables);

        // Assert
        missing.Should().HaveCount(2);
        missing.Should().Contain(["b", "c"]);
    }

    #endregion

    #region Implicit Conversion Tests

    [Fact]
    public void ImplicitConversion_FromString_ShouldWork()
    {
        // Arrange & Act
        PromptTemplate template = "Hello, {{name}}!";

        // Assert
        template.Template.Should().Be("Hello, {{name}}!");
        template.Variables.Should().Contain("name");
    }

    #endregion

    #region File Loading Tests

    [Fact]
    public async Task FromFileAsync_WithValidFile_ShouldLoadTemplate()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        await File.WriteAllTextAsync(tempFile, "Hello, {{name}}!");

        try
        {
            // Act
            var template = await PromptTemplate.FromFileAsync(tempFile);

            // Assert
            template.Template.Should().Be("Hello, {{name}}!");
            template.Variables.Should().Contain("name");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void FromFile_WithValidFile_ShouldLoadTemplate()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        File.WriteAllText(tempFile, "Explain {{topic}}");

        try
        {
            // Act
            var template = PromptTemplate.FromFile(tempFile);

            // Assert
            template.Template.Should().Be("Explain {{topic}}");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    #endregion
}

