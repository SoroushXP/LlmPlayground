using FluentAssertions;
using LlmPlayground.Utilities.Logging;
using NSubstitute;

namespace LlmPlayground.Utilities.Tests.Logging;

public class LoggingExtensionsTests
{
    private readonly ILoggerService _logger;
    private readonly ILogSink _sink;
    private readonly List<LogEntry> _capturedEntries;

    public LoggingExtensionsTests()
    {
        _logger = new LoggerService { MinimumLevel = LogLevel.Trace };
        _sink = Substitute.For<ILogSink>();
        _sink.Name.Returns("TestSink");
        _sink.IsEnabled.Returns(true);
        _sink.MinimumLevel.Returns(LogLevel.Trace);

        _capturedEntries = new List<LogEntry>();
        _sink.When(s => s.Write(Arg.Any<LogEntry>()))
            .Do(x => _capturedEntries.Add(x.Arg<LogEntry>()));

        ((LoggerService)_logger).AddSink(_sink);
    }

    public class LogInfoTests : LoggingExtensionsTests
    {
        [Fact]
        public void LogInfo_LogsAtInformationLevel()
        {
            // Act
            _logger.LogInfo("Test message");

            // Assert
            _capturedEntries.Should().ContainSingle()
                .Which.Level.Should().Be(LogLevel.Information);
        }

        [Fact]
        public void LogInfo_IncludesCallerInfo()
        {
            // Act
            _logger.LogInfo("Test message");

            // Assert
            var entry = _capturedEntries.Single();
            entry.Source.Should().Contain("LogInfo_IncludesCallerInfo");
        }
    }

    public class LogDebugTests : LoggingExtensionsTests
    {
        [Fact]
        public void LogDebug_LogsAtDebugLevel()
        {
            // Act
            _logger.LogDebug("Debug message");

            // Assert
            _capturedEntries.Should().ContainSingle()
                .Which.Level.Should().Be(LogLevel.Debug);
        }
    }

    public class LogWarningTests : LoggingExtensionsTests
    {
        [Fact]
        public void LogWarning_LogsAtWarningLevel()
        {
            // Act
            _logger.LogWarning("Warning message");

            // Assert
            _capturedEntries.Should().ContainSingle()
                .Which.Level.Should().Be(LogLevel.Warning);
        }

        [Fact]
        public void LogWarning_IncludesException()
        {
            // Arrange
            var exception = new InvalidOperationException("Test");

            // Act
            _logger.LogWarning("Warning", exception);

            // Assert
            _capturedEntries.Single().Exception.Should().Be(exception);
        }
    }

    public class LogErrorTests : LoggingExtensionsTests
    {
        [Fact]
        public void LogError_LogsAtErrorLevel()
        {
            // Act
            _logger.LogError("Error message");

            // Assert
            _capturedEntries.Should().ContainSingle()
                .Which.Level.Should().Be(LogLevel.Error);
        }

        [Fact]
        public void LogError_IncludesException()
        {
            // Arrange
            var exception = new Exception("Test error");

            // Act
            _logger.LogError("Error", exception);

            // Assert
            _capturedEntries.Single().Exception.Should().Be(exception);
        }
    }

    public class LogStructuredTests : LoggingExtensionsTests
    {
        [Fact]
        public void LogStructured_IncludesProperties()
        {
            // Arrange
            var props = new Dictionary<string, object?>
            {
                ["UserId"] = 123,
                ["Action"] = "Login"
            };

            // Act
            _logger.LogStructured(LogLevel.Information, "User action", props);

            // Assert
            var entry = _capturedEntries.Single();
            entry.Properties.Should().ContainKey("UserId");
            entry.Properties!["UserId"].Should().Be(123);
        }
    }

    public class LogTimedTests : LoggingExtensionsTests
    {
        [Fact]
        public void LogTimed_LogsExecutionTime()
        {
            // Act
            _logger.LogTimed("Test operation", () =>
            {
                Thread.Sleep(10);
            });

            // Assert
            var entry = _capturedEntries.Single();
            entry.Message.Should().Contain("Test operation");
            entry.Message.Should().Contain("ms");
        }

        [Fact]
        public async Task LogTimedAsync_LogsExecutionTime()
        {
            // Act
            await _logger.LogTimedAsync("Async operation", async () =>
            {
                await Task.Delay(10);
            });

            // Assert
            var entry = _capturedEntries.Single();
            entry.Message.Should().Contain("Async operation");
            entry.Message.Should().Contain("completed");
        }

        [Fact]
        public async Task LogTimedAsync_WithResult_ReturnsResult()
        {
            // Act
            var result = await _logger.LogTimedAsync("Get value", async () =>
            {
                await Task.Delay(10);
                return 42;
            });

            // Assert
            result.Should().Be(42);
            _capturedEntries.Should().ContainSingle();
        }

        [Fact]
        public void LogTimed_StillLogsOnException()
        {
            // Act & Assert
            var action = () => _logger.LogTimed("Failing operation", () =>
            {
                throw new InvalidOperationException("Test");
            });

            action.Should().Throw<InvalidOperationException>();
            _capturedEntries.Should().ContainSingle();
        }
    }

    public class CreateAutoScopeTests : LoggingExtensionsTests
    {
        [Fact]
        public void CreateAutoScope_UsesFileNameAsSource()
        {
            // Act
            var scope = _logger.CreateAutoScope();

            // Assert
            scope.Source.Should().Contain("LoggingExtensionsTests");
        }
    }
}

public class LoggingScopeTests
{
    [Fact]
    public void LoggingScope_LogsEntryAndExit()
    {
        // Arrange
        var logger = new LoggerService { MinimumLevel = LogLevel.Debug };
        var capturedEntries = new List<LogEntry>();
        var sink = Substitute.For<ILogSink>();
        sink.Name.Returns("TestSink");
        sink.IsEnabled.Returns(true);
        sink.MinimumLevel.Returns(LogLevel.Trace);
        sink.When(s => s.Write(Arg.Any<LogEntry>()))
            .Do(x => capturedEntries.Add(x.Arg<LogEntry>()));
        logger.AddSink(sink);

        // Act
        using (new LoggingScope(logger, "Test operation", "TestSource"))
        {
            // Do something
        }

        // Assert
        capturedEntries.Should().HaveCount(2);
        capturedEntries[0].Message.Should().Contain("Starting");
        capturedEntries[1].Message.Should().Contain("Completed");
        capturedEntries[1].Message.Should().Contain("ms");
    }
}

