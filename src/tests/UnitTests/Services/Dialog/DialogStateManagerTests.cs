using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Core.Models.Dialog;
using CKY.MultiAgentFramework.Core.Models.Message;
using CKY.MultiAgentFramework.Core.Models.Task;
using CKY.MultiAgentFramework.Services.Dialog;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace CKY.MultiAgentFramework.Tests.Services.Dialog
{
    /// <summary>
    /// DialogStateManager 单元测试
    /// Unit tests for DialogStateManager
    /// </summary>
    public class DialogStateManagerTests
    {
        private readonly Mock<IMafSessionStorage> _mockSessionStorage;
        private readonly Mock<ILogger<DialogStateManager>> _mockLogger;
        private readonly DialogStateManager _sut;

        public DialogStateManagerTests()
        {
            _mockSessionStorage = new Mock<IMafSessionStorage>();
            _mockLogger = new Mock<ILogger<DialogStateManager>>();
            _sut = new DialogStateManager(_mockSessionStorage.Object, _mockLogger.Object);
        }

        #region LoadOrCreateAsync Tests

        [Fact]
        public async Task LoadOrCreateAsync_SessionNotExists_ShouldCreateNewContext()
        {
            // Arrange
            var conversationId = "conv-123";
            var userId = "user-456";
            _mockSessionStorage
                .Setup(x => x.ExistsAsync(conversationId, default))
                .ReturnsAsync(false);

            // Act
            var result = await _sut.LoadOrCreateAsync(conversationId, userId);

            // Assert
            result.Should().NotBeNull();
            result.SessionId.Should().Be(conversationId);
            result.UserId.Should().Be(userId);
            result.TurnCount.Should().Be(1);
            result.HistoricalSlots.Should().BeEmpty();
            result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        }

        [Fact]
        public async Task LoadOrCreateAsync_SessionExistsWithoutContext_ShouldReturnNewContext()
        {
            // Arrange
            var conversationId = "conv-123";
            var userId = "user-456";
            var mockSession = new Mock<IAgentSession>();
            mockSession.Setup(x => x.Context).Returns(new Dictionary<string, object>());

            _mockSessionStorage
                .Setup(x => x.ExistsAsync(conversationId, default))
                .ReturnsAsync(true);
            _mockSessionStorage
                .Setup(x => x.LoadSessionAsync(conversationId, default))
                .ReturnsAsync(mockSession.Object);

            // Act
            var result = await _sut.LoadOrCreateAsync(conversationId, userId);

            // Assert
            result.Should().NotBeNull();
            result.SessionId.Should().Be(conversationId);
            result.TurnCount.Should().Be(1);
        }

        [Fact]
        public async Task LoadOrCreateAsync_SessionExistsWithContext_ShouldReturnExistingContext()
        {
            // Arrange
            var conversationId = "conv-123";
            var userId = "user-456";
            var existingContext = new DialogContext
            {
                SessionId = conversationId,
                UserId = userId,
                TurnCount = 5,
                HistoricalSlots = new Dictionary<string, object>
                {
                    ["control_device.Location"] = 3
                }
            };

            var mockSession = new Mock<IAgentSession>();
            mockSession.Setup(x => x.Context).Returns(new Dictionary<string, object>
            {
                ["dialog_context"] = existingContext
            });

            _mockSessionStorage
                .Setup(x => x.ExistsAsync(conversationId, default))
                .ReturnsAsync(true);
            _mockSessionStorage
                .Setup(x => x.LoadSessionAsync(conversationId, default))
                .ReturnsAsync(mockSession.Object);

            // Act
            var result = await _sut.LoadOrCreateAsync(conversationId, userId);

            // Assert
            result.Should().NotBeNull();
            result.TurnCount.Should().Be(5);
            result.HistoricalSlots["control_device.Location"].Should().Be(3);
        }

        #endregion

        #region UpdateAsync Tests

        [Fact]
        public async Task UpdateAsync_ShouldIncrementTurnCountAndUpdateContext()
        {
            // Arrange
            var context = new DialogContext
            {
                SessionId = "conv-123",
                UserId = "user-456",
                TurnCount = 1
            };
            var intent = "control_device";
            var slots = new Dictionary<string, object>
            {
                ["Location"] = "客厅",
                ["Device"] = "灯"
            };
            var executionResults = new List<TaskExecutionResult>();

            SetupMockSessionForSave(context.SessionId);

            // Act
            await _sut.UpdateAsync(context, intent, slots, executionResults);

            // Assert
            context.TurnCount.Should().Be(2);
            context.PreviousIntent.Should().Be(intent);
            context.PreviousSlots.Should().BeEquivalentTo(slots);
            context.HistoricalSlots.Should().ContainKey("control_device.Location");
            context.HistoricalSlots["control_device.Location"].Should().Be(1);
            context.HistoricalSlots["control_device.Device"].Should().Be(1);
        }

        [Fact]
        public async Task UpdateAsync_RepeatedSlot_ShouldIncrementCount()
        {
            // Arrange
            var context = new DialogContext
            {
                SessionId = "conv-123",
                UserId = "user-456",
                HistoricalSlots = new Dictionary<string, object>
                {
                    ["control_device.Location"] = 2
                }
            };
            var intent = "control_device";
            var slots = new Dictionary<string, object>
            {
                ["Location"] = "客厅"
            };
            var executionResults = new List<TaskExecutionResult>();

            SetupMockSessionForSave(context.SessionId);

            // Act
            await _sut.UpdateAsync(context, intent, slots, executionResults);

            // Assert
            context.HistoricalSlots["control_device.Location"].Should().Be(3);
        }

        [Fact]
        public async Task UpdateAsync_NullContext_ShouldThrowArgumentNullException()
        {
            // Arrange
            var intent = "control_device";
            var slots = new Dictionary<string, object>();
            var executionResults = new List<TaskExecutionResult>();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await _sut.UpdateAsync(null!, intent, slots, executionResults));
        }

        #endregion

        #region RecordPendingClarificationAsync Tests

        [Fact]
        public async Task RecordPendingClarificationAsync_ShouldSetPendingClarification()
        {
            // Arrange
            var context = new DialogContext
            {
                SessionId = "conv-123",
                UserId = "user-456"
            };
            var intent = "control_device";
            var detectedSlots = new Dictionary<string, object>
            {
                ["Device"] = "灯"
            };
            var missingSlots = new List<SlotDefinition>
            {
                new SlotDefinition
                {
                    SlotName = "Location",
                    Description = "位置",
                    Required = true
                }
            };

            SetupMockSessionForSave(context.SessionId);

            // Act
            await _sut.RecordPendingClarificationAsync(context, intent, detectedSlots, missingSlots);

            // Assert
            context.PendingClarification.Should().NotBeNull();
            context.PendingClarification!.Intent.Should().Be(intent);
            context.PendingClarification.DetectedSlots.Should().BeEquivalentTo(detectedSlots);
            context.PendingClarification.MissingSlots.Should().HaveCount(1);
            context.PendingClarification.MissingSlots[0].SlotName.Should().Be("Location");
        }

        [Fact]
        public async Task RecordPendingClarificationAsync_NullContext_ShouldThrowArgumentNullException()
        {
            // Arrange
            var intent = "control_device";
            var detectedSlots = new Dictionary<string, object>();
            var missingSlots = new List<SlotDefinition>();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await _sut.RecordPendingClarificationAsync(null!, intent, detectedSlots, missingSlots));
        }

        [Fact]
        public async Task RecordPendingClarificationAsync_NullDetectedSlots_ShouldThrowArgumentNullException()
        {
            // Arrange
            var context = new DialogContext();
            var intent = "control_device";
            var missingSlots = new List<SlotDefinition>();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await _sut.RecordPendingClarificationAsync(context, intent, null!, missingSlots));
        }

        [Fact]
        public async Task RecordPendingClarificationAsync_NullMissingSlots_ShouldThrowArgumentNullException()
        {
            // Arrange
            var context = new DialogContext();
            var intent = "control_device";
            var detectedSlots = new Dictionary<string, object>();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await _sut.RecordPendingClarificationAsync(context, intent, detectedSlots, null!));
        }

        #endregion

        #region RecordPendingTasksAsync Tests

        [Fact]
        public async Task RecordPendingTasksAsync_ShouldSetPendingTask()
        {
            // Arrange
            var context = new DialogContext
            {
                SessionId = "conv-123",
                UserId = "user-456"
            };
            var plan = new ExecutionPlan
            {
                PlanId = "plan-123"
            };
            var filledSlots = new Dictionary<string, object>
            {
                ["Device"] = "灯"
            };

            SetupMockSessionForSave(context.SessionId);

            // Act
            await _sut.RecordPendingTasksAsync(context, plan, filledSlots);

            // Assert
            context.PendingTask.Should().NotBeNull();
            context.PendingTask!.Plan.Should().Be(plan);
            context.PendingTask.FilledSlots.Should().BeEquivalentTo(filledSlots);
            context.PendingTask.StillMissing.Should().BeEmpty();
        }

        [Fact]
        public async Task RecordPendingTasksAsync_NullContext_ShouldThrowArgumentNullException()
        {
            // Arrange
            var plan = new ExecutionPlan();
            var filledSlots = new Dictionary<string, object>();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await _sut.RecordPendingTasksAsync(null!, plan, filledSlots));
        }

        [Fact]
        public async Task RecordPendingTasksAsync_NullPlan_ShouldThrowArgumentNullException()
        {
            // Arrange
            var context = new DialogContext();
            var filledSlots = new Dictionary<string, object>();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await _sut.RecordPendingTasksAsync(context, null!, filledSlots));
        }

        [Fact]
        public async Task RecordPendingTasksAsync_NullFilledSlots_ShouldThrowArgumentNullException()
        {
            // Arrange
            var context = new DialogContext();
            var plan = new ExecutionPlan();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await _sut.RecordPendingTasksAsync(context, plan, null!));
        }

        #endregion

        #region HandleClarificationResponseAsync Tests

        [Fact]
        public async Task HandleClarificationResponseAsync_NoPendingClarification_ShouldReturnFailure()
        {
            // Arrange
            var conversationId = "conv-123";
            var userResponse = "在客厅";
            SetupMockSessionForLoad(conversationId, createNew: true);

            // Act
            var result = await _sut.HandleClarificationResponseAsync(conversationId, userResponse);

            // Assert
            result.Success.Should().BeFalse();
            result.Result.Should().Contain("没有待处理的澄清问题");
        }

        [Fact]
        public async Task HandleClarificationResponseAsync_WithPendingClarification_ShouldReturnSuccess()
        {
            // Arrange
            var conversationId = "conv-123";
            var userResponse = "在客厅";
            var context = new DialogContext
            {
                SessionId = conversationId,
                UserId = "user-456",
                PendingClarification = new PendingClarificationInfo
                {
                    Intent = "control_device",
                    DetectedSlots = new Dictionary<string, object>
                    {
                        ["Device"] = "灯"
                    },
                    MissingSlots = new List<SlotDefinition>
                    {
                        new SlotDefinition { SlotName = "Location" }
                    }
                }
            };

            SetupMockSessionForLoad(conversationId, context);

            // Act
            var result = await _sut.HandleClarificationResponseAsync(conversationId, userResponse);

            // Assert
            result.Success.Should().BeTrue();
            result.Result.Should().Contain("已理解");
        }

        [Fact]
        public async Task HandleClarificationResponseAsync_AfterProcessing_ShouldClearPendingClarification()
        {
            // Arrange
            var conversationId = "conv-123";
            var userResponse = "在客厅";
            var context = new DialogContext
            {
                SessionId = conversationId,
                UserId = "user-456",
                PendingClarification = new PendingClarificationInfo
                {
                    Intent = "control_device"
                }
            };

            SetupMockSessionForLoad(conversationId, context);

            // Act
            await _sut.HandleClarificationResponseAsync(conversationId, userResponse);

            // Assert
            context.PendingClarification.Should().BeNull();
        }

        [Fact]
        public async Task HandleClarificationResponseAsync_EmptyConversationId_ShouldThrowArgumentException()
        {
            // Arrange
            var userResponse = "在客厅";

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(async () =>
                await _sut.HandleClarificationResponseAsync("", userResponse));
        }

        [Fact]
        public async Task HandleClarificationResponseAsync_EmptyUserResponse_ShouldThrowArgumentException()
        {
            // Arrange
            var conversationId = "conv-123";

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(async () =>
                await _sut.HandleClarificationResponseAsync(conversationId, ""));
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// 设置模拟会话存储用于保存操作
        /// </summary>
        private void SetupMockSessionForSave(string sessionId, CancellationToken ct = default)
        {
            var mockSession = new Mock<IAgentSession>();
            mockSession.Setup(x => x.Context).Returns(new Dictionary<string, object>());

            _mockSessionStorage
                .Setup(x => x.LoadSessionAsync(sessionId, ct))
                .ReturnsAsync(mockSession.Object);

            _mockSessionStorage
                .Setup(x => x.SaveSessionAsync(It.IsAny<IAgentSession>(), ct))
                .Returns(Task.CompletedTask);
        }

        /// <summary>
        /// 设置模拟会话存储用于加载操作
        /// </summary>
        private void SetupMockSessionForLoad(string sessionId, DialogContext? context = null, bool createNew = false, CancellationToken ct = default)
        {
            var mockSession = new Mock<IAgentSession>();

            if (context != null)
            {
                mockSession.Setup(x => x.Context).Returns(new Dictionary<string, object>
                {
                    ["dialog_context"] = context
                });
            }
            else
            {
                mockSession.Setup(x => x.Context).Returns(new Dictionary<string, object>());
            }

            _mockSessionStorage
                .Setup(x => x.ExistsAsync(sessionId, ct))
                .ReturnsAsync(!createNew);

            _mockSessionStorage
                .Setup(x => x.LoadSessionAsync(sessionId, ct))
                .ReturnsAsync(mockSession.Object);

            _mockSessionStorage
                .Setup(x => x.SaveSessionAsync(It.IsAny<IAgentSession>(), ct))
                .Returns(Task.CompletedTask);
        }

        #endregion
    }
}
