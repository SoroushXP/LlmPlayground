using FluentAssertions;
using LlmPlayground.Console;

namespace LlmPlayground.Console.Tests;

public class ConsoleThemeTests
{
    [Fact]
    public void DefaultValues_ShouldBeSetCorrectly()
    {
        // Arrange & Act
        var theme = new ConsoleTheme();

        // Assert
        theme.Enabled.Should().BeTrue();
        theme.ShowEmoji.Should().BeTrue();
        theme.TitleColor.Should().Be(ConsoleColor.Cyan);
        theme.SuccessColor.Should().Be(ConsoleColor.Green);
        theme.WarningColor.Should().Be(ConsoleColor.Yellow);
        theme.ErrorColor.Should().Be(ConsoleColor.Red);
        theme.InfoColor.Should().Be(ConsoleColor.Blue);
        theme.ResponseColor.Should().Be(ConsoleColor.Magenta);
        theme.PromptColor.Should().Be(ConsoleColor.Green);
        theme.MutedColor.Should().Be(ConsoleColor.DarkGray);
        theme.AccentColor.Should().Be(ConsoleColor.White);
        theme.LabelColor.Should().Be(ConsoleColor.DarkCyan);
        theme.ValueColor.Should().Be(ConsoleColor.White);
    }

    [Fact]
    public void MinimalTheme_ShouldHaveSubduedColors()
    {
        // Act
        var theme = ConsoleTheme.Minimal;

        // Assert
        theme.Enabled.Should().BeTrue();
        theme.ShowEmoji.Should().BeFalse();
        theme.TitleColor.Should().Be(ConsoleColor.White);
        theme.SuccessColor.Should().Be(ConsoleColor.Gray);
        theme.WarningColor.Should().Be(ConsoleColor.Gray);
        theme.ErrorColor.Should().Be(ConsoleColor.DarkRed);
        theme.InfoColor.Should().Be(ConsoleColor.Gray);
        theme.ResponseColor.Should().Be(ConsoleColor.White);
        theme.PromptColor.Should().Be(ConsoleColor.Gray);
    }

    [Fact]
    public void VibrantTheme_ShouldHaveColorfulSettings()
    {
        // Act
        var theme = ConsoleTheme.Vibrant;

        // Assert
        theme.Enabled.Should().BeTrue();
        theme.ShowEmoji.Should().BeTrue();
        theme.TitleColor.Should().Be(ConsoleColor.Cyan);
        theme.SuccessColor.Should().Be(ConsoleColor.Green);
        theme.WarningColor.Should().Be(ConsoleColor.Yellow);
        theme.ErrorColor.Should().Be(ConsoleColor.Red);
        theme.ResponseColor.Should().Be(ConsoleColor.Magenta);
    }

    [Fact]
    public void NoneTheme_ShouldBeDisabled()
    {
        // Act
        var theme = ConsoleTheme.None;

        // Assert
        theme.Enabled.Should().BeFalse();
        theme.ShowEmoji.Should().BeFalse();
    }

    [Fact]
    public void Colors_ShouldBeModifiable()
    {
        // Arrange
        var theme = new ConsoleTheme();

        // Act
        theme.TitleColor = ConsoleColor.DarkBlue;
        theme.SuccessColor = ConsoleColor.DarkGreen;
        theme.ErrorColor = ConsoleColor.DarkRed;

        // Assert
        theme.TitleColor.Should().Be(ConsoleColor.DarkBlue);
        theme.SuccessColor.Should().Be(ConsoleColor.DarkGreen);
        theme.ErrorColor.Should().Be(ConsoleColor.DarkRed);
    }

    [Fact]
    public void Enabled_ShouldBeModifiable()
    {
        // Arrange
        var theme = new ConsoleTheme();

        // Act
        theme.Enabled = false;

        // Assert
        theme.Enabled.Should().BeFalse();
    }

    [Fact]
    public void ShowEmoji_ShouldBeModifiable()
    {
        // Arrange
        var theme = new ConsoleTheme();

        // Act
        theme.ShowEmoji = false;

        // Assert
        theme.ShowEmoji.Should().BeFalse();
    }

    [Theory]
    [InlineData(ConsoleColor.Black)]
    [InlineData(ConsoleColor.DarkBlue)]
    [InlineData(ConsoleColor.DarkGreen)]
    [InlineData(ConsoleColor.DarkCyan)]
    [InlineData(ConsoleColor.DarkRed)]
    [InlineData(ConsoleColor.DarkMagenta)]
    [InlineData(ConsoleColor.DarkYellow)]
    [InlineData(ConsoleColor.Gray)]
    [InlineData(ConsoleColor.DarkGray)]
    [InlineData(ConsoleColor.Blue)]
    [InlineData(ConsoleColor.Green)]
    [InlineData(ConsoleColor.Cyan)]
    [InlineData(ConsoleColor.Red)]
    [InlineData(ConsoleColor.Magenta)]
    [InlineData(ConsoleColor.Yellow)]
    [InlineData(ConsoleColor.White)]
    public void AllColors_ShouldBeValidForThemeProperties(ConsoleColor color)
    {
        // Arrange
        var theme = new ConsoleTheme();

        // Act & Assert - all color properties should accept any ConsoleColor
        theme.TitleColor = color;
        theme.TitleColor.Should().Be(color);

        theme.SuccessColor = color;
        theme.SuccessColor.Should().Be(color);

        theme.ErrorColor = color;
        theme.ErrorColor.Should().Be(color);
    }
}

