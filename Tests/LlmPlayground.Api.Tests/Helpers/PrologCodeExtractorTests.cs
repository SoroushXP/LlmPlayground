using FluentAssertions;
using LlmPlayground.Api.Helpers;
using Xunit;

namespace LlmPlayground.Api.Tests.Helpers;

public class PrologCodeExtractorTests
{
    [Fact]
    public void ExtractPrologCode_WithPrologCodeBlock_ExtractsCode()
    {
        // Arrange
        var response = """
            Here is the Prolog code:

            ```prolog
            parent(tom, bob).
            parent(bob, pat).
            grandparent(X, Z) :- parent(X, Y), parent(Y, Z).
            main :- grandparent(tom, pat), write('Tom is Pat''s grandparent'), nl.
            ```

            This implements a simple family relationship.
            """;

        // Act
        var result = PrologCodeExtractor.ExtractPrologCode(response);

        // Assert
        result.Should().Contain("parent(tom, bob).");
        result.Should().Contain("grandparent(X, Z)");
        result.Should().Contain("main :-");
        result.Should().NotContain("```");
        result.Should().NotContain("Here is the Prolog code");
    }

    [Fact]
    public void ExtractPrologCode_WithPlCodeBlock_ExtractsCode()
    {
        // Arrange
        var response = """
            ```pl
            fact(a).
            fact(b).
            ```
            """;

        // Act
        var result = PrologCodeExtractor.ExtractPrologCode(response);

        // Assert
        result.Should().Contain("fact(a).");
        result.Should().Contain("fact(b).");
    }

    [Fact]
    public void ExtractPrologCode_WithGenericCodeBlock_ExtractsCode()
    {
        // Arrange
        var response = """
            ```
            likes(mary, food).
            likes(john, wine).
            ```
            """;

        // Act
        var result = PrologCodeExtractor.ExtractPrologCode(response);

        // Assert
        result.Should().Contain("likes(mary, food).");
    }

    [Fact]
    public void ExtractPrologCode_WithNoCodeBlock_ReturnsTrimmedInput()
    {
        // Arrange
        var response = "  Just some text  ";

        // Act
        var result = PrologCodeExtractor.ExtractPrologCode(response);

        // Assert
        result.Should().Be("Just some text");
    }

    [Fact]
    public void ExtractPrologCode_WithNullInput_ReturnsEmpty()
    {
        // Act
        var result = PrologCodeExtractor.ExtractPrologCode(null);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void ExtractPrologCode_WithEmptyInput_ReturnsEmpty()
    {
        // Act
        var result = PrologCodeExtractor.ExtractPrologCode("   ");

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void IsPrologCode_WithValidFact_ReturnsTrue()
    {
        // Arrange
        var code = "parent(tom, bob).";

        // Act
        var result = PrologCodeExtractor.IsPrologCode(code);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsPrologCode_WithValidRule_ReturnsTrue()
    {
        // Arrange
        var code = "grandparent(X, Z) :- parent(X, Y), parent(Y, Z).";

        // Act
        var result = PrologCodeExtractor.IsPrologCode(code);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsPrologCode_WithComment_ReturnsTrue()
    {
        // Arrange
        var code = "% This is a Prolog comment";

        // Act
        var result = PrologCodeExtractor.IsPrologCode(code);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsPrologCode_WithPlainText_ReturnsFalse()
    {
        // Arrange
        var code = "This is just plain text without any Prolog constructs";

        // Act
        var result = PrologCodeExtractor.IsPrologCode(code);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsPrologCode_WithNull_ReturnsFalse()
    {
        // Act
        var result = PrologCodeExtractor.IsPrologCode(null);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void SanitizePrologCode_WithUnsafePredicates_CommentsThemOut()
    {
        // Arrange
        var code = """
            main :- 
                write('Hello'),
                shell('ls'),
                nl.
            """;

        // Act
        var result = PrologCodeExtractor.SanitizePrologCode(code);

        // Assert
        result.Should().Contain("% UNSAFE - commented out");
        result.Should().Contain("write('Hello')");
    }

    [Fact]
    public void SanitizePrologCode_WithSafeCode_ReturnsUnmodified()
    {
        // Arrange
        var code = """
            parent(tom, bob).
            main :- write('Safe'), nl.
            """;

        // Act
        var result = PrologCodeExtractor.SanitizePrologCode(code);

        // Assert
        result.Should().NotContain("UNSAFE");
        result.Should().Contain("parent(tom, bob).");
    }

    [Fact]
    public void SanitizePrologCode_WithNull_ReturnsEmpty()
    {
        // Act
        var result = PrologCodeExtractor.SanitizePrologCode(null!);

        // Assert
        result.Should().BeEmpty();
    }

    [Theory]
    [InlineData("shell(")]
    [InlineData("system(")]
    [InlineData("exec(")]
    [InlineData("halt(")]
    [InlineData("abort(")]
    public void SanitizePrologCode_WithVariousUnsafePredicates_CommentsThemOut(string predicate)
    {
        // Arrange
        var code = $"test :- {predicate}'test').";

        // Act
        var result = PrologCodeExtractor.SanitizePrologCode(code);

        // Assert
        result.Should().Contain("% UNSAFE");
    }

    [Fact]
    public void ExtractPrologCode_WithMultipleCodeBlocks_ExtractsFirst()
    {
        // Arrange
        var response = """
            First block:
            ```prolog
            first(code).
            ```

            Second block:
            ```prolog
            second(code).
            ```
            """;

        // Act
        var result = PrologCodeExtractor.ExtractPrologCode(response);

        // Assert
        result.Should().Contain("first(code).");
        result.Should().NotContain("second(code).");
    }
}

