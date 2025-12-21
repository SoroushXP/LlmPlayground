using FluentAssertions;
using LlmPlayground.Utilities.Validation;

namespace LlmPlayground.Utilities.Tests;

public class RequestValidatorTests
{
    private readonly RequestValidator _validator = new();

    public class ValidatePromptTests : RequestValidatorTests
    {
        [Fact]
        public void ValidatePrompt_WithValidInput_ReturnsSuccess()
        {
            // Arrange
            var prompt = "Hello, how are you?";

            // Act
            var result = _validator.ValidatePrompt(prompt);

            // Assert
            result.IsValid.Should().BeTrue();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("\t\n")]
        public void ValidatePrompt_WithNullOrWhitespace_ReturnsError(string? prompt)
        {
            // Act
            var result = _validator.ValidatePrompt(prompt);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.Field == "Prompt" && e.Message.Contains("null or empty"));
        }

        [Fact]
        public void ValidatePrompt_ExceedingMaxLength_ReturnsError()
        {
            // Arrange
            var prompt = new string('a', 1001);

            // Act
            var result = _validator.ValidatePrompt(prompt, maxLength: 1000);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.Message.Contains("exceeds maximum length"));
        }

        [Fact]
        public void ValidatePrompt_WithNullBytes_ReturnsCriticalError()
        {
            // Arrange
            var prompt = "Hello\0World";

            // Act
            var result = _validator.ValidatePrompt(prompt);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => 
                e.Message.Contains("null characters") && 
                e.Severity == ValidationSeverity.Critical);
        }

        [Fact]
        public void ValidatePrompt_WithExcessiveControlChars_ReturnsWarning()
        {
            // Arrange - more than 10 control characters
            var prompt = "Hello" + new string('\x01', 15) + "World";

            // Act
            var result = _validator.ValidatePrompt(prompt);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => 
                e.Message.Contains("control characters") && 
                e.Severity == ValidationSeverity.Warning);
        }

        [Fact]
        public void ValidatePrompt_WithNormalWhitespace_IsValid()
        {
            // Arrange - normal whitespace should be allowed
            var prompt = "Hello\n\tWorld\r\n";

            // Act
            var result = _validator.ValidatePrompt(prompt);

            // Assert
            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public void ValidatePrompt_DefaultMaxLength_Is100000()
        {
            // Arrange
            var prompt = new string('a', 100_000);

            // Act
            var result = _validator.ValidatePrompt(prompt);

            // Assert
            result.IsValid.Should().BeTrue();
        }
    }

    public class ValidateFilePathTests : RequestValidatorTests
    {
        [Fact]
        public void ValidateFilePath_WithValidPath_ReturnsSuccess()
        {
            // Arrange
            var path = "examples/test.pl";

            // Act
            var result = _validator.ValidateFilePath(path);

            // Assert
            result.IsValid.Should().BeTrue();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void ValidateFilePath_WithNullOrWhitespace_ReturnsError(string? path)
        {
            // Act
            var result = _validator.ValidateFilePath(path);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.Field == "FilePath");
        }

        [Fact]
        public void ValidateFilePath_WithNullBytes_ReturnsCriticalError()
        {
            // Arrange
            var path = "test\0.pl";

            // Act
            var result = _validator.ValidateFilePath(path);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.Severity == ValidationSeverity.Critical);
        }

        [Theory]
        [InlineData("../etc/passwd")]
        [InlineData("..\\windows\\system32")]
        [InlineData("path/../../../etc/passwd")]
        [InlineData("path\\..\\..\\windows")]
        public void ValidateFilePath_WithPathTraversal_ReturnsCriticalError(string path)
        {
            // Act
            var result = _validator.ValidateFilePath(path);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => 
                e.Message.Contains("path traversal") && 
                e.Severity == ValidationSeverity.Critical);
        }

        [Fact]
        public void ValidateFilePath_WithAllowedExtension_ReturnsSuccess()
        {
            // Arrange
            var path = "test.pl";
            var allowedExtensions = new[] { ".pl", ".pro" };

            // Act
            var result = _validator.ValidateFilePath(path, allowedExtensions: allowedExtensions);

            // Assert
            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public void ValidateFilePath_WithDisallowedExtension_ReturnsError()
        {
            // Arrange
            var path = "test.exe";
            var allowedExtensions = new[] { ".pl", ".pro" };

            // Act
            var result = _validator.ValidateFilePath(path, allowedExtensions: allowedExtensions);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.Message.Contains("extension") && e.Message.Contains("not allowed"));
        }

        [Fact]
        public void ValidateFilePath_WithinAllowedBasePath_ReturnsSuccess()
        {
            // Arrange
            var tempDir = Path.GetTempPath();
            var path = Path.Combine(tempDir, "test.pl");

            // Act
            var result = _validator.ValidateFilePath(path, allowedBasePath: tempDir);

            // Assert
            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public void ValidateFilePath_OutsideAllowedBasePath_ReturnsCriticalError()
        {
            // Arrange
            var allowedBase = Path.Combine(Path.GetTempPath(), "allowed");
            var path = Path.Combine(Path.GetTempPath(), "other", "test.pl");

            // Act
            var result = _validator.ValidateFilePath(path, allowedBasePath: allowedBase);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => 
                e.Message.Contains("must be within") && 
                e.Severity == ValidationSeverity.Critical);
        }
    }

    public class ValidatePrologQueryTests : RequestValidatorTests
    {
        [Fact]
        public void ValidatePrologQuery_WithSafeQuery_ReturnsSuccess()
        {
            // Arrange
            var query = "member(X, [1,2,3])";

            // Act
            var result = _validator.ValidatePrologQuery(query);

            // Assert
            result.IsValid.Should().BeTrue();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void ValidatePrologQuery_WithNullOrWhitespace_ReturnsError(string? query)
        {
            // Act
            var result = _validator.ValidatePrologQuery(query);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.Field == "Query");
        }

        [Fact]
        public void ValidatePrologQuery_WithNullBytes_ReturnsCriticalError()
        {
            // Arrange
            var query = "test\0query";

            // Act
            var result = _validator.ValidatePrologQuery(query);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.Severity == ValidationSeverity.Critical);
        }

        [Theory]
        [InlineData("shell('ls')")]
        [InlineData("system('rm -rf /')")]
        [InlineData("exec('cmd.exe')")]
        [InlineData("popen('whoami', read, S)")]
        [InlineData("process_create(path(ls), [], [])")]
        public void ValidatePrologQuery_WithShellExecution_ReturnsCriticalError(string query)
        {
            // Act
            var result = _validator.ValidatePrologQuery(query);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => 
                e.Message.Contains("dangerous predicate") && 
                e.Severity == ValidationSeverity.Critical);
        }

        [Theory]
        [InlineData("halt(0)")]
        [InlineData("abort(1)")]
        public void ValidatePrologQuery_WithHaltAbort_ReturnsCriticalError(string query)
        {
            // Act
            var result = _validator.ValidatePrologQuery(query);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.Severity == ValidationSeverity.Critical);
        }

        [Theory]
        [InlineData("open('file.txt', write, S)")]
        [InlineData("delete_file('test.txt')")]
        [InlineData("rename_file('old.txt', 'new.txt')")]
        [InlineData("make_directory('test')")]
        public void ValidatePrologQuery_WithFileOperations_ReturnsCriticalError(string query)
        {
            // Act
            var result = _validator.ValidatePrologQuery(query);

            // Assert
            result.IsValid.Should().BeFalse();
        }

        [Theory]
        [InlineData("getenv('PATH', Value)")]
        [InlineData("setenv('MY_VAR', 'value')")]
        public void ValidatePrologQuery_WithEnvironmentAccess_ReturnsCriticalError(string query)
        {
            // Act
            var result = _validator.ValidatePrologQuery(query);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.Severity == ValidationSeverity.Critical);
        }

        [Theory]
        [InlineData("member(X, [1,2,3])")]
        [InlineData("findall(X, member(X, [1,2,3]), L)")]
        [InlineData("assertz(fact(a))")]
        [InlineData("retract(fact(a))")]
        public void ValidatePrologQuery_WithSafePredicates_ReturnsSuccess(string query)
        {
            // Act
            var result = _validator.ValidatePrologQuery(query);

            // Assert
            result.IsValid.Should().BeTrue();
        }
    }

    public class ValidateChatMessageTests : RequestValidatorTests
    {
        [Theory]
        [InlineData("user")]
        [InlineData("system")]
        [InlineData("assistant")]
        [InlineData("USER")]
        [InlineData("System")]
        [InlineData("ASSISTANT")]
        public void ValidateChatMessage_WithValidRole_ReturnsSuccess(string role)
        {
            // Act
            var result = _validator.ValidateChatMessage(role, "Hello");

            // Assert
            result.IsValid.Should().BeTrue();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void ValidateChatMessage_WithNullOrWhitespaceRole_ReturnsError(string? role)
        {
            // Act
            var result = _validator.ValidateChatMessage(role, "Hello");

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.Field == "Role");
        }

        [Theory]
        [InlineData("admin")]
        [InlineData("bot")]
        [InlineData("function")]
        [InlineData("tool")]
        public void ValidateChatMessage_WithInvalidRole_ReturnsError(string role)
        {
            // Act
            var result = _validator.ValidateChatMessage(role, "Hello");

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => 
                e.Field == "Role" && 
                e.Message.Contains("Invalid role"));
        }

        [Fact]
        public void ValidateChatMessage_WithInvalidContent_ReturnsError()
        {
            // Arrange - null bytes in content
            var content = "Hello\0World";

            // Act
            var result = _validator.ValidateChatMessage("user", content);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.Field.Contains("Content"));
        }
    }

    public class ValidateInferenceOptionsTests : RequestValidatorTests
    {
        [Fact]
        public void ValidateInferenceOptions_WithValidOptions_ReturnsSuccess()
        {
            // Act
            var result = _validator.ValidateInferenceOptions(
                maxTokens: 500,
                temperature: 0.7f,
                topP: 0.9f,
                repeatPenalty: 1.1f);

            // Assert
            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public void ValidateInferenceOptions_WithNullOptions_ReturnsSuccess()
        {
            // Act
            var result = _validator.ValidateInferenceOptions(null, null, null, null);

            // Assert
            result.IsValid.Should().BeTrue();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-100)]
        public void ValidateInferenceOptions_WithInvalidMaxTokens_ReturnsError(int maxTokens)
        {
            // Act
            var result = _validator.ValidateInferenceOptions(maxTokens, null, null, null);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.Field == "MaxTokens");
        }

        [Fact]
        public void ValidateInferenceOptions_WithExcessiveMaxTokens_ReturnsError()
        {
            // Act
            var result = _validator.ValidateInferenceOptions(100_001, null, null, null);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.Field == "MaxTokens" && e.Message.Contains("100,000"));
        }

        [Fact]
        public void ValidateInferenceOptions_WithNegativeTemperature_ReturnsError()
        {
            // Act
            var result = _validator.ValidateInferenceOptions(null, -0.1f, null, null);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.Field == "Temperature");
        }

        [Fact]
        public void ValidateInferenceOptions_WithHighTemperature_ReturnsWarning()
        {
            // Act
            var result = _validator.ValidateInferenceOptions(null, 2.5f, null, null);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => 
                e.Field == "Temperature" && 
                e.Severity == ValidationSeverity.Warning);
        }

        [Theory]
        [InlineData(0f)]
        [InlineData(-0.1f)]
        [InlineData(1.1f)]
        public void ValidateInferenceOptions_WithInvalidTopP_ReturnsError(float topP)
        {
            // Act
            var result = _validator.ValidateInferenceOptions(null, null, topP, null);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.Field == "TopP");
        }

        [Fact]
        public void ValidateInferenceOptions_WithValidTopP_ReturnsSuccess()
        {
            // Act
            var result = _validator.ValidateInferenceOptions(null, null, 1.0f, null);

            // Assert
            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public void ValidateInferenceOptions_WithNegativeRepeatPenalty_ReturnsError()
        {
            // Act
            var result = _validator.ValidateInferenceOptions(null, null, null, -0.1f);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.Field == "RepeatPenalty");
        }

        [Fact]
        public void ValidateInferenceOptions_WithHighRepeatPenalty_ReturnsWarning()
        {
            // Act
            var result = _validator.ValidateInferenceOptions(null, null, null, 5.5f);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => 
                e.Field == "RepeatPenalty" && 
                e.Severity == ValidationSeverity.Warning);
        }

        [Fact]
        public void ValidateInferenceOptions_WithMultipleInvalidOptions_ReturnsAllErrors()
        {
            // Act
            var result = _validator.ValidateInferenceOptions(
                maxTokens: -1,
                temperature: -0.1f,
                topP: 0f,
                repeatPenalty: -0.1f);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().HaveCount(4);
        }
    }
}

