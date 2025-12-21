using FluentAssertions;
using LlmPlayground.Core;
using LlmPlayground.Services.Models;

namespace LlmPlayground.Services.Tests;

public class ModelsTests
{
    public class ChatRequestTests
    {
        [Fact]
        public void ChatRequest_CanBeCreated_WithRequiredProperties()
        {
            // Arrange & Act
            var request = new ChatRequest
            {
                Messages = [new ChatMessageDto { Role = "user", Content = "Hello" }]
            };

            // Assert
            request.Messages.Should().HaveCount(1);
            request.Stream.Should().BeFalse();
            request.Options.Should().BeNull();
        }

        [Fact]
        public void ChatRequest_WithAllProperties_SetsCorrectly()
        {
            // Arrange & Act
            var request = new ChatRequest
            {
                Messages = [new ChatMessageDto { Role = "user", Content = "Hello" }],
                Stream = true,
                Options = new InferenceOptionsDto { MaxTokens = 100 }
            };

            // Assert
            request.Stream.Should().BeTrue();
            request.Options!.MaxTokens.Should().Be(100);
        }
    }

    public class CompletionRequestTests
    {
        [Fact]
        public void CompletionRequest_CanBeCreated_WithRequiredProperties()
        {
            // Arrange & Act
            var request = new CompletionRequest
            {
                Prompt = "Test prompt"
            };

            // Assert
            request.Prompt.Should().Be("Test prompt");
            request.Stream.Should().BeFalse();
            request.Options.Should().BeNull();
        }
    }

    public class CompletionResponseTests
    {
        [Fact]
        public void TokensPerSecond_WithZeroDuration_ReturnsZero()
        {
            // Arrange
            var response = new CompletionResponse
            {
                Text = "Test",
                TokensGenerated = 100,
                Duration = TimeSpan.Zero
            };

            // Act & Assert
            response.TokensPerSecond.Should().Be(0);
        }

        [Fact]
        public void TokensPerSecond_WithValidDuration_CalculatesCorrectly()
        {
            // Arrange
            var response = new CompletionResponse
            {
                Text = "Test",
                TokensGenerated = 100,
                Duration = TimeSpan.FromSeconds(2)
            };

            // Act & Assert
            response.TokensPerSecond.Should().Be(50);
        }
    }

    public class InferenceOptionsDtoTests
    {
        [Fact]
        public void InferenceOptionsDto_DefaultValues_AreNull()
        {
            // Arrange & Act
            var options = new InferenceOptionsDto();

            // Assert
            options.MaxTokens.Should().BeNull();
            options.Temperature.Should().BeNull();
            options.TopP.Should().BeNull();
            options.RepeatPenalty.Should().BeNull();
        }

        [Fact]
        public void InferenceOptionsDto_CanSetAllProperties()
        {
            // Arrange & Act
            var options = new InferenceOptionsDto
            {
                MaxTokens = 500,
                Temperature = 0.8f,
                TopP = 0.95f,
                RepeatPenalty = 1.2f
            };

            // Assert
            options.MaxTokens.Should().Be(500);
            options.Temperature.Should().Be(0.8f);
            options.TopP.Should().Be(0.95f);
            options.RepeatPenalty.Should().Be(1.2f);
        }
    }

    public class PrologModelsTests
    {
        [Fact]
        public void PrologQueryRequest_CanBeCreated()
        {
            // Arrange & Act
            var request = new PrologQueryRequest { Query = "member(X, [1,2,3])" };

            // Assert
            request.Query.Should().Be("member(X, [1,2,3])");
        }

        [Fact]
        public void PrologFileRequest_CanBeCreated_WithOptionalGoal()
        {
            // Arrange & Act
            var request = new PrologFileRequest
            {
                FilePath = "test.pl",
                Goal = "main"
            };

            // Assert
            request.FilePath.Should().Be("test.pl");
            request.Goal.Should().Be("main");
        }

        [Fact]
        public void PrologResponse_DefaultValues_AreCorrect()
        {
            // Arrange & Act
            var response = new PrologResponse();

            // Assert
            response.Success.Should().BeFalse();
            response.Output.Should().BeEmpty();
            response.Error.Should().BeNull();
            response.Warnings.Should().BeNull();
            response.ExitCode.Should().Be(0);
        }
    }

    public class PromptLabModelsTests
    {
        [Fact]
        public void CreateSessionRequest_DefaultProvider_IsOllama()
        {
            // Arrange & Act
            var request = new CreateSessionRequest();

            // Assert
            request.Provider.Should().Be(LlmProviderType.Ollama);
        }

        [Fact]
        public void PromptResponse_TokensPerSecond_CalculatesCorrectly()
        {
            // Arrange
            var response = new PromptResponse
            {
                Prompt = "Test",
                Response = "Result",
                TokensGenerated = 60,
                Duration = TimeSpan.FromSeconds(3),
                TokensPerSecond = 20,
                Success = true
            };

            // Assert
            response.TokensPerSecond.Should().Be(20);
        }

        [Fact]
        public void SessionInfoResponse_DefaultHistory_IsEmpty()
        {
            // Arrange & Act
            var response = new SessionInfoResponse
            {
                SessionId = "test",
                Provider = "Ollama"
            };

            // Assert
            response.History.Should().BeEmpty();
            response.HistoryCount.Should().Be(0);
        }

        [Fact]
        public void RenderTemplateRequest_CanBeCreated()
        {
            // Arrange & Act
            var request = new RenderTemplateRequest
            {
                Template = "Hello {{name}}",
                Variables = new Dictionary<string, string> { ["name"] = "World" }
            };

            // Assert
            request.Template.Should().Be("Hello {{name}}");
            request.Variables.Should().ContainKey("name");
        }
    }

    public class LlmProviderTypeTests
    {
        [Theory]
        [InlineData(LlmProviderType.Ollama, "Ollama")]
        [InlineData(LlmProviderType.LmStudio, "LmStudio")]
        [InlineData(LlmProviderType.OpenAI, "OpenAI")]
        public void LlmProviderType_ToString_ReturnsExpected(LlmProviderType type, string expected)
        {
            // Act & Assert
            type.ToString().Should().Be(expected);
        }

        [Fact]
        public void LlmProviderType_HasAllExpectedValues()
        {
            // Act
            var values = Enum.GetValues<LlmProviderType>();

            // Assert
            values.Should().HaveCount(4); // Ollama, LmStudio, OpenAI, Local
        }
    }
}

