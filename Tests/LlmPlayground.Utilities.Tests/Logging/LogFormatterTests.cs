using FluentAssertions;
using LlmPlayground.Utilities.Logging;

namespace LlmPlayground.Utilities.Tests.Logging;

public class LogFormatterTests
{
    public class DefaultLogFormatterTests
    {
        [Fact]
        public void Format_WithDefaultOptions_IncludesTimestampAndLevel()
        {
            // Arrange
            var formatter = new DefaultLogFormatter();
            var entry = new LogEntry
            {
                Message = "Test message",
                Level = LogLevel.Information
            };

            // Act
            var result = formatter.Format(entry);

            // Assert
            result.Should().Contain("[INF]");
            result.Should().Contain("Test message");
            result.Should().MatchRegex(@"\d{4}-\d{2}-\d{2}");
        }

        [Fact]
        public void Format_WithSource_IncludesSource()
        {
            // Arrange
            var formatter = new DefaultLogFormatter();
            var entry = new LogEntry
            {
                Message = "Test",
                Source = "MyService"
            };

            // Act
            var result = formatter.Format(entry);

            // Assert
            result.Should().Contain("MyService:");
        }

        [Fact]
        public void Format_WithCorrelationId_IncludesCorrelationId()
        {
            // Arrange
            var formatter = new DefaultLogFormatter();
            var entry = new LogEntry
            {
                Message = "Test",
                CorrelationId = "req-123"
            };

            // Act
            var result = formatter.Format(entry);

            // Assert
            result.Should().Contain("<req-123>");
        }

        [Fact]
        public void Format_WithProperties_IncludesProperties()
        {
            // Arrange
            var formatter = new DefaultLogFormatter();
            var entry = new LogEntry
            {
                Message = "Test",
                Properties = new Dictionary<string, object?>
                {
                    ["UserId"] = 42,
                    ["Action"] = "Login"
                }
            };

            // Act
            var result = formatter.Format(entry);

            // Assert
            result.Should().Contain("UserId=42");
            result.Should().Contain("Action=Login");
        }

        [Fact]
        public void Format_WithException_IncludesExceptionDetails()
        {
            // Arrange
            var formatter = new DefaultLogFormatter();
            var exception = new InvalidOperationException("Something went wrong");
            var entry = new LogEntry
            {
                Message = "Error occurred",
                Exception = exception
            };

            // Act
            var result = formatter.Format(entry);

            // Assert
            result.Should().Contain("InvalidOperationException");
            result.Should().Contain("Something went wrong");
        }

        [Fact]
        public void Format_WithShortException_OnlyShowsTypeName()
        {
            // Arrange
            var options = new LogFormatterOptions { IncludeFullException = false };
            var formatter = new DefaultLogFormatter(options);
            var exception = new ArgumentException("Bad argument");
            var entry = new LogEntry
            {
                Message = "Error",
                Exception = exception
            };

            // Act
            var result = formatter.Format(entry);

            // Assert
            result.Should().Contain("ArgumentException");
            result.Should().Contain("Bad argument");
            result.Should().NotContain("at "); // No stack trace
        }

        [Fact]
        public void Format_WithoutTimestamp_OmitsTimestamp()
        {
            // Arrange
            var options = new LogFormatterOptions { IncludeTimestamp = false };
            var formatter = new DefaultLogFormatter(options);
            var entry = new LogEntry { Message = "Test" };

            // Act
            var result = formatter.Format(entry);

            // Assert
            result.Should().NotMatchRegex(@"\d{4}-\d{2}-\d{2}");
        }

        [Fact]
        public void Format_WithThreadId_IncludesThreadId()
        {
            // Arrange
            var options = new LogFormatterOptions { IncludeThreadId = true };
            var formatter = new DefaultLogFormatter(options);
            var entry = new LogEntry { Message = "Test" };

            // Act
            var result = formatter.Format(entry);

            // Assert
            result.Should().MatchRegex(@"\[\s*\d+\]");
        }

        [Theory]
        [InlineData(LogLevel.Trace, "TRC")]
        [InlineData(LogLevel.Debug, "DBG")]
        [InlineData(LogLevel.Information, "INF")]
        [InlineData(LogLevel.Console, "CON")]
        [InlineData(LogLevel.Warning, "WRN")]
        [InlineData(LogLevel.Error, "ERR")]
        [InlineData(LogLevel.Critical, "CRT")]
        public void Format_ShortLevelNames_UsesThreeLetterCodes(LogLevel level, string expected)
        {
            // Arrange
            var options = new LogFormatterOptions { UseShortLevelNames = true };
            var formatter = new DefaultLogFormatter(options);
            var entry = new LogEntry { Message = "Test", Level = level };

            // Act
            var result = formatter.Format(entry);

            // Assert
            result.Should().Contain($"[{expected}]");
        }

        [Fact]
        public void Format_LongLevelNames_UsesFullName()
        {
            // Arrange
            var options = new LogFormatterOptions { UseShortLevelNames = false };
            var formatter = new DefaultLogFormatter(options);
            var entry = new LogEntry { Message = "Test", Level = LogLevel.Information };

            // Act
            var result = formatter.Format(entry);

            // Assert
            result.Should().Contain("INFORMATION");
        }

        [Fact]
        public void Format_WithNullPropertyValue_ShowsNull()
        {
            // Arrange
            var formatter = new DefaultLogFormatter();
            var entry = new LogEntry
            {
                Message = "Test",
                Properties = new Dictionary<string, object?> { ["Key"] = null }
            };

            // Act
            var result = formatter.Format(entry);

            // Assert
            result.Should().Contain("Key=null");
        }
    }

    public class JsonLogFormatterTests
    {
        [Fact]
        public void Format_CreatesValidJson()
        {
            // Arrange
            var formatter = new JsonLogFormatter();
            var entry = new LogEntry
            {
                Message = "Test message",
                Level = LogLevel.Warning
            };

            // Act
            var result = formatter.Format(entry);

            // Assert
            result.Should().StartWith("{");
            result.Should().EndWith("}");
            result.Should().Contain("\"message\":\"Test message\"");
            result.Should().Contain("\"level\":\"Warning\"");
        }

        [Fact]
        public void Format_IncludesAllFields()
        {
            // Arrange
            var formatter = new JsonLogFormatter();
            var entry = new LogEntry
            {
                Message = "Test",
                Source = "MyService",
                CorrelationId = "abc123",
                IsConsoleCapture = true
            };

            // Act
            var result = formatter.Format(entry);

            // Assert
            result.Should().Contain("\"source\":\"MyService\"");
            result.Should().Contain("\"correlationId\":\"abc123\"");
            result.Should().Contain("\"isConsoleCapture\":true");
        }

        [Fact]
        public void Format_WithException_IncludesExceptionObject()
        {
            // Arrange
            var formatter = new JsonLogFormatter();
            var entry = new LogEntry
            {
                Message = "Error",
                Exception = new ArgumentException("Bad arg")
            };

            // Act
            var result = formatter.Format(entry);

            // Assert
            result.Should().Contain("\"exception\":");
            result.Should().Contain("ArgumentException");
            result.Should().Contain("Bad arg");
        }

        [Fact]
        public void Format_WithProperties_PrefixesPropertyKeys()
        {
            // Arrange
            var formatter = new JsonLogFormatter();
            var entry = new LogEntry
            {
                Message = "Test",
                Properties = new Dictionary<string, object?> { ["UserId"] = 42 }
            };

            // Act
            var result = formatter.Format(entry);

            // Assert
            result.Should().Contain("\"prop_UserId\":42");
        }

        [Fact]
        public void Format_IncludesIsoTimestamp()
        {
            // Arrange
            var formatter = new JsonLogFormatter();
            var entry = new LogEntry { Message = "Test" };

            // Act
            var result = formatter.Format(entry);

            // Assert
            result.Should().Contain("\"timestamp\":");
            result.Should().MatchRegex(@"\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}");
        }
    }

    public class LogFormatterOptionsTests
    {
        [Fact]
        public void DefaultOptions_HasExpectedValues()
        {
            // Act
            var options = new LogFormatterOptions();

            // Assert
            options.IncludeTimestamp.Should().BeTrue();
            options.UseUtcTimestamp.Should().BeTrue();
            options.TimestampFormat.Should().Be("yyyy-MM-dd HH:mm:ss.fff");
            options.IncludeLevel.Should().BeTrue();
            options.UseShortLevelNames.Should().BeTrue();
            options.IncludeThreadId.Should().BeFalse();
            options.IncludeCorrelationId.Should().BeTrue();
            options.IncludeSource.Should().BeTrue();
            options.IncludeProperties.Should().BeTrue();
            options.IncludeFullException.Should().BeTrue();
        }
    }
}

