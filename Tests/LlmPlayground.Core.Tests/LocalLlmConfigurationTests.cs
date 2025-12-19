using FluentAssertions;
using LlmPlayground.Core;

namespace LlmPlayground.Core.Tests;

public class LocalLlmConfigurationTests
{
    [Fact]
    public void DefaultValues_ShouldBeSetCorrectly()
    {
        // Arrange & Act
        var config = new LocalLlmConfiguration
        {
            ModelPath = "/path/to/model.gguf"
        };

        // Assert
        config.Backend.Should().Be(LlmBackendType.Cpu);
        config.GpuDeviceIndex.Should().Be(0);
        config.GpuLayerCount.Should().Be(0);
        config.ContextSize.Should().Be(2048);
        config.ThreadCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public void ModelPath_ShouldBeRequired()
    {
        // Arrange & Act
        var config = new LocalLlmConfiguration
        {
            ModelPath = "/my/model.gguf"
        };

        // Assert
        config.ModelPath.Should().Be("/my/model.gguf");
    }

    [Fact]
    public void Backend_ShouldAcceptAllValidValues()
    {
        // Arrange & Act & Assert
        var cpuConfig = new LocalLlmConfiguration { ModelPath = "m.gguf", Backend = LlmBackendType.Cpu };
        cpuConfig.Backend.Should().Be(LlmBackendType.Cpu);

        var vulkanConfig = new LocalLlmConfiguration { ModelPath = "m.gguf", Backend = LlmBackendType.Vulkan };
        vulkanConfig.Backend.Should().Be(LlmBackendType.Vulkan);

        var cudaConfig = new LocalLlmConfiguration { ModelPath = "m.gguf", Backend = LlmBackendType.Cuda };
        cudaConfig.Backend.Should().Be(LlmBackendType.Cuda);
    }

    [Fact]
    public void GpuLayerCount_ShouldAcceptNegativeOneForAllLayers()
    {
        // Arrange & Act
        var config = new LocalLlmConfiguration
        {
            ModelPath = "model.gguf",
            GpuLayerCount = -1
        };

        // Assert
        config.GpuLayerCount.Should().Be(-1);
    }

    [Fact]
    public void Record_ShouldSupportWithExpression()
    {
        // Arrange
        var original = new LocalLlmConfiguration
        {
            ModelPath = "model.gguf",
            Backend = LlmBackendType.Cpu
        };

        // Act
        var modified = original with { Backend = LlmBackendType.Vulkan };

        // Assert
        modified.Backend.Should().Be(LlmBackendType.Vulkan);
        modified.ModelPath.Should().Be("model.gguf");
        original.Backend.Should().Be(LlmBackendType.Cpu); // Original unchanged
    }

    [Fact]
    public void Record_ShouldHaveValueEquality()
    {
        // Arrange
        var config1 = new LocalLlmConfiguration
        {
            ModelPath = "model.gguf",
            Backend = LlmBackendType.Vulkan,
            GpuLayerCount = 32
        };
        var config2 = new LocalLlmConfiguration
        {
            ModelPath = "model.gguf",
            Backend = LlmBackendType.Vulkan,
            GpuLayerCount = 32
        };

        // Assert
        config1.Should().Be(config2);
    }

    [Theory]
    [InlineData(LlmBackendType.Cpu)]
    [InlineData(LlmBackendType.Vulkan)]
    [InlineData(LlmBackendType.Cuda)]
    public void LlmBackendType_ShouldHaveExpectedValues(LlmBackendType backend)
    {
        // Assert
        Enum.IsDefined(typeof(LlmBackendType), backend).Should().BeTrue();
    }
}

