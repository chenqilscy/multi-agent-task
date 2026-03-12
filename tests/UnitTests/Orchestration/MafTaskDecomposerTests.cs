using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Core.Enums;
using CKY.MultiAgentFramework.Core.Models.Task;
using CKY.MultiAgentFramework.Services.Orchestration;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace CKY.MultiAgentFramework.Tests.Orchestration
{
    public class MafTaskDecomposerTests
    {
        private readonly MafTaskDecomposer _sut;

        public MafTaskDecomposerTests()
        {
            _sut = new MafTaskDecomposer(NullLogger<MafTaskDecomposer>.Instance);
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
    }
}
