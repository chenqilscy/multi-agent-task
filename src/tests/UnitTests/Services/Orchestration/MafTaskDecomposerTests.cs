using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Services.Orchestration;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CKY.MultiAgentFramework.Tests.Services.Orchestration
{
    public class MafTaskDecomposerTests
    {
        private readonly Mock<IIntentCapabilityProvider> _mockCapProvider = new();
        private readonly Mock<ILogger<MafTaskDecomposer>> _mockLogger = new();
        private readonly MafTaskDecomposer _decomposer;

        public MafTaskDecomposerTests()
        {
            _decomposer = new MafTaskDecomposer(_mockCapProvider.Object, _mockLogger.Object);
        }

        [Fact]
        public void Constructor_NullProvider_ShouldThrow()
        {
            var act = () => new MafTaskDecomposer(null!, _mockLogger.Object);
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_NullLogger_ShouldThrow()
        {
            var act = () => new MafTaskDecomposer(_mockCapProvider.Object, null!);
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public async Task DecomposeAsync_KnownIntent_ShouldCreatePrimaryTask()
        {
            _mockCapProvider.Setup(p => p.GetCapability("light_control"))
                .Returns("lighting");

            var intent = new IntentRecognitionResult
            {
                PrimaryIntent = "light_control",
                Confidence = 0.9,
                OriginalInput = "打开客厅的灯"
            };

            var result = await _decomposer.DecomposeAsync("打开客厅的灯", intent);

            result.SubTasks.Should().HaveCount(1);
            result.SubTasks[0].RequiredCapability.Should().Be("lighting");
            result.SubTasks[0].PriorityScore.Should().Be(50);
            result.SubTasks[0].Parameters.Should().ContainKey("userInput");
            result.Metadata.Strategy.Should().Be("RuleBased");
        }

        [Fact]
        public async Task DecomposeAsync_UnknownIntent_ShouldCreateGenericTask()
        {
            _mockCapProvider.Setup(p => p.GetCapability("unknown"))
                .Returns((string?)null);

            var intent = new IntentRecognitionResult
            {
                PrimaryIntent = "unknown",
                Confidence = 0.3,
                OriginalInput = "随便说说"
            };

            var result = await _decomposer.DecomposeAsync("随便说说", intent);

            result.SubTasks.Should().HaveCount(1);
            result.SubTasks[0].RequiredCapability.Should().Be("general");
            result.SubTasks[0].PriorityScore.Should().Be(30);
        }

        [Fact]
        public async Task DecomposeAsync_ShouldSetOriginalInput()
        {
            _mockCapProvider.Setup(p => p.GetCapability(It.IsAny<string>()))
                .Returns("cap");

            var intent = new IntentRecognitionResult
            {
                PrimaryIntent = "test",
                OriginalInput = "原始输入"
            };

            var result = await _decomposer.DecomposeAsync("原始输入", intent);

            result.OriginalUserInput.Should().Be("原始输入");
            result.Intent.Should().Be(intent);
        }

        [Fact]
        public async Task DecomposeTaskWithLlmAsync_ShouldDecompose()
        {
            var result = await _decomposer.DecomposeTaskWithLlmAsync("打开灯并且关闭窗帘");

            result.Should().NotBeNull();
            result.SubTasks.Should().NotBeEmpty();
            result.OriginalUserInput.Should().Be("打开灯并且关闭窗帘");
        }

        [Fact]
        public async Task DecomposeTaskWithLlmAsync_WithConjunctions_ShouldSplitTasks()
        {
            var result = await _decomposer.DecomposeTaskWithLlmAsync("打开灯并且设置温度");

            result.SubTasks.Should().HaveCountGreaterThanOrEqualTo(2);
        }

        [Fact]
        public async Task DecomposeTaskWithLlmAsync_SingleTask_ShouldCreateOneTask()
        {
            var result = await _decomposer.DecomposeTaskWithLlmAsync("打开灯");

            result.SubTasks.Should().HaveCountGreaterThanOrEqualTo(1);
        }
    }
}
