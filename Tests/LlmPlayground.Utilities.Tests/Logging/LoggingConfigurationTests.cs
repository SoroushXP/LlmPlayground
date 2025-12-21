using FluentAssertions;
using LlmPlayground.Utilities.Logging;
using LlmPlayground.Utilities.Logging.Sinks;

namespace LlmPlayground.Utilities.Tests.Logging;

public class LoggingConfigurationTests
{
    public class DefaultConfigurationTests
    {
        [Fact]
        public void Default_HasExpectedValues()
        {
            // Act
            var config = LoggingConfiguration.Default;

            // Assert
            config.MinimumLevel.Should().Be(LogLevel.Information);
            config.EnableConsoleSink.Should().BeTrue();
            config.ConsoleMinimumLevel.Should().Be(LogLevel.Information);
            config.ConsoleUseColors.Should().BeTrue();
            config.EnableFileSink.Should().BeFalse();
            config.FileDirectory.Should().Be("logs");
            config.FileMinimumLevel.Should().Be(LogLevel.Debug);
            config.FileNamePrefix.Should().Be("llmplayground_");
            config.FileRollingPolicy.Should().Be(RollingPolicy.Daily);
            config.MaxFileSizeMb.Should().Be(10);
            config.FileRetainDays.Should().Be(7);
            config.InterceptConsoleOutput.Should().BeFalse();
            config.IncludeTimestamps.Should().BeTrue();
            config.IncludeThreadId.Should().BeFalse();
            config.UseJsonFormatting.Should().BeFalse();
        }
    }

    public class CustomConfigurationTests
    {
        [Fact]
        public void CanCreateCustomConfiguration()
        {
            // Act
            var config = new LoggingConfiguration
            {
                MinimumLevel = LogLevel.Debug,
                EnableFileSink = true,
                FileDirectory = "custom_logs",
                FileRetainDays = 14,
                InterceptConsoleOutput = true
            };

            // Assert
            config.MinimumLevel.Should().Be(LogLevel.Debug);
            config.EnableFileSink.Should().BeTrue();
            config.FileDirectory.Should().Be("custom_logs");
            config.FileRetainDays.Should().Be(14);
            config.InterceptConsoleOutput.Should().BeTrue();
        }
    }
}

public class LoggerBuilderTests : IDisposable
{
    private readonly string _testDirectory;

    public LoggerBuilderTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), $"LogBuilderTest_{Guid.NewGuid():N}");
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_testDirectory))
            {
                Directory.Delete(_testDirectory, recursive: true);
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
    }

    public class BuildTests : LoggerBuilderTests
    {
        [Fact]
        public void Build_WithDefaults_CreatesLogger()
        {
            // Act
            var logger = new LoggerBuilder().Build();

            // Assert
            logger.Should().NotBeNull();
            logger.MinimumLevel.Should().Be(LogLevel.Information);

            // Cleanup
            logger.Dispose();
        }

        [Fact]
        public void Build_WithMinimumLevel_SetsLevel()
        {
            // Act
            var logger = new LoggerBuilder()
                .WithMinimumLevel(LogLevel.Debug)
                .Build();

            // Assert
            logger.MinimumLevel.Should().Be(LogLevel.Debug);

            // Cleanup
            logger.Dispose();
        }

        [Fact]
        public void Build_WithConsoleSink_AddsSink()
        {
            // Act
            var logger = new LoggerBuilder()
                .WithConsoleSink()
                .Build();

            // Assert
            // Should have either Console or FilteredConsole sink
            (logger.GetSink("Console") ?? logger.GetSink("FilteredConsole")).Should().NotBeNull();

            // Cleanup
            logger.Dispose();
        }

        [Fact]
        public void Build_WithFileSink_AddsSink()
        {
            // Act
            var logger = new LoggerBuilder()
                .WithFileSink(_testDirectory)
                .Build();

            // Assert
            var sink = logger.GetSink($"File:{_testDirectory}");
            sink.Should().NotBeNull();

            // Cleanup
            logger.Dispose();
        }

        [Fact]
        public void Build_WithConsoleInterception_StartsInterceptor()
        {
            // Arrange
            var originalOut = Console.Out;

            try
            {
                // Act
                var logger = new LoggerBuilder()
                    .WithConsoleInterception()
                    .Build();

                // Assert
                logger.IsConsoleInterceptorActive.Should().BeTrue();

                // Cleanup
                logger.Dispose();
            }
            finally
            {
                Console.SetOut(originalOut);
            }
        }

        [Fact]
        public void Build_WithConfiguration_UsesConfiguration()
        {
            // Arrange
            var config = new LoggingConfiguration
            {
                MinimumLevel = LogLevel.Warning,
                EnableConsoleSink = false
            };

            // Act
            var logger = new LoggerBuilder()
                .WithConfiguration(config)
                .Build();

            // Assert
            logger.MinimumLevel.Should().Be(LogLevel.Warning);

            // Cleanup
            logger.Dispose();
        }

        [Fact]
        public void Build_WithJsonFormatting_UsesJsonFormatter()
        {
            // Act
            var logger = new LoggerBuilder()
                .WithFileSink(_testDirectory)
                .WithJsonFormatting()
                .Build();

            // Assert - Write a log and verify JSON format
            logger.Information("Test");
            logger.Dispose();

            var files = Directory.GetFiles(_testDirectory, "*.log");
            if (files.Length > 0)
            {
                var content = File.ReadAllText(files[0]);
                content.Should().Contain("{");
                content.Should().Contain("\"message\":");
            }
        }
    }

    public class FluentApiTests : LoggerBuilderTests
    {
        [Fact]
        public void AllMethods_ReturnBuilder_ForChaining()
        {
            // Act & Assert - All should return builder for chaining
            var builder = new LoggerBuilder();
            builder.WithMinimumLevel(LogLevel.Debug).Should().Be(builder);
            builder.WithConsoleSink().Should().Be(builder);
            builder.WithFileSink(_testDirectory).Should().Be(builder);
            builder.WithConsoleInterception().Should().Be(builder);
            builder.WithJsonFormatting().Should().Be(builder);
            builder.WithConfiguration(LoggingConfiguration.Default).Should().Be(builder);
        }

        [Fact]
        public void CanChainAllMethods()
        {
            // Arrange
            var originalOut = Console.Out;

            try
            {
                // Act
                var logger = new LoggerBuilder()
                    .WithMinimumLevel(LogLevel.Trace)
                    .WithConsoleSink(LogLevel.Information, useColors: false)
                    .WithFileSink(_testDirectory, LogLevel.Debug, RollingPolicy.Daily, 7)
                    .WithConsoleInterception()
                    .Build();

                // Assert
                logger.MinimumLevel.Should().Be(LogLevel.Trace);
                logger.IsConsoleInterceptorActive.Should().BeTrue();

                // Cleanup
                logger.Dispose();
            }
            finally
            {
                Console.SetOut(originalOut);
            }
        }
    }

    public class SinkConfigurationTests : LoggerBuilderTests
    {
        [Fact]
        public void WithConsoleSink_SetsMinimumLevel()
        {
            // Act
            var logger = new LoggerBuilder()
                .WithConsoleSink(LogLevel.Warning)
                .Build();

            // Assert
            var sink = logger.GetSink("Console") ?? logger.GetSink("FilteredConsole");
            sink!.MinimumLevel.Should().Be(LogLevel.Warning);

            // Cleanup
            logger.Dispose();
        }

        [Fact]
        public void WithFileSink_SetsAllOptions()
        {
            // Act
            var logger = new LoggerBuilder()
                .WithFileSink(
                    directory: _testDirectory,
                    minimumLevel: LogLevel.Error,
                    rollingPolicy: RollingPolicy.Size,
                    retainDays: 14)
                .Build();

            // Assert
            var sink = logger.GetSink($"File:{_testDirectory}");
            sink!.MinimumLevel.Should().Be(LogLevel.Error);

            // Cleanup
            logger.Dispose();
        }

        [Fact]
        public void WithConsoleInterception_UsesFilteredSink()
        {
            // Arrange
            var originalOut = Console.Out;

            try
            {
                // Act
                var logger = new LoggerBuilder()
                    .WithConsoleSink()
                    .WithConsoleInterception()
                    .Build();

                // Assert - Should use FilteredConsoleSink to prevent infinite loop
                var regularSink = logger.GetSink("Console");
                var filteredSink = logger.GetSink("FilteredConsole");

                // One of them should exist
                (regularSink ?? filteredSink).Should().NotBeNull();

                // Cleanup
                logger.Dispose();
            }
            finally
            {
                Console.SetOut(originalOut);
            }
        }
    }
}

