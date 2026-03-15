using CKY.MultiAgentFramework.Core.Models.Dialog;
using CKY.MultiAgentFramework.Services.NLP;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace CKY.MultiAgentFramework.Tests.NLP
{
    public class IntentDriftDetectorTests
    {
        private readonly IntentDriftDetector _sut;

        public IntentDriftDetectorTests()
        {
            _sut = new IntentDriftDetector(NullLogger<IntentDriftDetector>.Instance);
        }

        [Fact]
        public async Task DetectDriftAsync_WithTopicSwitchTrigger_ShouldDetectDrift()
        {
            // Arrange
            var currentInput = "对了，今天天气怎么样";
            var previousIntent = "ControlLight";
            var context = new DialogContext { SessionId = "session1" };

            // Act
            var result = await _sut.DetectDriftAsync(currentInput, previousIntent, context);

            // Assert
            result.HasDrifted.Should().BeTrue();
            result.DriftScore.Should().BeGreaterThan(0.7);
            result.SuggestedAction.Should().Be(DriftAction.NewTopic);
            result.Reason.Should().Contain("触发词");
        }

        [Fact]
        public async Task DetectDriftAsync_WithoutTrigger_ShouldNotDetectDrift()
        {
            // Arrange
            var currentInput = "把它调低一点";
            var previousIntent = "AdjustClimate";
            var context = new DialogContext
            {
                SessionId = "session1",
                HistoricalSlots = new Dictionary<string, object>
                {
                    ["adjust.device"] = "空调"
                }
            };

            // Act
            var result = await _sut.DetectDriftAsync(currentInput, previousIntent, context);

            // Assert
            result.HasDrifted.Should().BeFalse();
            result.DriftScore.Should().BeLessThan(0.5);
            result.SuggestedAction.Should().Be(DriftAction.Continue);
        }

        [Fact]
        public async Task DetectDriftAsync_WithLowSemanticSimilarity_ShouldDetectPossibleDrift()
        {
            // Arrange
            var currentInput = "播放周杰伦的歌";
            var previousIntent = "ControlLight";
            var context = new DialogContext
            {
                SessionId = "session1",
                TurnCount = 5,
                HistoricalSlots = new Dictionary<string, object>
                {
                    ["control.device"] = "客厅灯",
                    ["control.location"] = "客厅"
                }
            };

            // Act
            var result = await _sut.DetectDriftAsync(currentInput, previousIntent, context);

            // Assert
            result.HasDrifted.Should().BeTrue();
            result.SuggestedAction.Should().Be(DriftAction.PossibleNewTopic);
        }

        [Fact]
        public async Task DetectDriftAsync_WithHighTurnCount_ShouldIncreaseDriftScore()
        {
            // Arrange
            var currentInput = "打开客厅的灯";
            var previousIntent = "ControlLight";
            var context = new DialogContext
            {
                SessionId = "session1",
                TurnCount = 15, // 长对话
                HistoricalSlots = new Dictionary<string, object>
                {
                    ["control.device"] = "空调"
                }
            };

            // Act
            var result = await _sut.DetectDriftAsync(currentInput, previousIntent, context);

            // Assert
            result.DriftScore.Should().BeGreaterThan(0.1);
            result.Reason.Should().Contain("对话轮次");
        }

        [Theory]
        [InlineData("顺便问一下")]
        [InlineData("另外")]
        [InlineData("对了")]
        [InlineData("还有一件事")]
        public async Task DetectDriftAsync_WithVariousTriggers_ShouldDetectNewTopic(string trigger)
        {
            // Arrange
            var currentInput = $"{trigger}，今天北京的天气怎么样";
            var previousIntent = "PlayMusic";
            var context = new DialogContext { SessionId = "session1" };

            // Act
            var result = await _sut.DetectDriftAsync(currentInput, previousIntent, context);

            // Assert
            result.HasDrifted.Should().BeTrue();
            result.SuggestedAction.Should().Be(DriftAction.NewTopic);
        }

        [Fact]
        public async Task DetectDriftAsync_WithEmptyContext_ShouldReturnContinue()
        {
            // Arrange
            var currentInput = "打开空调";
            var previousIntent = "";
            var context = new DialogContext { SessionId = "session1" };

            // Act
            var result = await _sut.DetectDriftAsync(currentInput, previousIntent, context);

            // Assert
            result.HasDrifted.Should().BeFalse();
            result.SuggestedAction.Should().Be(DriftAction.Continue);
        }
    }
}
