using FluentAssertions;
using LlmPlayground.Prolog;

namespace LlmPlayground.Prolog.Tests;

public class PrologRunnerTests
{
    private readonly PrologRunner _runner;
    private readonly string _testFilesPath;

    public PrologRunnerTests()
    {
        // Use the default constructor which auto-detects swipl from PATH
        _runner = new PrologRunner();
        _testFilesPath = Path.Combine(AppContext.BaseDirectory, "TestFiles");
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithDefaultPath_ShouldUseSwipl()
    {
        // Arrange & Act
        var runner = new PrologRunner();

        // Assert - just verifying no exception is thrown
        runner.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithCustomPath_ShouldAcceptPath()
    {
        // Arrange & Act
        var runner = new PrologRunner("/custom/path/to/prolog");

        // Assert
        runner.Should().NotBeNull();
    }

    #endregion

    #region RunFileAsync Tests

    [Fact]
    public async Task RunFileAsync_WithNonExistentFile_ShouldReturnFailure()
    {
        // Arrange
        var nonExistentPath = Path.Combine(_testFilesPath, "does_not_exist.pl");

        // Act
        var result = await _runner.RunFileAsync(nonExistentPath);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Contain("File not found");
    }

    [SkippableFact]
    public async Task RunFileAsync_WithHelloWorld_ShouldProduceExpectedOutput()
    {
        // Skip if Prolog is not available
        Skip.IfNot(await _runner.IsPrologAvailableAsync(), "SWI-Prolog is not installed");

        // Arrange
        var filePath = Path.Combine(_testFilesPath, "hello.pl");

        // Act
        var result = await _runner.RunFileAsync(filePath);

        // Assert
        result.Success.Should().BeTrue();
        result.Output.Should().Contain("Hello from Prolog!");
    }

    [SkippableFact]
    public async Task RunFileAsync_WithArithmetic_ShouldCalculateCorrectly()
    {
        // Skip if Prolog is not available
        Skip.IfNot(await _runner.IsPrologAvailableAsync(), "SWI-Prolog is not installed");

        // Arrange
        var filePath = Path.Combine(_testFilesPath, "arithmetic.pl");

        // Act
        var result = await _runner.RunFileAsync(filePath, "run_tests");

        // Assert
        result.Success.Should().BeTrue();
        result.Output.Should().Contain("Factorial of 5 is 120");
        result.Output.Should().Contain("Sum of [1,2,3,4,5] is 15");
    }

    [SkippableFact]
    public async Task RunFileAsync_WithFamilyRelations_ShouldFindFathers()
    {
        // Skip if Prolog is not available
        Skip.IfNot(await _runner.IsPrologAvailableAsync(), "SWI-Prolog is not installed");

        // Arrange
        var filePath = Path.Combine(_testFilesPath, "family.pl");

        // Act
        var result = await _runner.RunFileAsync(filePath, "find_fathers");

        // Assert
        result.Success.Should().BeTrue();
        result.Output.Should().Contain("tom is father of");
    }

    [SkippableFact]
    public async Task RunFileAsync_WithFamilyRelations_ShouldFindGrandparents()
    {
        // Skip if Prolog is not available
        Skip.IfNot(await _runner.IsPrologAvailableAsync(), "SWI-Prolog is not installed");

        // Arrange
        var filePath = Path.Combine(_testFilesPath, "family.pl");

        // Act
        var result = await _runner.RunFileAsync(filePath, "find_grandparents");

        // Assert
        result.Success.Should().BeTrue();
        result.Output.Should().Contain("tom is grandparent of");
    }

    [SkippableFact]
    public async Task RunFileAsync_WithSyntaxError_ShouldReturnError()
    {
        // Skip if Prolog is not available
        Skip.IfNot(await _runner.IsPrologAvailableAsync(), "SWI-Prolog is not installed");

        // Arrange
        var filePath = Path.Combine(_testFilesPath, "syntax_error.pl");

        // Act
        var result = await _runner.RunFileAsync(filePath);

        // Assert
        // SWI-Prolog may return exit code 0 but writes errors to stderr
        result.Success.Should().BeFalse();
        result.Error.Should().Contain("Syntax error");
    }

    #endregion

    #region RunQueryAsync Tests

    [SkippableFact]
    public async Task RunQueryAsync_WithSimpleQuery_ShouldSucceed()
    {
        // Skip if Prolog is not available
        Skip.IfNot(await _runner.IsPrologAvailableAsync(), "SWI-Prolog is not installed");

        // Arrange
        var query = "X is 2 + 2, format('Result: ~w~n', [X])";

        // Act
        var result = await _runner.RunQueryAsync(query);

        // Assert
        result.Success.Should().BeTrue();
        result.Output.Should().Contain("Result: 4");
    }

    [SkippableFact]
    public async Task RunQueryAsync_WithWriteQuery_ShouldProduceOutput()
    {
        // Skip if Prolog is not available
        Skip.IfNot(await _runner.IsPrologAvailableAsync(), "SWI-Prolog is not installed");

        // Arrange
        var query = "write('Hello Query'), nl";

        // Act
        var result = await _runner.RunQueryAsync(query);

        // Assert
        result.Success.Should().BeTrue();
        result.Output.Should().Contain("Hello Query");
    }

    [SkippableFact]
    public async Task RunQueryAsync_WithoutTrailingPeriod_ShouldAddPeriod()
    {
        // Skip if Prolog is not available
        Skip.IfNot(await _runner.IsPrologAvailableAsync(), "SWI-Prolog is not installed");

        // Arrange - query without period
        var query = "write('No period')";

        // Act
        var result = await _runner.RunQueryAsync(query);

        // Assert
        result.Success.Should().BeTrue();
        result.Output.Should().Contain("No period");
    }

    [SkippableFact]
    public async Task RunQueryAsync_WithFailingGoal_ShouldNotSucceed()
    {
        // Skip if Prolog is not available
        Skip.IfNot(await _runner.IsPrologAvailableAsync(), "SWI-Prolog is not installed");

        // Arrange - a goal that will fail
        var query = "fail";

        // Act
        var result = await _runner.RunQueryAsync(query);

        // Assert
        result.ExitCode.Should().NotBe(0);
    }

    #endregion

    #region IsPrologAvailableAsync Tests

    [Fact]
    public async Task IsPrologAvailableAsync_WithInvalidPath_ShouldReturnFalse()
    {
        // Arrange - use a path that definitely doesn't exist
        var runner = new PrologRunner("nonexistent_prolog_interpreter_xyz123");

        // Act
        var isAvailable = await runner.IsPrologAvailableAsync();

        // Assert
        isAvailable.Should().BeFalse();
    }

    [SkippableFact]
    public async Task IsPrologAvailableAsync_WithValidInstallation_ShouldReturnTrue()
    {
        // Skip if Prolog is not available
        Skip.IfNot(await _runner.IsPrologAvailableAsync(), "SWI-Prolog is not installed");

        // Act
        var isAvailable = await _runner.IsPrologAvailableAsync();

        // Assert
        isAvailable.Should().BeTrue();
    }

    #endregion

    #region Cancellation Tests

    [SkippableFact]
    public async Task RunFileAsync_WithCancellation_ShouldThrowOperationCancelledException()
    {
        // Skip if Prolog is not available
        Skip.IfNot(await _runner.IsPrologAvailableAsync(), "SWI-Prolog is not installed");

        // Arrange
        var filePath = Path.Combine(_testFilesPath, "hello.pl");
        using var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        // Act
        Func<Task> act = async () => await _runner.RunFileAsync(filePath, null, cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    #endregion
}


