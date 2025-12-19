using FluentAssertions;
using LlmPlayground.Core;
using System.Reflection;

namespace LlmPlayground.Core.Tests;

/// <summary>
/// Tests to verify that all provider methods properly support cancellation tokens.
/// </summary>
public class ProviderCancellationTests
{
    [Fact]
    public void ILlmProvider_InitializeAsync_ShouldAcceptCancellationToken()
    {
        // Arrange
        var method = typeof(ILlmProvider).GetMethod(nameof(ILlmProvider.InitializeAsync));

        // Assert
        method.Should().NotBeNull();
        var parameters = method!.GetParameters();
        parameters.Should().ContainSingle(p => p.ParameterType == typeof(CancellationToken));
    }

    [Fact]
    public void ILlmProvider_CompleteAsync_ShouldAcceptCancellationToken()
    {
        // Arrange
        var method = typeof(ILlmProvider).GetMethod(nameof(ILlmProvider.CompleteAsync));

        // Assert
        method.Should().NotBeNull();
        var parameters = method!.GetParameters();
        parameters.Should().Contain(p => p.ParameterType == typeof(CancellationToken));
    }

    [Fact]
    public void ILlmProvider_CompleteStreamingAsync_ShouldAcceptCancellationToken()
    {
        // Arrange
        var method = typeof(ILlmProvider).GetMethod(nameof(ILlmProvider.CompleteStreamingAsync));

        // Assert
        method.Should().NotBeNull();
        var parameters = method!.GetParameters();
        parameters.Should().Contain(p => p.ParameterType == typeof(CancellationToken));
    }

    [Fact]
    public void ILlmProvider_ChatAsync_ShouldAcceptCancellationToken()
    {
        // Arrange
        var method = typeof(ILlmProvider).GetMethod(nameof(ILlmProvider.ChatAsync));

        // Assert
        method.Should().NotBeNull();
        var parameters = method!.GetParameters();
        parameters.Should().Contain(p => p.ParameterType == typeof(CancellationToken));
    }

    [Fact]
    public void ILlmProvider_ChatStreamingAsync_ShouldAcceptCancellationToken()
    {
        // Arrange
        var method = typeof(ILlmProvider).GetMethod(nameof(ILlmProvider.ChatStreamingAsync));

        // Assert
        method.Should().NotBeNull();
        var parameters = method!.GetParameters();
        parameters.Should().Contain(p => p.ParameterType == typeof(CancellationToken));
    }

    [Fact]
    public void IModelListingProvider_GetAvailableModelsAsync_ShouldAcceptCancellationToken()
    {
        // Arrange
        var method = typeof(IModelListingProvider).GetMethod(nameof(IModelListingProvider.GetAvailableModelsAsync));

        // Assert
        method.Should().NotBeNull();
        var parameters = method!.GetParameters();
        parameters.Should().ContainSingle(p => p.ParameterType == typeof(CancellationToken));
    }

    [Theory]
    [InlineData(typeof(OpenAiProvider))]
    [InlineData(typeof(OllamaProvider))]
    [InlineData(typeof(LmStudioProvider))]
    [InlineData(typeof(LocalLlmProvider))]
    public void Provider_ShouldImplementILlmProvider(Type providerType)
    {
        // Assert
        providerType.Should().Implement<ILlmProvider>();
    }

    [Theory]
    [InlineData(typeof(OpenAiProvider), nameof(ILlmProvider.CompleteAsync))]
    [InlineData(typeof(OpenAiProvider), nameof(ILlmProvider.CompleteStreamingAsync))]
    [InlineData(typeof(OpenAiProvider), nameof(ILlmProvider.ChatAsync))]
    [InlineData(typeof(OpenAiProvider), nameof(ILlmProvider.ChatStreamingAsync))]
    [InlineData(typeof(OllamaProvider), nameof(ILlmProvider.CompleteAsync))]
    [InlineData(typeof(OllamaProvider), nameof(ILlmProvider.CompleteStreamingAsync))]
    [InlineData(typeof(OllamaProvider), nameof(ILlmProvider.ChatAsync))]
    [InlineData(typeof(OllamaProvider), nameof(ILlmProvider.ChatStreamingAsync))]
    [InlineData(typeof(LmStudioProvider), nameof(ILlmProvider.CompleteAsync))]
    [InlineData(typeof(LmStudioProvider), nameof(ILlmProvider.CompleteStreamingAsync))]
    [InlineData(typeof(LmStudioProvider), nameof(ILlmProvider.ChatAsync))]
    [InlineData(typeof(LmStudioProvider), nameof(ILlmProvider.ChatStreamingAsync))]
    [InlineData(typeof(LocalLlmProvider), nameof(ILlmProvider.CompleteAsync))]
    [InlineData(typeof(LocalLlmProvider), nameof(ILlmProvider.CompleteStreamingAsync))]
    [InlineData(typeof(LocalLlmProvider), nameof(ILlmProvider.ChatAsync))]
    [InlineData(typeof(LocalLlmProvider), nameof(ILlmProvider.ChatStreamingAsync))]
    public void Provider_Method_ShouldHaveCancellationTokenParameter(Type providerType, string methodName)
    {
        // Arrange
        var methods = providerType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(m => m.Name == methodName);

        // Assert
        methods.Should().NotBeEmpty($"{providerType.Name} should have method {methodName}");
        foreach (var method in methods)
        {
            var parameters = method.GetParameters();
            parameters.Should().Contain(
                p => p.ParameterType == typeof(CancellationToken),
                $"{providerType.Name}.{methodName} should accept CancellationToken");
        }
    }

    [Fact]
    public void AllProviderMethods_CancellationToken_ShouldHaveDefaultValue()
    {
        // Arrange
        var interfaceType = typeof(ILlmProvider);
        var asyncMethods = interfaceType.GetMethods()
            .Where(m => m.Name.EndsWith("Async"));

        // Assert
        foreach (var method in asyncMethods)
        {
            var ctParam = method.GetParameters()
                .FirstOrDefault(p => p.ParameterType == typeof(CancellationToken));

            ctParam.Should().NotBeNull($"{method.Name} should have CancellationToken parameter");
            ctParam!.HasDefaultValue.Should().BeTrue(
                $"{method.Name}'s CancellationToken parameter should have default value");
        }
    }
}

