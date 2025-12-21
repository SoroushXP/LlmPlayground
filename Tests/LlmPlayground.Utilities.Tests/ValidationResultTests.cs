using FluentAssertions;
using LlmPlayground.Utilities.Validation;

namespace LlmPlayground.Utilities.Tests;

public class ValidationResultTests
{
    public class SuccessTests
    {
        [Fact]
        public void Success_ReturnsValidResult()
        {
            // Act
            var result = ValidationResult.Success();

            // Assert
            result.IsValid.Should().BeTrue();
            result.Errors.Should().BeEmpty();
        }
    }

    public class FailureTests
    {
        [Fact]
        public void Failure_WithFieldAndMessage_ReturnsInvalidResult()
        {
            // Act
            var result = ValidationResult.Failure("TestField", "Test error message");

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().HaveCount(1);
            result.Errors[0].Field.Should().Be("TestField");
            result.Errors[0].Message.Should().Be("Test error message");
            result.Errors[0].Severity.Should().Be(ValidationSeverity.Error);
        }

        [Fact]
        public void Failure_WithSeverity_SetsSeverityCorrectly()
        {
            // Act
            var result = ValidationResult.Failure("Field", "Critical issue", ValidationSeverity.Critical);

            // Assert
            result.Errors[0].Severity.Should().Be(ValidationSeverity.Critical);
        }

        [Fact]
        public void Failure_WithValidationError_ReturnsInvalidResult()
        {
            // Arrange
            var error = new ValidationError("Field", "Message", ValidationSeverity.Warning);

            // Act
            var result = ValidationResult.Failure(error);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().ContainSingle().Which.Should().Be(error);
        }
    }

    public class AddErrorTests
    {
        [Fact]
        public void AddError_WithFieldAndMessage_AddsToErrors()
        {
            // Arrange
            var result = ValidationResult.Success();

            // Act
            result.AddError("Field1", "Error 1");
            result.AddError("Field2", "Error 2");

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().HaveCount(2);
        }

        [Fact]
        public void AddError_WithValidationError_AddsToErrors()
        {
            // Arrange
            var result = ValidationResult.Success();
            var error = new ValidationError("Field", "Message", ValidationSeverity.Critical);

            // Act
            result.AddError(error);

            // Assert
            result.Errors.Should().ContainSingle().Which.Should().Be(error);
        }

        [Fact]
        public void AddError_WithDifferentSeverities_PreservesEachSeverity()
        {
            // Arrange
            var result = ValidationResult.Success();

            // Act
            result.AddError("Field1", "Warning", ValidationSeverity.Warning);
            result.AddError("Field2", "Error", ValidationSeverity.Error);
            result.AddError("Field3", "Critical", ValidationSeverity.Critical);

            // Assert
            result.Errors.Should().HaveCount(3);
            result.Errors[0].Severity.Should().Be(ValidationSeverity.Warning);
            result.Errors[1].Severity.Should().Be(ValidationSeverity.Error);
            result.Errors[2].Severity.Should().Be(ValidationSeverity.Critical);
        }
    }

    public class MergeTests
    {
        [Fact]
        public void Merge_CombinesErrorsFromBothResults()
        {
            // Arrange
            var result1 = ValidationResult.Failure("Field1", "Error 1");
            var result2 = ValidationResult.Failure("Field2", "Error 2");

            // Act
            result1.Merge(result2);

            // Assert
            result1.Errors.Should().HaveCount(2);
            result1.Errors.Should().Contain(e => e.Field == "Field1");
            result1.Errors.Should().Contain(e => e.Field == "Field2");
        }

        [Fact]
        public void Merge_WithSuccessResult_PreservesOriginalErrors()
        {
            // Arrange
            var result1 = ValidationResult.Failure("Field", "Error");
            var result2 = ValidationResult.Success();

            // Act
            result1.Merge(result2);

            // Assert
            result1.Errors.Should().HaveCount(1);
        }

        [Fact]
        public void Merge_EmptyIntoEmpty_RemainsValid()
        {
            // Arrange
            var result1 = ValidationResult.Success();
            var result2 = ValidationResult.Success();

            // Act
            result1.Merge(result2);

            // Assert
            result1.IsValid.Should().BeTrue();
        }
    }

    public class GetErrorSummaryTests
    {
        [Fact]
        public void GetErrorSummary_WithSingleError_ReturnsFormattedMessage()
        {
            // Arrange
            var result = ValidationResult.Failure("Prompt", "Cannot be empty");

            // Act
            var summary = result.GetErrorSummary();

            // Assert
            summary.Should().Be("Prompt: Cannot be empty");
        }

        [Fact]
        public void GetErrorSummary_WithMultipleErrors_JoinsWithDefaultSeparator()
        {
            // Arrange
            var result = ValidationResult.Success();
            result.AddError("Field1", "Error 1");
            result.AddError("Field2", "Error 2");

            // Act
            var summary = result.GetErrorSummary();

            // Assert
            summary.Should().Be("Field1: Error 1; Field2: Error 2");
        }

        [Fact]
        public void GetErrorSummary_WithCustomSeparator_UsesCustomSeparator()
        {
            // Arrange
            var result = ValidationResult.Success();
            result.AddError("Field1", "Error 1");
            result.AddError("Field2", "Error 2");

            // Act
            var summary = result.GetErrorSummary(" | ");

            // Assert
            summary.Should().Be("Field1: Error 1 | Field2: Error 2");
        }

        [Fact]
        public void GetErrorSummary_WithNoErrors_ReturnsEmptyString()
        {
            // Arrange
            var result = ValidationResult.Success();

            // Act
            var summary = result.GetErrorSummary();

            // Assert
            summary.Should().BeEmpty();
        }
    }
}

public class ValidationErrorTests
{
    [Fact]
    public void ValidationError_IsRecord_SupportsEquality()
    {
        // Arrange
        var error1 = new ValidationError("Field", "Message", ValidationSeverity.Error);
        var error2 = new ValidationError("Field", "Message", ValidationSeverity.Error);
        var error3 = new ValidationError("DifferentField", "Message", ValidationSeverity.Error);

        // Assert
        error1.Should().Be(error2);
        error1.Should().NotBe(error3);
    }

    [Fact]
    public void ValidationError_DefaultSeverity_IsError()
    {
        // Act
        var error = new ValidationError("Field", "Message");

        // Assert
        error.Severity.Should().Be(ValidationSeverity.Error);
    }
}

public class ValidationSeverityTests
{
    [Fact]
    public void ValidationSeverity_HasExpectedValues()
    {
        // Act
        var values = Enum.GetValues<ValidationSeverity>();

        // Assert
        values.Should().HaveCount(3);
        values.Should().Contain(ValidationSeverity.Warning);
        values.Should().Contain(ValidationSeverity.Error);
        values.Should().Contain(ValidationSeverity.Critical);
    }

    [Theory]
    [InlineData(ValidationSeverity.Warning, 0)]
    [InlineData(ValidationSeverity.Error, 1)]
    [InlineData(ValidationSeverity.Critical, 2)]
    public void ValidationSeverity_HasExpectedOrder(ValidationSeverity severity, int expectedValue)
    {
        // Assert - severity increases in order
        ((int)severity).Should().Be(expectedValue);
    }
}

