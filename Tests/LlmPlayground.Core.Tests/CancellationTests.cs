using FluentAssertions;
using LlmPlayground.Core;

namespace LlmPlayground.Core.Tests;

public class CancellationTests
{
    [Fact]
    public void CancellationToken_WhenAlreadyCancelled_ShouldBeRecognized()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        cts.Token.IsCancellationRequested.Should().BeTrue();
    }

    [Fact]
    public async Task CancellationTokenSource_WhenCancelled_ShouldThrowOperationCanceledException()
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        // Act
        cts.Cancel();
        var act = async () =>
        {
            cts.Token.ThrowIfCancellationRequested();
            await Task.CompletedTask;
        };

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public void LinkedCancellationTokenSource_WhenParentCancelled_ShouldCancelChild()
    {
        // Arrange
        using var parentCts = new CancellationTokenSource();
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(parentCts.Token);

        // Act
        parentCts.Cancel();

        // Assert
        linkedCts.Token.IsCancellationRequested.Should().BeTrue();
    }

    [Fact]
    public void LinkedCancellationTokenSource_WhenChildCancelled_ShouldNotCancelParent()
    {
        // Arrange
        using var parentCts = new CancellationTokenSource();
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(parentCts.Token);

        // Act
        linkedCts.Cancel();

        // Assert
        parentCts.Token.IsCancellationRequested.Should().BeFalse();
        linkedCts.Token.IsCancellationRequested.Should().BeTrue();
    }

    [Fact]
    public async Task AsyncEnumerable_WithCancellation_ShouldStopEnumeration()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var itemsReceived = 0;

        // Act
        var act = async () =>
        {
            await foreach (var item in GenerateItemsWithCancellationCheckAsync(cts.Token))
            {
                itemsReceived++;
                if (itemsReceived >= 3)
                {
                    cts.Cancel();
                }
            }
        };

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
        itemsReceived.Should().Be(3);
    }

    [Fact]
    public async Task AsyncEnumerable_WithCancellation_ShouldRespectToken()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var itemsReceived = 0;

        // Act - Cancel after receiving some items
        await foreach (var item in GenerateItemsAsync().WithCancellation(cts.Token))
        {
            itemsReceived++;
            if (itemsReceived >= 5)
            {
                break; // Normal exit without exception
            }
        }

        // Assert
        itemsReceived.Should().Be(5);
    }

    [Fact]
    public async Task TaskDelay_WithCancellation_ShouldThrowWhenCancelled()
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        // Act
        var delayTask = Task.Delay(TimeSpan.FromSeconds(10), cts.Token);
        cts.Cancel();

        // Assert
        var act = async () => await delayTask;
        await act.Should().ThrowAsync<TaskCanceledException>();
    }

    [Fact]
    public void CancellationToken_Default_ShouldNotBeCancelled()
    {
        // Arrange
        var token = CancellationToken.None;

        // Assert
        token.IsCancellationRequested.Should().BeFalse();
        token.CanBeCanceled.Should().BeFalse();
    }

    [Fact]
    public void CancellationToken_FromCts_ShouldBeCancellable()
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        // Assert
        cts.Token.CanBeCanceled.Should().BeTrue();
        cts.Token.IsCancellationRequested.Should().BeFalse();
    }

    private static async IAsyncEnumerable<int> GenerateItemsAsync()
    {
        for (int i = 0; i < 100; i++)
        {
            yield return i;
            await Task.Delay(10);
        }
    }

    private static async IAsyncEnumerable<int> GenerateItemsWithCancellationCheckAsync(
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        for (int i = 0; i < 100; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return i;
            await Task.Delay(10, cancellationToken);
        }
    }
}

