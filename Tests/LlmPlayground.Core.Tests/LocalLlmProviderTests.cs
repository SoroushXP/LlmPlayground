using FluentAssertions;
using LlmPlayground.Core;

namespace LlmPlayground.Core.Tests;

public class LocalLlmProviderTests
{
    private const string NonExistentModelPath = "non_existent_model.gguf";

    [Fact]
    public void Constructor_WithNullConfiguration_ShouldThrowArgumentNullException()
    {
        // Arrange & Act
        var act = () => new LocalLlmProvider(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("configuration");
    }

    [Fact]
    public void Constructor_WithEmptyModelPath_ShouldThrowArgumentException()
    {
        // Arrange
        var config = new LocalLlmConfiguration { ModelPath = "" };

        // Act
        var act = () => new LocalLlmProvider(config);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("configuration")
            .WithMessage("*Model path cannot be empty*");
    }

    [Fact]
    public void Constructor_WithWhitespaceModelPath_ShouldThrowArgumentException()
    {
        // Arrange
        var config = new LocalLlmConfiguration { ModelPath = "   " };

        // Act
        var act = () => new LocalLlmProvider(config);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("configuration");
    }

    [Fact]
    public void Constructor_WithNonExistentModelFile_ShouldThrowFileNotFoundException()
    {
        // Arrange
        var config = new LocalLlmConfiguration { ModelPath = NonExistentModelPath };

        // Act
        var act = () => new LocalLlmProvider(config);

        // Assert
        act.Should().Throw<FileNotFoundException>()
            .WithMessage($"*{NonExistentModelPath}*");
    }

    [Fact]
    public void ProviderName_ShouldContainBackendType()
    {
        // Arrange - Create a temporary file to pass validation
        var tempFile = Path.GetTempFileName();
        File.Move(tempFile, tempFile + ".gguf");
        tempFile += ".gguf";

        try
        {
            var config = new LocalLlmConfiguration
            {
                ModelPath = tempFile,
                Backend = LlmBackendType.Vulkan
            };
            using var provider = new LocalLlmProvider(config);

            // Act
            var name = provider.ProviderName;

            // Assert
            name.Should().Contain("LocalLlm");
            name.Should().Contain("LLamaSharp");
            name.Should().Contain("Vulkan");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void IsReady_BeforeInitialization_ShouldBeFalse()
    {
        // Arrange
        var tempFile = CreateTempModelFile();
        try
        {
            var config = new LocalLlmConfiguration { ModelPath = tempFile };
            using var provider = new LocalLlmProvider(config);

            // Act & Assert
            provider.IsReady.Should().BeFalse();
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void Dispose_ShouldNotThrow()
    {
        // Arrange
        var tempFile = CreateTempModelFile();
        try
        {
            var config = new LocalLlmConfiguration { ModelPath = tempFile };
            var provider = new LocalLlmProvider(config);

            // Act
            var act = () => provider.Dispose();

            // Assert
            act.Should().NotThrow();
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void Dispose_CalledMultipleTimes_ShouldNotThrow()
    {
        // Arrange
        var tempFile = CreateTempModelFile();
        try
        {
            var config = new LocalLlmConfiguration { ModelPath = tempFile };
            var provider = new LocalLlmProvider(config);

            // Act
            var act = () =>
            {
                provider.Dispose();
                provider.Dispose();
                provider.Dispose();
            };

            // Assert
            act.Should().NotThrow();
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task DisposeAsync_ShouldNotThrow()
    {
        // Arrange
        var tempFile = CreateTempModelFile();
        try
        {
            var config = new LocalLlmConfiguration { ModelPath = tempFile };
            var provider = new LocalLlmProvider(config);

            // Act
            var act = async () => await provider.DisposeAsync();

            // Assert
            await act.Should().NotThrowAsync();
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task CompleteAsync_AfterDispose_ShouldThrowObjectDisposedException()
    {
        // Arrange
        var tempFile = CreateTempModelFile();
        try
        {
            var config = new LocalLlmConfiguration { ModelPath = tempFile };
            var provider = new LocalLlmProvider(config);
            provider.Dispose();

            // Act
            var act = async () => await provider.CompleteAsync("test");

            // Assert
            await act.Should().ThrowAsync<ObjectDisposedException>();
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task InitializeAsync_AfterDispose_ShouldThrowObjectDisposedException()
    {
        // Arrange
        var tempFile = CreateTempModelFile();
        try
        {
            var config = new LocalLlmConfiguration { ModelPath = tempFile };
            var provider = new LocalLlmProvider(config);
            provider.Dispose();

            // Act
            var act = async () => await provider.InitializeAsync();

            // Assert
            await act.Should().ThrowAsync<ObjectDisposedException>();
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void IsReady_AfterDispose_ShouldBeFalse()
    {
        // Arrange
        var tempFile = CreateTempModelFile();
        try
        {
            var config = new LocalLlmConfiguration { ModelPath = tempFile };
            var provider = new LocalLlmProvider(config);
            provider.Dispose();

            // Act & Assert
            provider.IsReady.Should().BeFalse();
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task CompleteAsync_WithoutInitialization_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var tempFile = CreateTempModelFile();
        try
        {
            var config = new LocalLlmConfiguration { ModelPath = tempFile };
            using var provider = new LocalLlmProvider(config);

            // Act
            var act = async () => await provider.CompleteAsync("test");

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*not initialized*");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    private static string CreateTempModelFile()
    {
        var tempFile = Path.GetTempFileName();
        var ggufFile = tempFile + ".gguf";
        File.Move(tempFile, ggufFile);
        return ggufFile;
    }
}

