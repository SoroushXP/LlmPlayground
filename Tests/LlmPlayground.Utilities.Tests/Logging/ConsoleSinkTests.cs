using FluentAssertions;
using LlmPlayground.Utilities.Logging;
using LlmPlayground.Utilities.Logging.Sinks;

namespace LlmPlayground.Utilities.Tests.Logging;

public class ConsoleSinkTests
{
    public class WriteTests
    {
        [Fact]
        public void Write_WritesToOutputStream()
        {
            // Arrange
            var output = new StringWriter();
            var error = new StringWriter();
            var sink = new ConsoleSink(output, error, useColors: false);
            var entry = new LogEntry
            {
                Message = "Test message",
                Level = LogLevel.Information
            };

            // Act
            sink.Write(entry);

            // Assert
            output.ToString().Should().Contain("Test message");
        }

        [Fact]
        public void Write_ErrorLevel_WritesToErrorStream()
        {
            // Arrange
            var output = new StringWriter();
            var error = new StringWriter();
            var sink = new ConsoleSink(output, error, useColors: false, writeErrorsToStdErr: true);
            var entry = new LogEntry
            {
                Message = "Error message",
                Level = LogLevel.Error
            };

            // Act
            sink.Write(entry);

            // Assert
            error.ToString().Should().Contain("Error message");
            output.ToString().Should().BeEmpty();
        }

        [Fact]
        public void Write_CriticalLevel_WritesToErrorStream()
        {
            // Arrange
            var output = new StringWriter();
            var error = new StringWriter();
            var sink = new ConsoleSink(output, error, useColors: false, writeErrorsToStdErr: true);
            var entry = new LogEntry
            {
                Message = "Critical message",
                Level = LogLevel.Critical
            };

            // Act
            sink.Write(entry);

            // Assert
            error.ToString().Should().Contain("Critical message");
        }

        [Fact]
        public void Write_WarningLevel_WritesToStdOut()
        {
            // Arrange
            var output = new StringWriter();
            var error = new StringWriter();
            var sink = new ConsoleSink(output, error, useColors: false, writeErrorsToStdErr: true);
            var entry = new LogEntry
            {
                Message = "Warning message",
                Level = LogLevel.Warning
            };

            // Act
            sink.Write(entry);

            // Assert
            output.ToString().Should().Contain("Warning message");
        }

        [Fact]
        public void Write_BelowMinimumLevel_DoesNotWrite()
        {
            // Arrange
            var output = new StringWriter();
            var error = new StringWriter();
            var sink = new ConsoleSink(output, error, useColors: false)
            {
                MinimumLevel = LogLevel.Warning
            };
            var entry = new LogEntry
            {
                Message = "Debug message",
                Level = LogLevel.Debug
            };

            // Act
            sink.Write(entry);

            // Assert
            output.ToString().Should().BeEmpty();
        }

        [Fact]
        public void Write_UsesFormatter()
        {
            // Arrange
            var output = new StringWriter();
            var error = new StringWriter();
            var formatter = new DefaultLogFormatter(new LogFormatterOptions
            {
                IncludeTimestamp = false,
                IncludeSource = true
            });
            var sink = new ConsoleSink(output, error, formatter, useColors: false);
            var entry = new LogEntry
            {
                Message = "Test",
                Source = "MySource"
            };

            // Act
            sink.Write(entry);

            // Assert
            output.ToString().Should().Contain("MySource");
        }
    }

    public class PropertyTests
    {
        [Fact]
        public void Name_ReturnsConsole()
        {
            // Arrange
            var output = new StringWriter();
            var error = new StringWriter();
            var sink = new ConsoleSink(output, error);

            // Assert
            sink.Name.Should().Be("Console");
        }

        [Fact]
        public void IsEnabled_BeforeDispose_ReturnsTrue()
        {
            // Arrange
            var output = new StringWriter();
            var error = new StringWriter();
            var sink = new ConsoleSink(output, error);

            // Assert
            sink.IsEnabled.Should().BeTrue();
        }

        [Fact]
        public void IsEnabled_AfterDispose_ReturnsFalse()
        {
            // Arrange
            var output = new StringWriter();
            var error = new StringWriter();
            var sink = new ConsoleSink(output, error);

            // Act
            sink.Dispose();

            // Assert
            sink.IsEnabled.Should().BeFalse();
        }
    }

    public class FilteredConsoleSinkTests
    {
        [Fact]
        public void Write_ConsoleCapture_IsFiltered()
        {
            // Arrange
            var output = new StringWriter();
            Console.SetOut(output);
            var sink = new FilteredConsoleSink(useColors: false);
            var entry = new LogEntry
            {
                Message = "Console capture",
                IsConsoleCapture = true
            };

            // Act
            sink.Write(entry);

            // Assert
            output.ToString().Should().NotContain("Console capture");
        }

        [Fact]
        public void Write_NonConsoleCapture_IsWritten()
        {
            // Arrange
            var output = new StringWriter();
            var originalOut = Console.Out;
            try
            {
                Console.SetOut(output);
                var sink = new FilteredConsoleSink(useColors: false);
                var entry = new LogEntry
                {
                    Message = "Normal log",
                    IsConsoleCapture = false
                };

                // Act
                sink.Write(entry);

                // Assert
                output.ToString().Should().Contain("Normal log");
            }
            finally
            {
                Console.SetOut(originalOut);
            }
        }

        [Fact]
        public void Name_ReturnsFilteredConsole()
        {
            // Arrange
            var sink = new FilteredConsoleSink();

            // Assert
            sink.Name.Should().Be("FilteredConsole");
        }
    }
}

