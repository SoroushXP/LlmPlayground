using FluentAssertions;
using LlmPlayground.Core;

namespace LlmPlayground.Core.Tests;

public class ConversationHistoryTests
{
    [Fact]
    public void ConversationHistory_ShouldMaintainMessageOrder()
    {
        // Arrange
        var history = new List<ChatMessage>();

        // Act
        history.Add(new ChatMessage(ChatRole.System, "You are helpful."));
        history.Add(new ChatMessage(ChatRole.User, "Hello"));
        history.Add(new ChatMessage(ChatRole.Assistant, "Hi there!"));
        history.Add(new ChatMessage(ChatRole.User, "How are you?"));
        history.Add(new ChatMessage(ChatRole.Assistant, "I'm doing well!"));

        // Assert
        history.Should().HaveCount(5);
        history[0].Role.Should().Be(ChatRole.System);
        history[1].Role.Should().Be(ChatRole.User);
        history[2].Role.Should().Be(ChatRole.Assistant);
        history[3].Role.Should().Be(ChatRole.User);
        history[4].Role.Should().Be(ChatRole.Assistant);
    }

    [Fact]
    public void ConversationHistory_ShouldBeCleared()
    {
        // Arrange
        var history = new List<ChatMessage>
        {
            new(ChatRole.User, "Hello"),
            new(ChatRole.Assistant, "Hi!")
        };

        // Act
        history.Clear();

        // Assert
        history.Should().BeEmpty();
    }

    [Fact]
    public void ConversationHistory_ShouldSupportRemovingLastMessage()
    {
        // Arrange
        var history = new List<ChatMessage>
        {
            new(ChatRole.User, "Hello"),
            new(ChatRole.Assistant, "Hi!"),
            new(ChatRole.User, "Failed message")
        };

        // Act - Simulate removing failed message
        if (history.Count > 0 && history[^1].Role == ChatRole.User)
        {
            history.RemoveAt(history.Count - 1);
        }

        // Assert
        history.Should().HaveCount(2);
        history[^1].Role.Should().Be(ChatRole.Assistant);
    }

    [Fact]
    public void ConversationHistory_AsReadOnlyList_ShouldBePassable()
    {
        // Arrange
        var history = new List<ChatMessage>
        {
            new(ChatRole.System, "System prompt"),
            new(ChatRole.User, "User message")
        };

        // Act
        IReadOnlyList<ChatMessage> readOnlyHistory = history;

        // Assert
        readOnlyHistory.Should().HaveCount(2);
        readOnlyHistory[0].Role.Should().Be(ChatRole.System);
    }

    [Fact]
    public void ConversationHistory_ShouldTrackConversationContext()
    {
        // Arrange
        var history = new List<ChatMessage>();

        // Act - Simulate a multi-turn conversation
        history.Add(new ChatMessage(ChatRole.User, "My name is Alice."));
        history.Add(new ChatMessage(ChatRole.Assistant, "Nice to meet you, Alice!"));
        history.Add(new ChatMessage(ChatRole.User, "What is my name?"));
        // The assistant would use context from history to know the name is Alice

        // Assert - History contains all context needed
        history.Should().HaveCount(3);
        history.Should().Contain(m => m.Content.Contains("Alice"));
    }

    [Fact]
    public void ConversationHistory_ShouldHandleLongConversations()
    {
        // Arrange
        var history = new List<ChatMessage>();

        // Act - Add many messages
        for (int i = 0; i < 50; i++)
        {
            history.Add(new ChatMessage(ChatRole.User, $"User message {i}"));
            history.Add(new ChatMessage(ChatRole.Assistant, $"Assistant response {i}"));
        }

        // Assert
        history.Should().HaveCount(100);
        history.First().Role.Should().Be(ChatRole.User);
        history.Last().Role.Should().Be(ChatRole.Assistant);
    }

    [Fact]
    public void ConversationHistory_ShouldFilterByRole()
    {
        // Arrange
        var history = new List<ChatMessage>
        {
            new(ChatRole.System, "System"),
            new(ChatRole.User, "User 1"),
            new(ChatRole.Assistant, "Assistant 1"),
            new(ChatRole.User, "User 2"),
            new(ChatRole.Assistant, "Assistant 2")
        };

        // Act
        var userMessages = history.Where(m => m.Role == ChatRole.User).ToList();
        var assistantMessages = history.Where(m => m.Role == ChatRole.Assistant).ToList();
        var systemMessages = history.Where(m => m.Role == ChatRole.System).ToList();

        // Assert
        userMessages.Should().HaveCount(2);
        assistantMessages.Should().HaveCount(2);
        systemMessages.Should().HaveCount(1);
    }
}

