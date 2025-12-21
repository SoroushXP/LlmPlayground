using FluentAssertions;
using LlmPlayground.Utilities.Logging;
using NSubstitute;

namespace LlmPlayground.Utilities.Tests.Logging;

public class LoggerServiceTests
{
    public class LoggingTests
    {
        [Fact]
        public void Log_WithValidEntry_WritesToSinks()
        {
            // Arrange
            var logger = new LoggerService();
            var sink = Substitute.For<ILogSink>();
            sink.Name.Returns("TestSink");
            sink.IsEnabled.Returns(true);
            sink.MinimumLevel.Returns(LogLevel.Trace);
            logger.AddSink(sink);

            // Act
            logger.Information("Test message", "TestSource");

            // Assert
            sink.Received(1).Write(Arg.Is<LogEntry>(e =>
                e.Message == "Test message" &&
                e.Source == "TestSource" &&
                e.Level == LogLevel.Information));
        }

        [Fact]
        public void Log_BelowMinimumLevel_DoesNotWrite()
        {
            // Arrange
            var logger = new LoggerService { MinimumLevel = LogLevel.Warning };
            var sink = Substitute.For<ILogSink>();
            sink.Name.Returns("TestSink");
            sink.IsEnabled.Returns(true);
            sink.MinimumLevel.Returns(LogLevel.Trace);
            logger.AddSink(sink);

            // Act
            logger.Debug("Debug message");

            // Assert
            sink.DidNotReceive().Write(Arg.Any<LogEntry>());
        }

        [Fact]
        public void Log_SinkBelowMinimumLevel_DoesNotWriteToThatSink()
        {
            // Arrange
            var logger = new LoggerService { MinimumLevel = LogLevel.Debug };
            var sink = Substitute.For<ILogSink>();
            sink.Name.Returns("TestSink");
            sink.IsEnabled.Returns(true);
            sink.MinimumLevel.Returns(LogLevel.Error);
            logger.AddSink(sink);

            // Act
            logger.Information("Info message");

            // Assert
            sink.DidNotReceive().Write(Arg.Any<LogEntry>());
        }

        [Theory]
        [InlineData(LogLevel.Trace)]
        [InlineData(LogLevel.Debug)]
        [InlineData(LogLevel.Information)]
        [InlineData(LogLevel.Warning)]
        [InlineData(LogLevel.Error)]
        [InlineData(LogLevel.Critical)]
        public void Log_AllLevelMethods_WriteCorrectLevel(LogLevel level)
        {
            // Arrange
            var logger = new LoggerService { MinimumLevel = LogLevel.Trace };
            var sink = Substitute.For<ILogSink>();
            sink.Name.Returns("TestSink");
            sink.IsEnabled.Returns(true);
            sink.MinimumLevel.Returns(LogLevel.Trace);
            logger.AddSink(sink);

            // Act
            switch (level)
            {
                case LogLevel.Trace: logger.Trace("msg"); break;
                case LogLevel.Debug: logger.Debug("msg"); break;
                case LogLevel.Information: logger.Information("msg"); break;
                case LogLevel.Warning: logger.Warning("msg"); break;
                case LogLevel.Error: logger.Error("msg"); break;
                case LogLevel.Critical: logger.Critical("msg"); break;
            }

            // Assert
            sink.Received(1).Write(Arg.Is<LogEntry>(e => e.Level == level));
        }

        [Fact]
        public void Log_WithException_IncludesException()
        {
            // Arrange
            var logger = new LoggerService();
            var sink = Substitute.For<ILogSink>();
            sink.Name.Returns("TestSink");
            sink.IsEnabled.Returns(true);
            sink.MinimumLevel.Returns(LogLevel.Trace);
            logger.AddSink(sink);
            var exception = new InvalidOperationException("Test error");

            // Act
            logger.Error("Error occurred", exception: exception);

            // Assert
            sink.Received(1).Write(Arg.Is<LogEntry>(e => e.Exception == exception));
        }

        [Fact]
        public void Log_ToMultipleSinks_WritesToAll()
        {
            // Arrange
            var logger = new LoggerService();
            var sink1 = Substitute.For<ILogSink>();
            sink1.Name.Returns("Sink1");
            sink1.IsEnabled.Returns(true);
            sink1.MinimumLevel.Returns(LogLevel.Trace);

            var sink2 = Substitute.For<ILogSink>();
            sink2.Name.Returns("Sink2");
            sink2.IsEnabled.Returns(true);
            sink2.MinimumLevel.Returns(LogLevel.Trace);

            logger.AddSink(sink1);
            logger.AddSink(sink2);

            // Act
            logger.Information("Test");

            // Assert
            sink1.Received(1).Write(Arg.Any<LogEntry>());
            sink2.Received(1).Write(Arg.Any<LogEntry>());
        }

        [Fact]
        public void Log_DisabledSink_DoesNotWrite()
        {
            // Arrange
            var logger = new LoggerService();
            var sink = Substitute.For<ILogSink>();
            sink.Name.Returns("TestSink");
            sink.IsEnabled.Returns(false);
            sink.MinimumLevel.Returns(LogLevel.Trace);
            logger.AddSink(sink);

            // Act
            logger.Information("Test");

            // Assert
            sink.DidNotReceive().Write(Arg.Any<LogEntry>());
        }
    }

    public class SinkManagementTests
    {
        [Fact]
        public void AddSink_AddsSinkToLogger()
        {
            // Arrange
            var logger = new LoggerService();
            var sink = Substitute.For<ILogSink>();
            sink.Name.Returns("TestSink");

            // Act
            logger.AddSink(sink);
            var retrieved = logger.GetSink("TestSink");

            // Assert
            retrieved.Should().Be(sink);
        }

        [Fact]
        public void RemoveSink_RemovesSink()
        {
            // Arrange
            var logger = new LoggerService();
            var sink = Substitute.For<ILogSink>();
            sink.Name.Returns("TestSink");
            logger.AddSink(sink);

            // Act
            var removed = logger.RemoveSink("TestSink");
            var retrieved = logger.GetSink("TestSink");

            // Assert
            removed.Should().BeTrue();
            retrieved.Should().BeNull();
        }

        [Fact]
        public void RemoveSink_NonExistent_ReturnsFalse()
        {
            // Arrange
            var logger = new LoggerService();

            // Act
            var removed = logger.RemoveSink("NonExistent");

            // Assert
            removed.Should().BeFalse();
        }

        [Fact]
        public void GetSink_NonExistent_ReturnsNull()
        {
            // Arrange
            var logger = new LoggerService();

            // Act
            var sink = logger.GetSink("NonExistent");

            // Assert
            sink.Should().BeNull();
        }
    }

    public class CorrelationTests
    {
        [Fact]
        public void BeginCorrelationScope_SetsCorrelationId()
        {
            // Arrange
            var logger = new LoggerService();
            var sink = Substitute.For<ILogSink>();
            sink.Name.Returns("TestSink");
            sink.IsEnabled.Returns(true);
            sink.MinimumLevel.Returns(LogLevel.Trace);
            logger.AddSink(sink);

            // Act
            using (logger.BeginCorrelationScope("test-correlation"))
            {
                logger.Information("Test");
            }

            // Assert
            sink.Received(1).Write(Arg.Is<LogEntry>(e => e.CorrelationId == "test-correlation"));
        }

        [Fact]
        public void BeginCorrelationScope_WithoutId_GeneratesId()
        {
            // Arrange
            var logger = new LoggerService();
            var sink = Substitute.For<ILogSink>();
            sink.Name.Returns("TestSink");
            sink.IsEnabled.Returns(true);
            sink.MinimumLevel.Returns(LogLevel.Trace);
            logger.AddSink(sink);

            // Act
            using (logger.BeginCorrelationScope())
            {
                logger.Information("Test");
            }

            // Assert
            sink.Received(1).Write(Arg.Is<LogEntry>(e => !string.IsNullOrEmpty(e.CorrelationId)));
        }

        [Fact]
        public void BeginCorrelationScope_RestoresPreviousIdOnDispose()
        {
            // Arrange
            var logger = new LoggerService();
            logger.CorrelationId = "original";

            // Act
            using (logger.BeginCorrelationScope("scoped"))
            {
                logger.CorrelationId.Should().Be("scoped");
            }

            // Assert
            logger.CorrelationId.Should().Be("original");
        }

        [Fact]
        public void CorrelationId_CanBeSetDirectly()
        {
            // Arrange
            var logger = new LoggerService();
            var sink = Substitute.For<ILogSink>();
            sink.Name.Returns("TestSink");
            sink.IsEnabled.Returns(true);
            sink.MinimumLevel.Returns(LogLevel.Trace);
            logger.AddSink(sink);

            // Act
            logger.CorrelationId = "direct-set";
            logger.Information("Test");

            // Assert
            sink.Received(1).Write(Arg.Is<LogEntry>(e => e.CorrelationId == "direct-set"));
        }
    }

    public class ScopeTests
    {
        [Fact]
        public void CreateScope_CreatesLoggerWithSource()
        {
            // Arrange
            var logger = new LoggerService();
            var sink = Substitute.For<ILogSink>();
            sink.Name.Returns("TestSink");
            sink.IsEnabled.Returns(true);
            sink.MinimumLevel.Returns(LogLevel.Trace);
            logger.AddSink(sink);

            // Act
            var scope = logger.CreateScope("MyService");
            scope.Information("Test");

            // Assert
            scope.Source.Should().Be("MyService");
            sink.Received(1).Write(Arg.Is<LogEntry>(e => e.Source == "MyService"));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void CreateScope_WithInvalidSource_Throws(string? source)
        {
            // Arrange
            var logger = new LoggerService();

            // Act & Assert
            var action = () => logger.CreateScope(source!);
            action.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Scope_AllMethods_UseCorrectSource()
        {
            // Arrange
            var logger = new LoggerService { MinimumLevel = LogLevel.Trace };
            var sink = Substitute.For<ILogSink>();
            sink.Name.Returns("TestSink");
            sink.IsEnabled.Returns(true);
            sink.MinimumLevel.Returns(LogLevel.Trace);
            logger.AddSink(sink);
            var scope = logger.CreateScope("TestScope");

            // Act
            scope.Trace("t");
            scope.Debug("d");
            scope.Information("i");
            scope.Warning("w");
            scope.Error("e");
            scope.Critical("c");

            // Assert
            sink.Received(6).Write(Arg.Is<LogEntry>(e => e.Source == "TestScope"));
        }
    }

    public class DisposeTests
    {
        [Fact]
        public void Dispose_DisposesAllSinks()
        {
            // Arrange
            var logger = new LoggerService();
            var sink = Substitute.For<ILogSink>();
            sink.Name.Returns("TestSink");
            logger.AddSink(sink);

            // Act
            logger.Dispose();

            // Assert
            sink.Received(1).Dispose();
        }

        [Fact]
        public async Task DisposeAsync_DisposesAllSinksAsync()
        {
            // Arrange
            var logger = new LoggerService();
            var sink = Substitute.For<ILogSink>();
            sink.Name.Returns("TestSink");
            logger.AddSink(sink);

            // Act
            await logger.DisposeAsync();

            // Assert
            await sink.Received(1).DisposeAsync();
        }

        [Fact]
        public void AfterDispose_LoggingDoesNothing()
        {
            // Arrange
            var logger = new LoggerService();
            var sink = Substitute.For<ILogSink>();
            sink.Name.Returns("TestSink");
            sink.IsEnabled.Returns(true);
            sink.MinimumLevel.Returns(LogLevel.Trace);
            logger.AddSink(sink);
            logger.Dispose();
            sink.ClearReceivedCalls();

            // Act
            logger.Information("After dispose");

            // Assert
            sink.DidNotReceive().Write(Arg.Any<LogEntry>());
        }
    }

    public class FlushTests
    {
        [Fact]
        public async Task FlushAsync_FlushesAllSinks()
        {
            // Arrange
            var logger = new LoggerService();
            var sink = Substitute.For<ILogSink>();
            sink.Name.Returns("TestSink");
            logger.AddSink(sink);

            // Act
            await logger.FlushAsync();

            // Assert
            await sink.Received(1).FlushAsync(Arg.Any<CancellationToken>());
        }
    }
}

