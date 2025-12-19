using FluentAssertions;
using LlmPlayground.Console;
using Microsoft.Extensions.Configuration;

namespace LlmPlayground.Console.Tests;

public class ConsoleStylesTests
{
    [Fact]
    public void Theme_ShouldDefaultToVibrant()
    {
        // Reset to default
        ConsoleStyles.Theme = ConsoleTheme.Vibrant;

        // Assert
        ConsoleStyles.Theme.Should().NotBeNull();
        ConsoleStyles.Theme.Enabled.Should().BeTrue();
        ConsoleStyles.Theme.ShowEmoji.Should().BeTrue();
    }

    [Fact]
    public void Theme_ShouldBeSettable()
    {
        // Arrange
        var customTheme = ConsoleTheme.Minimal;

        // Act
        ConsoleStyles.Theme = customTheme;

        // Assert
        ConsoleStyles.Theme.Should().Be(customTheme);
        ConsoleStyles.Theme.ShowEmoji.Should().BeFalse();

        // Cleanup
        ConsoleStyles.Theme = ConsoleTheme.Vibrant;
    }

    [Fact]
    public void Theme_ShouldDefaultToVibrantWhenSetToNull()
    {
        // Act
        ConsoleStyles.Theme = null!;

        // Assert
        ConsoleStyles.Theme.Should().NotBeNull();
        ConsoleStyles.Theme.Enabled.Should().BeTrue();
    }

    [Fact]
    public void Initialize_ShouldUseVibrantWhenNoConfigSection()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        // Act
        ConsoleStyles.Initialize(config);

        // Assert
        ConsoleStyles.Theme.Enabled.Should().BeTrue();
        ConsoleStyles.Theme.ShowEmoji.Should().BeTrue();
    }

    [Fact]
    public void Initialize_ShouldLoadMinimalTheme()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Console:Theme"] = "Minimal"
            })
            .Build();

        // Act
        ConsoleStyles.Initialize(config);

        // Assert
        ConsoleStyles.Theme.ShowEmoji.Should().BeFalse();

        // Cleanup
        ConsoleStyles.Theme = ConsoleTheme.Vibrant;
    }

    [Fact]
    public void Initialize_ShouldLoadNoneTheme()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Console:Theme"] = "None"
            })
            .Build();

        // Act
        ConsoleStyles.Initialize(config);

        // Assert
        ConsoleStyles.Theme.Enabled.Should().BeFalse();

        // Cleanup
        ConsoleStyles.Theme = ConsoleTheme.Vibrant;
    }

    [Fact]
    public void Initialize_ShouldOverrideEnabledSetting()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Console:Theme"] = "Vibrant",
                ["Console:Enabled"] = "false"
            })
            .Build();

        // Act
        ConsoleStyles.Initialize(config);

        // Assert
        ConsoleStyles.Theme.Enabled.Should().BeFalse();

        // Cleanup
        ConsoleStyles.Theme = ConsoleTheme.Vibrant;
    }

    [Fact]
    public void Initialize_ShouldOverrideShowEmojiSetting()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Console:Theme"] = "Vibrant",
                ["Console:ShowEmoji"] = "false"
            })
            .Build();

        // Act
        ConsoleStyles.Initialize(config);

        // Assert
        ConsoleStyles.Theme.ShowEmoji.Should().BeFalse();

        // Cleanup
        ConsoleStyles.Theme = ConsoleTheme.Vibrant;
    }

    [Fact]
    public void Initialize_ShouldParseCustomColors()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Console:Theme"] = "Vibrant",
                ["Console:TitleColor"] = "DarkBlue",
                ["Console:ErrorColor"] = "DarkRed"
            })
            .Build();

        // Act
        ConsoleStyles.Initialize(config);

        // Assert
        ConsoleStyles.Theme.TitleColor.Should().Be(ConsoleColor.DarkBlue);
        ConsoleStyles.Theme.ErrorColor.Should().Be(ConsoleColor.DarkRed);

        // Cleanup
        ConsoleStyles.Theme = ConsoleTheme.Vibrant;
    }

    [Fact]
    public void Initialize_ShouldIgnoreInvalidColors()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Console:Theme"] = "Vibrant",
                ["Console:TitleColor"] = "InvalidColor"
            })
            .Build();

        // Act
        ConsoleStyles.Initialize(config);

        // Assert - should keep default Vibrant color
        ConsoleStyles.Theme.TitleColor.Should().Be(ConsoleColor.Cyan);

        // Cleanup
        ConsoleStyles.Theme = ConsoleTheme.Vibrant;
    }

    [Fact]
    public void Initialize_ShouldHandleDisabledTheme()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Console:Theme"] = "Disabled"
            })
            .Build();

        // Act
        ConsoleStyles.Initialize(config);

        // Assert
        ConsoleStyles.Theme.Enabled.Should().BeFalse();

        // Cleanup
        ConsoleStyles.Theme = ConsoleTheme.Vibrant;
    }

    [Fact]
    public void Emoji_ShouldHaveExpectedValues()
    {
        // Assert - verify key emoji constants exist and are not empty
        ConsoleStyles.Emoji.Sparkles.Should().NotBeNullOrEmpty();
        ConsoleStyles.Emoji.Robot.Should().NotBeNullOrEmpty();
        ConsoleStyles.Emoji.Lightning.Should().NotBeNullOrEmpty();
        ConsoleStyles.Emoji.Check.Should().NotBeNullOrEmpty();
        ConsoleStyles.Emoji.Cross.Should().NotBeNullOrEmpty();
        ConsoleStyles.Emoji.Arrow.Should().NotBeNullOrEmpty();
        ConsoleStyles.Emoji.Dot.Should().NotBeNullOrEmpty();
        ConsoleStyles.Emoji.Star.Should().NotBeNullOrEmpty();
        ConsoleStyles.Emoji.Gear.Should().NotBeNullOrEmpty();
        ConsoleStyles.Emoji.Chat.Should().NotBeNullOrEmpty();
        ConsoleStyles.Emoji.Brain.Should().NotBeNullOrEmpty();
        ConsoleStyles.Emoji.Rocket.Should().NotBeNullOrEmpty();
        ConsoleStyles.Emoji.Warning.Should().NotBeNullOrEmpty();
        ConsoleStyles.Emoji.Info.Should().NotBeNullOrEmpty();
        ConsoleStyles.Emoji.Question.Should().NotBeNullOrEmpty();
        ConsoleStyles.Emoji.Hourglass.Should().NotBeNullOrEmpty();
        ConsoleStyles.Emoji.Success.Should().NotBeNullOrEmpty();
        ConsoleStyles.Emoji.Error.Should().NotBeNullOrEmpty();
        ConsoleStyles.Emoji.Wave.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Emoji_CheckShouldBeAsciiCompatible()
    {
        ConsoleStyles.Emoji.Check.Should().Be("[OK]");
    }

    [Fact]
    public void Emoji_CrossShouldBeAsciiCompatible()
    {
        ConsoleStyles.Emoji.Cross.Should().Be("[X]");
    }

    [Fact]
    public void Emoji_ArrowShouldBeAsciiCompatible()
    {
        ConsoleStyles.Emoji.Arrow.Should().Be(">");
    }

    [Fact]
    public void Initialize_ShouldBeCaseInsensitiveForThemeName()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Console:Theme"] = "MINIMAL"
            })
            .Build();

        // Act
        ConsoleStyles.Initialize(config);

        // Assert
        ConsoleStyles.Theme.ShowEmoji.Should().BeFalse();

        // Cleanup
        ConsoleStyles.Theme = ConsoleTheme.Vibrant;
    }

    [Fact]
    public void Initialize_ShouldBeCaseInsensitiveForColors()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Console:TitleColor"] = "darkblue"
            })
            .Build();

        // Act
        ConsoleStyles.Initialize(config);

        // Assert
        ConsoleStyles.Theme.TitleColor.Should().Be(ConsoleColor.DarkBlue);

        // Cleanup
        ConsoleStyles.Theme = ConsoleTheme.Vibrant;
    }
}

