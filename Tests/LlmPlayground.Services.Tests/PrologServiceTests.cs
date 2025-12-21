using FluentAssertions;
using LlmPlayground.Prolog;
using LlmPlayground.Services.Models;
using LlmPlayground.Services.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace LlmPlayground.Services.Tests;

public class PrologServiceTests
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<PrologService> _logger;
    private readonly PrologService _sut;

    public PrologServiceTests()
    {
        var configData = new Dictionary<string, string?>
        {
            ["Prolog:ExecutablePath"] = null,
            ["Prolog:WorkingDirectory"] = null
        };

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        _logger = Substitute.For<ILogger<PrologService>>();
        _sut = new PrologService(_configuration, _logger);
    }

    [Fact]
    public void Constructor_WithNullConfiguration_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new PrologService(null!, _logger);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new PrologService(_configuration, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public async Task ExecuteQueryAsync_WithNullRequest_ThrowsArgumentNullException()
    {
        // Act
        var act = () => _sut.ExecuteQueryAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task ExecuteQueryAsync_WithEmptyQuery_ThrowsArgumentException()
    {
        // Arrange
        var request = new PrologQueryRequest { Query = "" };

        // Act
        var act = () => _sut.ExecuteQueryAsync(request);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task ExecuteQueryAsync_WithWhitespaceQuery_ThrowsArgumentException()
    {
        // Arrange
        var request = new PrologQueryRequest { Query = "   " };

        // Act
        var act = () => _sut.ExecuteQueryAsync(request);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task ExecuteFileAsync_WithNullRequest_ThrowsArgumentNullException()
    {
        // Act
        var act = () => _sut.ExecuteFileAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task ExecuteFileAsync_WithEmptyFilePath_ThrowsArgumentException()
    {
        // Arrange
        var request = new PrologFileRequest { FilePath = "" };

        // Act
        var act = () => _sut.ExecuteFileAsync(request);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task ExecuteFileAsync_WithNonExistentFile_ReturnsFailure()
    {
        // Arrange
        var request = new PrologFileRequest { FilePath = "nonexistent.pl" };

        // Act
        var result = await _sut.ExecuteFileAsync(request);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ValidateSyntaxAsync_WithEmptyCode_ThrowsArgumentException()
    {
        // Act
        var act = () => _sut.ValidateSyntaxAsync("");

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task ValidateSyntaxAsync_WithWhitespaceCode_ThrowsArgumentException()
    {
        // Act
        var act = () => _sut.ValidateSyntaxAsync("   ");

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task CheckAvailabilityAsync_ReturnsResponse()
    {
        // Act
        var result = await _sut.CheckAvailabilityAsync();

        // Assert
        result.Should().NotBeNull();
        result.Info.Should().NotBeNullOrEmpty();
    }
}

