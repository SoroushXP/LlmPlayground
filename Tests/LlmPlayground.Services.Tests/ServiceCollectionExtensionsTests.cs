using FluentAssertions;
using LlmPlayground.Services.Extensions;
using LlmPlayground.Services.Interfaces;
using LlmPlayground.Services.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LlmPlayground.Services.Tests;

public class ServiceCollectionExtensionsTests
{
    private readonly IServiceCollection _services;
    private readonly IConfiguration _configuration;

    public ServiceCollectionExtensionsTests()
    {
        _services = new ServiceCollection();

        var configData = new Dictionary<string, string?>
        {
            ["Ollama:Host"] = "localhost",
            ["Ollama:Port"] = "11434",
            ["Prolog:ExecutablePath"] = null
        };

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        _services.AddSingleton(_configuration);
        _services.AddLogging();
    }

    [Fact]
    public void AddLlmPlaygroundServices_RegistersAllServices()
    {
        // Act
        _services.AddLlmPlaygroundServices();
        var provider = _services.BuildServiceProvider();

        // Assert
        provider.GetService<ILlmService>().Should().NotBeNull();
        provider.GetService<IPrologService>().Should().NotBeNull();
        provider.GetService<IPromptLabService>().Should().NotBeNull();
    }

    [Fact]
    public void AddLlmService_RegistersLlmService()
    {
        // Act
        _services.AddLlmService();
        var provider = _services.BuildServiceProvider();

        // Assert
        var service = provider.GetService<ILlmService>();
        service.Should().NotBeNull();
        service.Should().BeOfType<LlmService>();
    }

    [Fact]
    public void AddPrologService_RegistersPrologService()
    {
        // Act
        _services.AddPrologService();
        var provider = _services.BuildServiceProvider();

        // Assert
        var service = provider.GetService<IPrologService>();
        service.Should().NotBeNull();
        service.Should().BeOfType<PrologService>();
    }

    [Fact]
    public void AddPromptLabService_RegistersPromptLabService()
    {
        // Act
        _services.AddPromptLabService();
        var provider = _services.BuildServiceProvider();

        // Assert
        var service = provider.GetService<IPromptLabService>();
        service.Should().NotBeNull();
        service.Should().BeOfType<PromptLabService>();
    }

    [Fact]
    public void AddLlmPlaygroundServices_ReturnsSameServiceCollection()
    {
        // Act
        var result = _services.AddLlmPlaygroundServices();

        // Assert
        result.Should().BeSameAs(_services);
    }

    [Fact]
    public void AddLlmService_ReturnsSameServiceCollection()
    {
        // Act
        var result = _services.AddLlmService();

        // Assert
        result.Should().BeSameAs(_services);
    }

    [Fact]
    public void AddPrologService_ReturnsSameServiceCollection()
    {
        // Act
        var result = _services.AddPrologService();

        // Assert
        result.Should().BeSameAs(_services);
    }

    [Fact]
    public void AddPromptLabService_ReturnsSameServiceCollection()
    {
        // Act
        var result = _services.AddPromptLabService();

        // Assert
        result.Should().BeSameAs(_services);
    }

    [Fact]
    public void Services_AreRegisteredAsSingletons()
    {
        // Arrange
        _services.AddLlmPlaygroundServices();
        var provider = _services.BuildServiceProvider();

        // Act
        var llmService1 = provider.GetService<ILlmService>();
        var llmService2 = provider.GetService<ILlmService>();
        var prologService1 = provider.GetService<IPrologService>();
        var prologService2 = provider.GetService<IPrologService>();
        var promptLabService1 = provider.GetService<IPromptLabService>();
        var promptLabService2 = provider.GetService<IPromptLabService>();

        // Assert
        llmService1.Should().BeSameAs(llmService2);
        prologService1.Should().BeSameAs(prologService2);
        promptLabService1.Should().BeSameAs(promptLabService2);
    }
}

