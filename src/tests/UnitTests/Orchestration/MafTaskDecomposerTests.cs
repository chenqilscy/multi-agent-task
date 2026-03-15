using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Core.Enums;
using CKY.MultiAgentFramework.Core.Models.Task;
using CKY.MultiAgentFramework.Services.Orchestration;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace CKY.MultiAgentFramework.Tests.Orchestration
{
    /// <summary>
    /// 测试用的意图能力提供者
    /// </summary>
    public class TestIntentCapabilityProvider : IIntentCapabilityProvider
    {
        private readonly Dictionary<string, string> _map = new(StringComparer.OrdinalIgnoreCase)
        {
            ["ControlLight"] = "lighting",
            ["AdjustClimate"] = "climate",
            ["PlayMusic"] = "music",
            ["SecurityControl"] = "security",
            ["GeneralQuery"] = "general"
        };

        public string? GetCapability(string intent)
        {
            _map.TryGetValue(intent, out var capability);
            return capability;
        }

        public IEnumerable<string> GetSupportedIntents()
        {
            return _map.Keys;
        }
    }

    public class MafTaskDecomposerTests
    {
        private readonly MafTaskDecomposer _sut;
        private readonly TestIntentCapabilityProvider _capabilityProvider;

        public MafTaskDecomposerTests()
        {
            _capabilityProvider = new TestIntentCapabilityProvider();
            _sut = new MafTaskDecomposer(_capabilityProvider, NullLogger<MafTaskDecomposer>.Instance);
        }

        [Fact]
        public async Task DecomposeAsync_WithKnownIntent_ShouldCreateSubTask()
        {
            // Arrange
            var userInput = "打开客厅的灯";
            var intent = new IntentRecognitionResult
            {
                PrimaryIntent = "ControlLight",
                Confidence = 0.9,
                OriginalInput = userInput
            };

            // Act
            var result = await _sut.DecomposeAsync(userInput, intent);

            // Assert
            result.Should().NotBeNull();
            result.OriginalUserInput.Should().Be(userInput);
            result.SubTasks.Should().HaveCount(1);
            result.SubTasks[0].RequiredCapability.Should().Be("lighting");
            result.SubTasks[0].Intent.Should().Be("ControlLight");
        }

        [Fact]
        public async Task DecomposeAsync_WithUnknownIntent_ShouldCreateGenericTask()
        {
            // Arrange
            var userInput = "执行一些不明确的操作";
            var intent = new IntentRecognitionResult
            {
                PrimaryIntent = "Unknown",
                Confidence = 0,
                OriginalInput = userInput
            };

            // Act
            var result = await _sut.DecomposeAsync(userInput, intent);

            // Assert
            result.Should().NotBeNull();
            result.SubTasks.Should().HaveCount(1);
            result.SubTasks[0].RequiredCapability.Should().Be("general");
        }

        [Fact]
        public async Task DecomposeAsync_ShouldSetMetadata()
        {
            // Arrange
            var intent = new IntentRecognitionResult { PrimaryIntent = "PlayMusic" };

            // Act
            var result = await _sut.DecomposeAsync("播放音乐", intent);

            // Assert
            result.Metadata.Should().NotBeNull();
            result.Metadata.Strategy.Should().Be("RuleBased");
            result.DecompositionId.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task DecomposeTaskWithLlmAsync_SimpleTask_ShouldCreateSingleSubtask()
        {
            // Arrange
            var task = "打开客厅的灯";

            // Act
            var result = await _sut.DecomposeTaskWithLlmAsync(task);

            // Assert
            result.Should().NotBeNull();
            result.SubTasks.Should().HaveCount(1);
            result.SubTasks[0].Intent.Should().Be("ControlLight");
            result.Metadata.Strategy.Should().Be("LlmAssisted");
        }

        [Fact]
        public async Task DecomposeTaskWithLlmAsync_ComplexTaskWithConnector_ShouldDecompose()
        {
            // Arrange
            var task = "打开客厅的灯并且播放音乐";

            // Act
            var result = await _sut.DecomposeTaskWithLlmAsync(task);

            // Assert
            result.Should().NotBeNull();
            result.SubTasks.Should().HaveCountGreaterThan(1, "should decompose complex task");
            result.SubTasks[0].Priority.Should().Be(TaskPriority.High, "first task should be high priority");
        }

        [Fact]
        public async Task DecomposeTaskWithLlmAsync_ShouldSetCorrectParameters()
        {
            // Arrange
            var task = "调节温度到26度";

            // Act
            var result = await _sut.DecomposeTaskWithLlmAsync(task);

            // Assert
            result.SubTasks.Should().NotBeEmpty();
            result.SubTasks[0].Parameters.Should().ContainKey("userInput");
            result.SubTasks[0].RequiredCapability.Should().Be("climate");
        }

        [Fact]
        public async Task DecomposeTaskWithLlmAsync_MultiPartTask_ShouldCreateMultipleSubtasks()
        {
            // Arrange
            var task = "打开灯然后播放音乐";

            // Act
            var result = await _sut.DecomposeTaskWithLlmAsync(task);

            // Assert
            result.SubTasks.Should().HaveCountGreaterThan(1);
            foreach (var subtask in result.SubTasks)
            {
                subtask.Parameters.Should().ContainKey("order");
            }
        }
    }
}
