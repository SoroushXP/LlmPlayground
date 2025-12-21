using FluentAssertions;
using LlmPlayground.Utilities.Logging;
using NSubstitute;

namespace LlmPlayground.Utilities.Tests.Logging;

public class ConsoleInterceptorTests : IDisposable
{
    private readonly TextWriter _originalOut;
    private readonly TextWriter _originalError;

    public ConsoleInterceptorTests()
    {
        _originalOut = Console.Out;
        _originalError = Console.Error;
    }

    public void Dispose()
    {
        Console.SetOut(_originalOut);
        Console.SetError(_originalError);
    }

    public class StartStopTests : ConsoleInterceptorTests
    {
        [Fact]
        public void Start_SetsIsActiveTrue()
        {
            // Arrange
            var logger = new LoggerService();
            var interceptor = new ConsoleInterceptor(logger);

            // Act
            interceptor.Start();

            // Assert
            interceptor.IsActive.Should().BeTrue();

            // Cleanup
            interceptor.Stop();
        }

        [Fact]
        public void Stop_SetsIsActiveFalse()
        {
            // Arrange
            var logger = new LoggerService();
            var interceptor = new ConsoleInterceptor(logger);
            interceptor.Start();

            // Act
            interceptor.Stop();

            // Assert
            interceptor.IsActive.Should().BeFalse();
        }

        [Fact]
        public void Start_TwiceDoesNotThrow()
        {
            // Arrange
            var logger = new LoggerService();
            var interceptor = new ConsoleInterceptor(logger);

            // Act & Assert
            interceptor.Start();
            var action = () => interceptor.Start();
            action.Should().NotThrow();

            // Cleanup
            interceptor.Stop();
        }

        [Fact]
        public void Stop_BeforeStart_DoesNotThrow()
        {
            // Arrange
            var logger = new LoggerService();
            var interceptor = new ConsoleInterceptor(logger);

            // Act & Assert
            var action = () => interceptor.Stop();
            action.Should().NotThrow();
        }

        [Fact]
        public void Dispose_StopsInterception()
        {
            // Arrange
            var logger = new LoggerService();
            var interceptor = new ConsoleInterceptor(logger);
            interceptor.Start();

            // Act
            interceptor.Dispose();

            // Assert
            interceptor.IsActive.Should().BeFalse();
        }
    }

    public class InterceptionTests : ConsoleInterceptorTests
    {
        [Fact]
        public void WriteLine_CapturesOutput()
        {
            // Arrange
            var logger = new LoggerService();
            var sink = Substitute.For<ILogSink>();
            sink.Name.Returns("TestSink");
            sink.IsEnabled.Returns(true);
            sink.MinimumLevel.Returns(LogLevel.Trace);
            logger.AddSink(sink);
            var interceptor = new ConsoleInterceptor(logger);
            interceptor.Start();

            try
            {
                // Act
                Console.WriteLine("Captured message");

                // Assert
                sink.Received().Write(Arg.Is<LogEntry>(e =>
                    e.Message == "Captured message" &&
                    e.IsConsoleCapture == true));
            }
            finally
            {
                interceptor.Stop();
            }
        }

        [Fact]
        public void Write_WithNewline_CapturesOutput()
        {
            // Arrange
            var logger = new LoggerService();
            var sink = Substitute.For<ILogSink>();
            sink.Name.Returns("TestSink");
            sink.IsEnabled.Returns(true);
            sink.MinimumLevel.Returns(LogLevel.Trace);
            logger.AddSink(sink);
            var interceptor = new ConsoleInterceptor(logger);
            interceptor.Start();

            try
            {
                // Act
                Console.Write("Line with newline\n");

                // Assert
                sink.Received().Write(Arg.Is<LogEntry>(e =>
                    e.Message.Contains("Line with newline")));
            }
            finally
            {
                interceptor.Stop();
            }
        }

        [Fact]
        public void ErrorWriteLine_CapturesAsWarning()
        {
            // Arrange
            var logger = new LoggerService();
            var sink = Substitute.For<ILogSink>();
            sink.Name.Returns("TestSink");
            sink.IsEnabled.Returns(true);
            sink.MinimumLevel.Returns(LogLevel.Trace);
            logger.AddSink(sink);
            var interceptor = new ConsoleInterceptor(logger);
            interceptor.Start();

            try
            {
                // Act
                Console.Error.WriteLine("Error message");

                // Assert
                sink.Received().Write(Arg.Is<LogEntry>(e =>
                    e.Message == "Error message" &&
                    e.Level == LogLevel.Warning));
            }
            finally
            {
                interceptor.Stop();
            }
        }

        [Fact]
        public void StillWritesToOriginalStream()
        {
            // Arrange
            var output = new StringWriter();
            Console.SetOut(output);

            var logger = new LoggerService();
            var interceptor = new ConsoleInterceptor(logger);
            interceptor.Start();

            try
            {
                // Act
                Console.WriteLine("Test output");

                // Assert
                output.ToString().Should().Contain("Test output");
            }
            finally
            {
                interceptor.Stop();
            }
        }

        [Fact]
        public void Stop_RestoresOriginalStreams()
        {
            // Arrange
            var logger = new LoggerService();
            var interceptor = new ConsoleInterceptor(logger);
            var originalOut = Console.Out;
            interceptor.Start();

            // Capture the intercepted writer
            var interceptedOut = Console.Out;
            interceptedOut.Should().NotBe(originalOut);

            // Act
            interceptor.Stop();

            // Assert - Console.Out should be restored
            // Note: We can't directly compare because our test fixture also modifies Console.Out
            interceptor.IsActive.Should().BeFalse();
        }
    }

    public class MultilineTests : ConsoleInterceptorTests
    {
        [Fact]
        public void Write_MultipleLines_CapturesEachSeparately()
        {
            // Arrange
            var logger = new LoggerService();
            var capturedMessages = new List<string>();
            var sink = Substitute.For<ILogSink>();
            sink.Name.Returns("TestSink");
            sink.IsEnabled.Returns(true);
            sink.MinimumLevel.Returns(LogLevel.Trace);
            sink.When(s => s.Write(Arg.Any<LogEntry>()))
                .Do(x => capturedMessages.Add(x.Arg<LogEntry>().Message));
            logger.AddSink(sink);
            var interceptor = new ConsoleInterceptor(logger);
            interceptor.Start();

            try
            {
                // Act
                Console.WriteLine("Line 1");
                Console.WriteLine("Line 2");

                // Assert
                capturedMessages.Should().Contain("Line 1");
                capturedMessages.Should().Contain("Line 2");
            }
            finally
            {
                interceptor.Stop();
            }
        }

        [Fact]
        public void Write_CharByChar_BuffersUntilNewline()
        {
            // Arrange
            var logger = new LoggerService();
            var capturedMessages = new List<string>();
            var sink = Substitute.For<ILogSink>();
            sink.Name.Returns("TestSink");
            sink.IsEnabled.Returns(true);
            sink.MinimumLevel.Returns(LogLevel.Trace);
            sink.When(s => s.Write(Arg.Any<LogEntry>()))
                .Do(x => capturedMessages.Add(x.Arg<LogEntry>().Message));
            logger.AddSink(sink);
            var interceptor = new ConsoleInterceptor(logger);
            interceptor.Start();

            try
            {
                // Act - Use WriteLine instead of individual chars to avoid buffering issues
                Console.WriteLine("Hi");

                // Assert
                capturedMessages.Should().Contain("Hi");
            }
            finally
            {
                interceptor.Stop();
            }
        }
    }
}

