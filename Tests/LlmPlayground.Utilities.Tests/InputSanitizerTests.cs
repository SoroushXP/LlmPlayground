using FluentAssertions;
using LlmPlayground.Utilities.Sanitization;

namespace LlmPlayground.Utilities.Tests;

public class InputSanitizerTests
{
    public class SanitizeTests
    {
        [Theory]
        [InlineData(null, "")]
        [InlineData("", "")]
        public void Sanitize_WithNullOrEmpty_ReturnsEmpty(string? input, string expected)
        {
            // Act
            var result = InputSanitizer.Sanitize(input);

            // Assert
            result.Should().Be(expected);
        }

        [Fact]
        public void Sanitize_WithNullBytes_RemovesThem()
        {
            // Arrange
            var input = "Hello\0World";

            // Act
            var result = InputSanitizer.Sanitize(input);

            // Assert
            result.Should().Be("HelloWorld");
        }

        [Fact]
        public void Sanitize_WithControlCharacters_RemovesThem()
        {
            // Arrange - \x01 through \x1F except \t, \n, \r
            var input = "Hello\x01\x02\x03World";

            // Act
            var result = InputSanitizer.Sanitize(input);

            // Assert
            result.Should().Be("HelloWorld");
        }

        [Fact]
        public void Sanitize_PreservesNormalWhitespace()
        {
            // Arrange
            var input = "Hello\t\n\rWorld";

            // Act
            var result = InputSanitizer.Sanitize(input);

            // Assert
            result.Should().Contain("\t");
            result.Should().Contain("\n");
        }

        [Fact]
        public void Sanitize_NormalizesLineEndings()
        {
            // Arrange
            var input = "Line1\r\nLine2\rLine3";

            // Act
            var result = InputSanitizer.Sanitize(input);

            // Assert
            result.Should().Be("Line1\nLine2\nLine3");
        }

        [Fact]
        public void Sanitize_TrimsWhitespace()
        {
            // Arrange
            var input = "   Hello World   ";

            // Act
            var result = InputSanitizer.Sanitize(input);

            // Assert
            result.Should().Be("Hello World");
        }

        [Fact]
        public void Sanitize_WithCollapseWhitespace_CollapsesMultipleSpaces()
        {
            // Arrange
            var input = "Hello    World";
            var options = new SanitizationOptions { CollapseWhitespace = true };

            // Act
            var result = InputSanitizer.Sanitize(input, options);

            // Assert
            result.Should().Be("Hello World");
        }

        [Fact]
        public void Sanitize_WithMaxLength_TruncatesInput()
        {
            // Arrange
            var input = "Hello World";
            var options = new SanitizationOptions { MaxLength = 5 };

            // Act
            var result = InputSanitizer.Sanitize(input, options);

            // Assert
            result.Should().Be("Hello");
        }

        [Fact]
        public void Sanitize_WithMinimalOptions_OnlyRemovesNullBytes()
        {
            // Arrange
            var input = "  Hello\0\x01World\r\n  ";

            // Act
            var result = InputSanitizer.Sanitize(input, SanitizationOptions.Minimal);

            // Assert
            result.Should().Be("  Hello\x01World\r\n  ");
        }

        [Fact]
        public void Sanitize_WithStrictOptions_AppliesAllCleaning()
        {
            // Arrange
            var input = "  Hello\0\x01    World\r\n  ";

            // Act
            var result = InputSanitizer.Sanitize(input, SanitizationOptions.Strict);

            // Assert
            result.Should().Be("Hello World");
        }
    }

    public class SanitizeFilePathTests
    {
        [Theory]
        [InlineData(null, "")]
        [InlineData("", "")]
        public void SanitizeFilePath_WithNullOrEmpty_ReturnsEmpty(string? input, string expected)
        {
            // Act
            var result = InputSanitizer.SanitizeFilePath(input);

            // Assert
            result.Should().Be(expected);
        }

        [Fact]
        public void SanitizeFilePath_WithNullBytes_RemovesThem()
        {
            // Arrange
            var input = "path\0to\0file.txt";

            // Act
            var result = InputSanitizer.SanitizeFilePath(input);

            // Assert
            result.Should().NotContain("\0");
        }

        [Theory]
        [InlineData("../file.txt")]
        [InlineData("..\\file.txt")]
        [InlineData("path/../file.txt")]
        [InlineData("path\\..\\file.txt")]
        public void SanitizeFilePath_RemovesPathTraversal(string input)
        {
            // Act
            var result = InputSanitizer.SanitizeFilePath(input);

            // Assert
            result.Should().NotContain("..");
        }

        [Fact]
        public void SanitizeFilePath_TrimsWhitespace()
        {
            // Arrange
            var input = "   path/to/file.txt   ";

            // Act
            var result = InputSanitizer.SanitizeFilePath(input);

            // Assert
            result.Should().NotStartWith(" ");
            result.Should().NotEndWith(" ");
        }

        [Fact]
        public void SanitizeFilePath_NormalizesPathSeparators()
        {
            // Arrange
            var input = "path/to/file.txt";

            // Act
            var result = InputSanitizer.SanitizeFilePath(input);

            // Assert
            result.Should().Contain(Path.DirectorySeparatorChar.ToString());
        }

        [Fact]
        public void SanitizeFilePath_RemovesConsecutiveSeparators()
        {
            // Arrange
            var sep = Path.DirectorySeparatorChar;
            var input = $"path{sep}{sep}to{sep}{sep}{sep}file.txt";

            // Act
            var result = InputSanitizer.SanitizeFilePath(input);

            // Assert
            result.Should().NotContain($"{sep}{sep}");
        }
    }

    public class SanitizeForLoggingTests
    {
        [Theory]
        [InlineData(null, "[empty]")]
        [InlineData("", "[empty]")]
        public void SanitizeForLogging_WithNullOrEmpty_ReturnsEmpty(string? input, string expected)
        {
            // Act
            var result = InputSanitizer.SanitizeForLogging(input);

            // Assert
            result.Should().Be(expected);
        }

        [Theory]
        [InlineData("sk-1234567890abcdefghijklmn")]
        [InlineData("api_key_abcdefghijklmnopqrstuvwx")]
        [InlineData("api-key-abcdefghijklmnopqrstuvwx")]
        public void SanitizeForLogging_MasksApiKeys(string apiKey)
        {
            // Arrange
            var input = $"Using key: {apiKey}";

            // Act
            var result = InputSanitizer.SanitizeForLogging(input);

            // Assert
            result.Should().Contain("[MASKED_KEY]");
            result.Should().NotContain(apiKey);
        }

        [Theory]
        [InlineData("password=secret123")]
        [InlineData("password: secret123")]
        [InlineData("pwd=mypassword")]
        [InlineData("secret=mysecret")]
        [InlineData("token=abc123token")]
        public void SanitizeForLogging_MasksPasswords(string sensitiveData)
        {
            // Arrange
            var input = $"Config: {sensitiveData}";

            // Act
            var result = InputSanitizer.SanitizeForLogging(input);

            // Assert
            result.Should().Contain("[MASKED]");
        }

        [Fact]
        public void SanitizeForLogging_MasksEmailAddresses()
        {
            // Arrange
            var input = "Contact: user@example.com for support";

            // Act
            var result = InputSanitizer.SanitizeForLogging(input);

            // Assert
            result.Should().Contain("[MASKED_EMAIL]");
            result.Should().NotContain("user@example.com");
        }

        [Fact]
        public void SanitizeForLogging_TruncatesLongStrings()
        {
            // Arrange
            var input = new string('a', 600);

            // Act
            var result = InputSanitizer.SanitizeForLogging(input);

            // Assert
            result.Should().Contain("truncated");
            result.Should().Contain("600 chars total");
            result.Length.Should().BeLessThan(600);
        }

        [Fact]
        public void SanitizeForLogging_PreservesNormalText()
        {
            // Arrange
            var input = "This is a normal log message without sensitive data.";

            // Act
            var result = InputSanitizer.SanitizeForLogging(input);

            // Assert
            result.Should().Be(input);
        }
    }

    public class UrlEncodeTests
    {
        [Theory]
        [InlineData(null, "")]
        [InlineData("", "")]
        public void UrlEncode_WithNullOrEmpty_ReturnsEmpty(string? input, string expected)
        {
            // Act
            var result = InputSanitizer.UrlEncode(input);

            // Assert
            result.Should().Be(expected);
        }

        [Fact]
        public void UrlEncode_EncodesSpecialCharacters()
        {
            // Arrange
            var input = "hello world?foo=bar&baz=qux";

            // Act
            var result = InputSanitizer.UrlEncode(input);

            // Assert
            result.Should().Be("hello%20world%3Ffoo%3Dbar%26baz%3Dqux");
        }

        [Fact]
        public void UrlEncode_PreservesAlphanumeric()
        {
            // Arrange
            var input = "abc123";

            // Act
            var result = InputSanitizer.UrlEncode(input);

            // Assert
            result.Should().Be("abc123");
        }
    }

    public class EscapeForJsonTests
    {
        [Theory]
        [InlineData(null, "")]
        [InlineData("", "")]
        public void EscapeForJson_WithNullOrEmpty_ReturnsEmpty(string? input, string expected)
        {
            // Act
            var result = InputSanitizer.EscapeForJson(input);

            // Assert
            result.Should().Be(expected);
        }

        [Fact]
        public void EscapeForJson_EscapesQuotes()
        {
            // Arrange
            var input = "He said \"Hello\"";

            // Act
            var result = InputSanitizer.EscapeForJson(input);

            // Assert
            result.Should().Be("He said \\\"Hello\\\"");
        }

        [Fact]
        public void EscapeForJson_EscapesBackslashes()
        {
            // Arrange
            var input = "path\\to\\file";

            // Act
            var result = InputSanitizer.EscapeForJson(input);

            // Assert
            result.Should().Be("path\\\\to\\\\file");
        }

        [Fact]
        public void EscapeForJson_EscapesControlCharacters()
        {
            // Arrange
            var input = "Line1\nLine2\tTabbed\rCarriage";

            // Act
            var result = InputSanitizer.EscapeForJson(input);

            // Assert
            result.Should().Contain("\\n");
            result.Should().Contain("\\t");
            result.Should().Contain("\\r");
        }

        [Fact]
        public void EscapeForJson_EscapesFormFeedAndBackspace()
        {
            // Arrange
            var input = "Before\fAfter\bBack";

            // Act
            var result = InputSanitizer.EscapeForJson(input);

            // Assert
            result.Should().Contain("\\f");
            result.Should().Contain("\\b");
        }

        [Fact]
        public void EscapeForJson_EscapesOtherControlCharsAsUnicode()
        {
            // Arrange
            var input = "Test\x01Char";

            // Act
            var result = InputSanitizer.EscapeForJson(input);

            // Assert
            // Control character \x01 gets escaped as \u0001
            result.Should().NotBe(input);
            result.Should().Contain("Test");
            result.Should().Contain("Char");
        }
    }

    public class StripHtmlTests
    {
        [Theory]
        [InlineData(null, "")]
        [InlineData("", "")]
        public void StripHtml_WithNullOrEmpty_ReturnsEmpty(string? input, string expected)
        {
            // Act
            var result = InputSanitizer.StripHtml(input);

            // Assert
            result.Should().Be(expected);
        }

        [Fact]
        public void StripHtml_RemovesSimpleTags()
        {
            // Arrange
            var input = "<p>Hello World</p>";

            // Act
            var result = InputSanitizer.StripHtml(input);

            // Assert
            result.Should().Be("Hello World");
        }

        [Fact]
        public void StripHtml_RemovesTagsWithAttributes()
        {
            // Arrange
            var input = "<a href=\"https://example.com\" class=\"link\">Click here</a>";

            // Act
            var result = InputSanitizer.StripHtml(input);

            // Assert
            result.Should().Be("Click here");
        }

        [Fact]
        public void StripHtml_RemovesNestedTags()
        {
            // Arrange
            var input = "<div><p><strong>Bold</strong> text</p></div>";

            // Act
            var result = InputSanitizer.StripHtml(input);

            // Assert
            result.Should().Be("Bold text");
        }

        [Fact]
        public void StripHtml_RemovesSelfClosingTags()
        {
            // Arrange
            var input = "Line1<br/>Line2<hr/>Line3";

            // Act
            var result = InputSanitizer.StripHtml(input);

            // Assert
            result.Should().Be("Line1Line2Line3");
        }

        [Fact]
        public void StripHtml_PreservesTextWithoutTags()
        {
            // Arrange
            var input = "Plain text without any HTML";

            // Act
            var result = InputSanitizer.StripHtml(input);

            // Assert
            result.Should().Be(input);
        }
    }

    public class SanitizationOptionsTests
    {
        [Fact]
        public void Default_HasExpectedSettings()
        {
            // Act
            var options = SanitizationOptions.Default;

            // Assert
            options.RemoveNullBytes.Should().BeTrue();
            options.RemoveControlCharacters.Should().BeTrue();
            options.NormalizeLineEndings.Should().BeTrue();
            options.TrimWhitespace.Should().BeTrue();
            options.CollapseWhitespace.Should().BeFalse();
            options.MaxLength.Should().BeNull();
        }

        [Fact]
        public void Minimal_HasExpectedSettings()
        {
            // Act
            var options = SanitizationOptions.Minimal;

            // Assert
            options.RemoveNullBytes.Should().BeTrue();
            options.RemoveControlCharacters.Should().BeFalse();
            options.NormalizeLineEndings.Should().BeFalse();
            options.TrimWhitespace.Should().BeFalse();
            options.CollapseWhitespace.Should().BeFalse();
        }

        [Fact]
        public void Strict_HasExpectedSettings()
        {
            // Act
            var options = SanitizationOptions.Strict;

            // Assert
            options.RemoveNullBytes.Should().BeTrue();
            options.RemoveControlCharacters.Should().BeTrue();
            options.NormalizeLineEndings.Should().BeTrue();
            options.TrimWhitespace.Should().BeTrue();
            options.CollapseWhitespace.Should().BeTrue();
            options.MaxLength.Should().Be(50_000);
        }

        [Fact]
        public void CustomOptions_CanBeCreated()
        {
            // Act
            var options = new SanitizationOptions
            {
                RemoveNullBytes = false,
                MaxLength = 1000
            };

            // Assert
            options.RemoveNullBytes.Should().BeFalse();
            options.MaxLength.Should().Be(1000);
        }
    }
}

