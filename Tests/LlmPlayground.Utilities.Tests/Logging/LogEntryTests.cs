using FluentAssertions;
using LlmPlayground.Utilities.Logging;

namespace LlmPlayground.Utilities.Tests.Logging;

public class LogEntryTests
{
    public class LogEntryCreation
    {
        [Fact]
        public void LogEntry_WithRequiredMessage_SetsDefaults()
        {
            // Act
            var entry = new LogEntry { Message = "Test message" };

            // Assert
            entry.Message.Should().Be("Test message");
            entry.Level.Should().Be(LogLevel.Information);
            entry.Timestamp.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
            entry.ThreadId.Should().Be(Environment.CurrentManagedThreadId);
            entry.Source.Should().BeNull();
            entry.Exception.Should().BeNull();
            entry.Properties.Should().BeNull();
            entry.CorrelationId.Should().BeNull();
            entry.IsConsoleCapture.Should().BeFalse();
        }

        [Fact]
        public void LogEntry_WithAllProperties_SetsAllValues()
        {
            // Arrange
            var exception = new InvalidOperationException("Test");
            var properties = new Dictionary<string, object?> { ["key"] = "value" };
            var timestamp = DateTimeOffset.UtcNow.AddHours(-1);

            // Act
            var entry = new LogEntry
            {
                Message = "Test",
                Level = LogLevel.Error,
                Timestamp = timestamp,
                Source = "TestSource",
                Exception = exception,
                Properties = properties,
                CorrelationId = "abc123",
                IsConsoleCapture = true
            };

            // Assert
            entry.Level.Should().Be(LogLevel.Error);
            entry.Timestamp.Should().Be(timestamp);
            entry.Source.Should().Be("TestSource");
            entry.Exception.Should().Be(exception);
            entry.Properties.Should().ContainKey("key");
            entry.CorrelationId.Should().Be("abc123");
            entry.IsConsoleCapture.Should().BeTrue();
        }

        [Fact]
        public void LogEntry_IsImmutable_CanCreateWithExpression()
        {
            // Arrange
            var original = new LogEntry { Message = "Original", Level = LogLevel.Debug };

            // Act
            var modified = original with { Level = LogLevel.Error };

            // Assert
            original.Level.Should().Be(LogLevel.Debug);
            modified.Level.Should().Be(LogLevel.Error);
            modified.Message.Should().Be("Original");
        }
    }

    public class LogEntryBuilderTests
    {
        [Fact]
        public void Build_WithMinimalConfiguration_CreatesEntry()
        {
            // Act
            var entry = new LogEntryBuilder()
                .WithMessage("Test")
                .Build();

            // Assert
            entry.Message.Should().Be("Test");
            entry.Level.Should().Be(LogLevel.Information);
        }

        [Fact]
        public void Build_WithAllOptions_CreatesCompleteEntry()
        {
            // Arrange
            var exception = new Exception("Error");

            // Act
            var entry = new LogEntryBuilder()
                .WithLevel(LogLevel.Critical)
                .WithMessage("Critical error")
                .WithSource("MyService")
                .WithException(exception)
                .WithProperty("UserId", 123)
                .WithProperty("Action", "Login")
                .WithCorrelationId("req-456")
                .AsConsoleCapture()
                .Build();

            // Assert
            entry.Level.Should().Be(LogLevel.Critical);
            entry.Message.Should().Be("Critical error");
            entry.Source.Should().Be("MyService");
            entry.Exception.Should().Be(exception);
            entry.Properties.Should().ContainKey("UserId").WhoseValue.Should().Be(123);
            entry.Properties.Should().ContainKey("Action").WhoseValue.Should().Be("Login");
            entry.CorrelationId.Should().Be("req-456");
            entry.IsConsoleCapture.Should().BeTrue();
        }

        [Fact]
        public void WithProperties_AddsMultipleProperties()
        {
            // Arrange
            var props = new Dictionary<string, object?>
            {
                ["A"] = 1,
                ["B"] = "two",
                ["C"] = null
            };

            // Act
            var entry = new LogEntryBuilder()
                .WithMessage("Test")
                .WithProperties(props)
                .Build();

            // Assert
            entry.Properties.Should().HaveCount(3);
            entry.Properties!["A"].Should().Be(1);
            entry.Properties["B"].Should().Be("two");
            entry.Properties["C"].Should().BeNull();
        }

        [Fact]
        public void WithProperty_OverwritesPreviousValue()
        {
            // Act
            var entry = new LogEntryBuilder()
                .WithMessage("Test")
                .WithProperty("Key", "First")
                .WithProperty("Key", "Second")
                .Build();

            // Assert
            entry.Properties!["Key"].Should().Be("Second");
        }

        [Fact]
        public void AsConsoleCapture_WithFalse_DoesNotSetFlag()
        {
            // Act
            var entry = new LogEntryBuilder()
                .WithMessage("Test")
                .AsConsoleCapture(false)
                .Build();

            // Assert
            entry.IsConsoleCapture.Should().BeFalse();
        }
    }
}

public class LogLevelTests
{
    [Theory]
    [InlineData(LogLevel.Trace, 0)]
    [InlineData(LogLevel.Debug, 1)]
    [InlineData(LogLevel.Information, 2)]
    [InlineData(LogLevel.Console, 3)]
    [InlineData(LogLevel.Warning, 4)]
    [InlineData(LogLevel.Error, 5)]
    [InlineData(LogLevel.Critical, 6)]
    [InlineData(LogLevel.None, 7)]
    public void LogLevel_HasCorrectOrdinalValues(LogLevel level, int expected)
    {
        // Assert
        ((int)level).Should().Be(expected);
    }

    [Fact]
    public void LogLevel_CanCompare_ForFiltering()
    {
        // Assert - Compare underlying integer values
        ((int)LogLevel.Debug).Should().BeGreaterThan((int)LogLevel.Trace);
        ((int)LogLevel.Information).Should().BeGreaterThan((int)LogLevel.Debug);
        ((int)LogLevel.Warning).Should().BeGreaterThan((int)LogLevel.Information);
        ((int)LogLevel.Error).Should().BeGreaterThan((int)LogLevel.Warning);
        ((int)LogLevel.Critical).Should().BeGreaterThan((int)LogLevel.Error);
    }
}

