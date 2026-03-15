using CKY.MultiAgentFramework.Core.Models.Dialog;
using CKY.MultiAgentFramework.Services.Dialog;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace CKY.MultiAgentFramework.Tests.Services.Dialog
{
    public class DialogStateManagerTests
    {
        private readonly DialogStateManager _sut;

        public DialogStateManagerTests()
        {
            _sut = new DialogStateManager(NullLogger<DialogStateManager>.Instance);
        }

        [Fact]
        public async Task PushStateAsync_ShouldIncreaseStackDepth()
        {
            // Arrange
            var initialState = _sut.StackDepth;
            var state = new DialogState { CurrentIntent = "ControlLight" };

            // Act
            await _sut.PushStateAsync(state);

            // Assert
            _sut.StackDepth.Should().Be(initialState + 1);
        }

        [Fact]
        public async Task PushStateAsync_NullState_ShouldThrow()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(async () => await _sut.PushStateAsync(null!));
        }

        [Fact]
        public async Task PopStateAsync_WithStates_ShouldReturnTopState()
        {
            // Arrange
            var state1 = new DialogState { CurrentIntent = "ControlLight" };
            var state2 = new DialogState { CurrentIntent = "PlayMusic" };
            await _sut.PushStateAsync(state1);
            await _sut.PushStateAsync(state2);

            // Act
            var popped = await _sut.PopStateAsync();

            // Assert
            popped.Should().NotBeNull();
            popped!.CurrentIntent.Should().Be("PlayMusic");
            _sut.StackDepth.Should().Be(1);
        }

        [Fact]
        public async Task PopStateAsync_EmptyStack_ShouldReturnNull()
        {
            // Act
            var result = await _sut.PopStateAsync();

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetCurrentStateAsync_WithStates_ShouldReturnTopWithoutPopping()
        {
            // Arrange
            var state = new DialogState { CurrentIntent = "ControlLight" };
            await _sut.PushStateAsync(state);

            // Act
            var current = await _sut.GetCurrentStateAsync();

            // Assert
            current.Should().NotBeNull();
            current!.CurrentIntent.Should().Be("ControlLight");
            _sut.StackDepth.Should().Be(1, "state should not be popped");
        }

        [Fact]
        public async Task GetCurrentStateAsync_EmptyStack_ShouldReturnNull()
        {
            // Act
            var result = await _sut.GetCurrentStateAsync();

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task HandleTopicSwitchAsync_DifferentIntent_ShouldReturnTrue()
        {
            // Arrange
            var currentState = new DialogState { CurrentIntent = "ControlLight" };
            await _sut.PushStateAsync(currentState);

            // Act
            var shouldSave = await _sut.HandleTopicSwitchAsync("PlayMusic");

            // Assert
            shouldSave.Should().BeTrue();
        }

        [Fact]
        public async Task HandleTopicSwitchAsync_SameIntent_ShouldReturnFalse()
        {
            // Arrange
            var currentState = new DialogState { CurrentIntent = "ControlLight" };
            await _sut.PushStateAsync(currentState);

            // Act
            var shouldSave = await _sut.HandleTopicSwitchAsync("ControlLight");

            // Assert
            shouldSave.Should().BeFalse();
        }

        [Fact]
        public async Task HandleTopicSwitchAsync_EmptyStack_ShouldReturnFalse()
        {
            // Act
            var shouldSave = await _sut.HandleTopicSwitchAsync("PlayMusic");

            // Assert
            shouldSave.Should().BeFalse();
        }

        [Fact]
        public async Task RollbackAsync_WithStates_ShouldSucceed()
        {
            // Arrange
            await _sut.PushStateAsync(new DialogState { CurrentIntent = "ControlLight" });
            await _sut.PushStateAsync(new DialogState { CurrentIntent = "PlayMusic" });

            // Act
            var result = await _sut.RollbackAsync();

            // Assert
            result.Should().BeTrue();
            _sut.StackDepth.Should().Be(1);
            var current = await _sut.GetCurrentStateAsync();
            current!.CurrentIntent.Should().Be("ControlLight");
        }

        [Fact]
        public async Task RollbackAsync_EmptyStack_ShouldReturnFalse()
        {
            // Act
            var result = await _sut.RollbackAsync();

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task ClearAllAsync_ShouldEmptyStack()
        {
            // Arrange
            await _sut.PushStateAsync(new DialogState { CurrentIntent = "ControlLight" });
            await _sut.PushStateAsync(new DialogState { CurrentIntent = "PlayMusic" });

            // Act
            await _sut.ClearAllAsync();

            // Assert
            _sut.StackDepth.Should().Be(0);
        }

        [Fact]
        public async Task GetAllStatesAsync_ShouldReturnAllStates()
        {
            // Arrange
            var state1 = new DialogState { CurrentIntent = "ControlLight" };
            var state2 = new DialogState { CurrentIntent = "PlayMusic" };
            await _sut.PushStateAsync(state1);
            await _sut.PushStateAsync(state2);

            // Act
            var allStates = await _sut.GetAllStatesAsync();

            // Assert
            allStates.Should().HaveCount(2);
            allStates[0].CurrentIntent.Should().Be("PlayMusic", "top of stack should be first");
            allStates[1].CurrentIntent.Should().Be("ControlLight");
        }

        [Fact]
        public async Task DialogState_ShouldPreserveAllProperties()
        {
            // Arrange
            var state = new DialogState
            {
                CurrentIntent = "ControlLight",
                SlotValues = new Dictionary<string, object>
                {
                    ["location"] = "客厅",
                    ["device"] = "灯"
                },
                IsCompleted = false,
                Metadata = new Dictionary<string, object>
                {
                    ["turn_count"] = 3
                }
            };

            await _sut.PushStateAsync(state);

            // Act
            var retrieved = await _sut.GetCurrentStateAsync();

            // Assert
            retrieved.Should().BeEquivalentTo(state);
        }

        [Fact]
        public async Task MultiLevelPushPop_ShouldMaintainOrder()
        {
            // Arrange
            var states = new[]
            {
                new DialogState { CurrentIntent = "Intent1" },
                new DialogState { CurrentIntent = "Intent2" },
                new DialogState { CurrentIntent = "Intent3" }
            };

            foreach (var state in states)
            {
                await _sut.PushStateAsync(state);
            }

            // Act & Assert
            foreach (var expectedState in states.Reverse())
            {
                var popped = await _sut.PopStateAsync();
                popped!.CurrentIntent.Should().Be(expectedState.CurrentIntent);
            }

            _sut.StackDepth.Should().Be(0);
        }
    }
}
