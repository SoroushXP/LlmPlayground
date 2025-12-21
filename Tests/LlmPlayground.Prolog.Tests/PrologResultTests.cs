using FluentAssertions;
using LlmPlayground.Prolog;

namespace LlmPlayground.Prolog.Tests;

public class PrologResultTests
{
    [Fact]
    public void DefaultValues_ShouldBeSetCorrectly()
    {
        // Arrange & Act
        var result = new PrologResult();

        // Assert
        result.Success.Should().BeFalse();
        result.Output.Should().BeEmpty();
        result.Error.Should().BeEmpty();
        result.ExitCode.Should().Be(0);
    }

    [Fact]
    public void AllProperties_ShouldBeSettable()
    {
        // Arrange & Act
        var result = new PrologResult
        {
            Success = true,
            Output = "test output",
            Error = "test error",
            ExitCode = 1
        };

        // Assert
        result.Success.Should().BeTrue();
        result.Output.Should().Be("test output");
        result.Error.Should().Be("test error");
        result.ExitCode.Should().Be(1);
    }

    [Fact]
    public void SuccessfulResult_ShouldHaveZeroExitCode()
    {
        // Arrange & Act
        var result = new PrologResult
        {
            Success = true,
            Output = "Hello World",
            ExitCode = 0
        };

        // Assert
        result.Success.Should().BeTrue();
        result.ExitCode.Should().Be(0);
        result.Error.Should().BeEmpty();
    }

    [Fact]
    public void FailedResult_ShouldHaveErrorMessage()
    {
        // Arrange & Act
        var result = new PrologResult
        {
            Success = false,
            Error = "Syntax error at line 5",
            ExitCode = 1
        };

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().NotBeEmpty();
        result.ExitCode.Should().NotBe(0);
    }
}



