using FluentAssertions;
using LlmPlayground.Core;

namespace LlmPlayground.Core.Tests;

public class ChatMessageTests
{
    [Fact]
    public void ChatMessage_ShouldCreateWithUserRole()
    {
        // Arrange & Act
        var message = new ChatMessage(ChatRole.User, "Hello, world!");

        // Assert
        message.Role.Should().Be(ChatRole.User);
        message.Content.Should().Be("Hello, world!");
    }

    [Fact]
    public void ChatMessage_ShouldCreateWithAssistantRole()
    {
        // Arrange & Act
        var message = new ChatMessage(ChatRole.Assistant, "I am an AI assistant.");

        // Assert
        message.Role.Should().Be(ChatRole.Assistant);
        message.Content.Should().Be("I am an AI assistant.");
    }

    [Fact]
    public void ChatMessage_ShouldCreateWithSystemRole()
    {
        // Arrange & Act
        var message = new ChatMessage(ChatRole.System, "You are a helpful assistant.");

        // Assert
        message.Role.Should().Be(ChatRole.System);
        message.Content.Should().Be("You are a helpful assistant.");
    }

    [Fact]
    public void ChatMessage_Record_ShouldHaveValueEquality()
    {
        // Arrange
        var message1 = new ChatMessage(ChatRole.User, "Hello");
        var message2 = new ChatMessage(ChatRole.User, "Hello");

        // Assert
        message1.Should().Be(message2);
        message1.GetHashCode().Should().Be(message2.GetHashCode());
    }

    [Fact]
    public void ChatMessage_Record_ShouldSupportWithExpression()
    {
        // Arrange
        var original = new ChatMessage(ChatRole.User, "Original content");

        // Act
        var modified = original with { Content = "Modified content" };

        // Assert
        modified.Role.Should().Be(ChatRole.User);
        modified.Content.Should().Be("Modified content");
        original.Content.Should().Be("Original content"); // Original unchanged
    }

    [Fact]
    public void ChatMessage_ShouldHandleEmptyContent()
    {
        // Arrange & Act
        var message = new ChatMessage(ChatRole.User, string.Empty);

        // Assert
        message.Content.Should().BeEmpty();
    }

    [Fact]
    public void ChatMessage_ShouldHandleMultilineContent()
    {
        // Arrange
        var multilineContent = "Line 1\nLine 2\nLine 3";

        // Act
        var message = new ChatMessage(ChatRole.Assistant, multilineContent);

        // Assert
        message.Content.Should().Contain("\n");
        message.Content.Should().Be(multilineContent);
    }
}

public class ChatRoleTests
{
    [Fact]
    public void ChatRole_ShouldHaveSystemValue()
    {
        // Assert
        ChatRole.System.Should().Be(ChatRole.System);
        ((int)ChatRole.System).Should().Be(0);
    }

    [Fact]
    public void ChatRole_ShouldHaveUserValue()
    {
        // Assert
        ChatRole.User.Should().Be(ChatRole.User);
        ((int)ChatRole.User).Should().Be(1);
    }

    [Fact]
    public void ChatRole_ShouldHaveAssistantValue()
    {
        // Assert
        ChatRole.Assistant.Should().Be(ChatRole.Assistant);
        ((int)ChatRole.Assistant).Should().Be(2);
    }

    [Fact]
    public void ChatRole_ShouldBeConvertibleToString()
    {
        // Assert
        ChatRole.System.ToString().Should().Be("System");
        ChatRole.User.ToString().Should().Be("User");
        ChatRole.Assistant.ToString().Should().Be("Assistant");
    }
}

