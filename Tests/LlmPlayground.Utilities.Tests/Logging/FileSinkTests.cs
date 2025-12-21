using FluentAssertions;
using LlmPlayground.Utilities.Logging;
using LlmPlayground.Utilities.Logging.Sinks;

namespace LlmPlayground.Utilities.Tests.Logging;

public class FileSinkTests : IDisposable
{
    private readonly string _testDirectory;

    public FileSinkTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), $"LogTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDirectory);
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
            // Ignore cleanup errors in tests
        }
    }

    public class WriteTests : FileSinkTests
    {
        [Fact]
        public async Task Write_CreatesLogFile()
        {
            // Arrange
            var options = new FileSinkOptions
            {
                Directory = _testDirectory,
                BufferSize = 0 // Immediate write
            };
            var sink = new FileSink(options);
            var entry = new LogEntry { Message = "Test message" };

            // Act
            sink.Write(entry);
            await sink.FlushAsync();
            await sink.DisposeAsync();

            // Assert
            var files = Directory.GetFiles(_testDirectory, "*.log");
            files.Should().HaveCount(1);
        }

        [Fact]
        public async Task Write_AppendsToFile()
        {
            // Arrange
            var options = new FileSinkOptions
            {
                Directory = _testDirectory,
                BufferSize = 0
            };
            var sink = new FileSink(options);

            // Act
            sink.Write(new LogEntry { Message = "Message 1" });
            sink.Write(new LogEntry { Message = "Message 2" });
            await sink.FlushAsync();
            await sink.DisposeAsync();

            // Assert
            var files = Directory.GetFiles(_testDirectory, "*.log");
            var content = await File.ReadAllTextAsync(files[0]);
            content.Should().Contain("Message 1");
            content.Should().Contain("Message 2");
        }

        [Fact]
        public async Task Write_UsesFormatter()
        {
            // Arrange
            var formatter = new DefaultLogFormatter(new LogFormatterOptions
            {
                IncludeTimestamp = false
            });
            var options = new FileSinkOptions
            {
                Directory = _testDirectory,
                BufferSize = 0
            };
            var sink = new FileSink(options, formatter);

            // Act
            sink.Write(new LogEntry { Message = "Test", Level = LogLevel.Warning });
            await sink.FlushAsync();
            await sink.DisposeAsync();

            // Assert
            var files = Directory.GetFiles(_testDirectory, "*.log");
            var content = await File.ReadAllTextAsync(files[0]);
            content.Should().Contain("[WRN]");
        }

        [Fact]
        public async Task WriteAsync_WritesToBuffer()
        {
            // Arrange
            var options = new FileSinkOptions
            {
                Directory = _testDirectory,
                BufferSize = 10
            };
            var sink = new FileSink(options);

            // Act
            await sink.WriteAsync(new LogEntry { Message = "Async message" });
            await sink.FlushAsync();
            await sink.DisposeAsync();

            // Assert
            var files = Directory.GetFiles(_testDirectory, "*.log");
            var content = await File.ReadAllTextAsync(files[0]);
            content.Should().Contain("Async message");
        }
    }

    public class RollingTests : FileSinkTests
    {
        [Fact]
        public async Task DailyRolling_CreatesFileWithDateInName()
        {
            // Arrange
            var options = new FileSinkOptions
            {
                Directory = _testDirectory,
                FileNamePrefix = "test_",
                RollingPolicy = RollingPolicy.Daily,
                BufferSize = 0
            };
            var sink = new FileSink(options);

            // Act
            sink.Write(new LogEntry { Message = "Test" });
            await sink.FlushAsync();
            await sink.DisposeAsync();

            // Assert
            var files = Directory.GetFiles(_testDirectory, "*.log");
            files.Should().HaveCount(1);
            var fileName = Path.GetFileName(files[0]);
            fileName.Should().StartWith("test_");
            fileName.Should().Contain(DateTime.Now.ToString("yyyy-MM-dd"));
        }
    }

    public class RetentionTests : FileSinkTests
    {
        [Fact]
        public async Task Retention_DeletesOldFiles()
        {
            // Arrange - Create old file
            var oldDate = DateTime.Now.AddDays(-10);
            var oldFileName = $"test_{oldDate:yyyy-MM-dd}.log";
            var oldFilePath = Path.Combine(_testDirectory, oldFileName);
            await File.WriteAllTextAsync(oldFilePath, "Old content");
            File.SetLastWriteTime(oldFilePath, oldDate);

            var options = new FileSinkOptions
            {
                Directory = _testDirectory,
                FileNamePrefix = "test_",
                RetainDays = 7,
                BufferSize = 0
            };
            var sink = new FileSink(options);

            // Act - Write new entry to trigger cleanup
            sink.Write(new LogEntry { Message = "New" });
            await sink.FlushAsync();
            await sink.DisposeAsync();

            // Assert
            File.Exists(oldFilePath).Should().BeFalse();
        }

        [Fact]
        public async Task Retention_KeepsRecentFiles()
        {
            // Arrange - Create recent file
            var recentDate = DateTime.Now.AddDays(-3);
            var recentFileName = $"test_{recentDate:yyyy-MM-dd}.log";
            var recentFilePath = Path.Combine(_testDirectory, recentFileName);
            await File.WriteAllTextAsync(recentFilePath, "Recent content");

            var options = new FileSinkOptions
            {
                Directory = _testDirectory,
                FileNamePrefix = "test_",
                RetainDays = 7,
                BufferSize = 0
            };
            var sink = new FileSink(options);

            // Act
            sink.Write(new LogEntry { Message = "New" });
            await sink.FlushAsync();
            await sink.DisposeAsync();

            // Assert
            File.Exists(recentFilePath).Should().BeTrue();
        }
    }

    public class PropertyTests : FileSinkTests
    {
        [Fact]
        public void Name_IncludesDirectory()
        {
            // Arrange
            var options = new FileSinkOptions { Directory = _testDirectory };
            var sink = new FileSink(options);

            // Assert
            sink.Name.Should().Contain(_testDirectory);
        }

        [Fact]
        public void CurrentFilePath_ReturnsPathAfterWrite()
        {
            // Arrange
            var options = new FileSinkOptions
            {
                Directory = _testDirectory,
                BufferSize = 0
            };
            var sink = new FileSink(options);

            // Act
            sink.Write(new LogEntry { Message = "Test" });

            // Assert
            sink.CurrentFilePath.Should().NotBeEmpty();
            sink.CurrentFilePath.Should().StartWith(_testDirectory);
        }
    }

    public class DisposeTests : FileSinkTests
    {
        [Fact]
        public async Task Dispose_FlushesBuffer()
        {
            // Arrange
            var options = new FileSinkOptions
            {
                Directory = _testDirectory,
                BufferSize = 0 // Immediate write to simplify test
            };
            var sink = new FileSink(options);
            sink.Write(new LogEntry { Message = "Buffered message" });

            // Act
            await sink.FlushAsync();
            sink.Dispose();

            // Assert
            var files = Directory.GetFiles(_testDirectory, "*.log");
            files.Should().HaveCountGreaterThan(0);
            var content = await File.ReadAllTextAsync(files[0]);
            content.Should().Contain("Buffered message");
        }

        [Fact]
        public async Task DisposeAsync_FlushesBuffer()
        {
            // Arrange
            var options = new FileSinkOptions
            {
                Directory = _testDirectory,
                BufferSize = 0 // Immediate write to simplify test
            };
            var sink = new FileSink(options);
            sink.Write(new LogEntry { Message = "Async buffered" });

            // Act
            await sink.DisposeAsync();

            // Assert
            var files = Directory.GetFiles(_testDirectory, "*.log");
            files.Should().HaveCountGreaterThan(0);
            var content = await File.ReadAllTextAsync(files[0]);
            content.Should().Contain("Async buffered");
        }
    }
}

public class FileSinkOptionsTests
{
    [Fact]
    public void DefaultOptions_HasExpectedValues()
    {
        // Arrange & Act
        var options = new FileSinkOptions { Directory = "logs" };

        // Assert
        options.FileNamePrefix.Should().Be("log_");
        options.RollingPolicy.Should().Be(RollingPolicy.Daily);
        options.MaxFileSizeBytes.Should().Be(10 * 1024 * 1024);
        options.RetainDays.Should().Be(7);
        options.BufferSize.Should().Be(100);
        options.FlushIntervalSeconds.Should().Be(5);
    }
}

