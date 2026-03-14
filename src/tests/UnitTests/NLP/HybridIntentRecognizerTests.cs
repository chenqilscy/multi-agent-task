using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Core.Models.Task;
using CKY.MultiAgentFramework.Services.NLP;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace CKY.MultiAgentFramework.Tests.NLP
{
    public class HybridIntentRecognizerTests
    {
        private readonly HybridIntentRecognizer _sut;
        private readonly Mock<IIntentRecognizer> _mockLlmRecognizer;
        private readonly Mock<IIntentRecognizer> _mockRuleRecognizer;
        private readonly TestIntentKeywordProvider _keywordProvider;

        public HybridIntentRecognizerTests()
        {
            _mockLlmRecognizer = new Mock<IIntentRecognizer>();
            _mockRuleRecognizer = new Mock<IIntentRecognizer>();
            _keywordProvider = new TestIntentKeywordProvider();

            _sut = new HybridIntentRecognizer(
                _mockLlmRecognizer.Object,
                _mockRuleRecognizer.Object,
                NullLogger<HybridIntentRecognizer>.Instance);
        }

        [Fact]
        public async Task RecognizeAsync_WhenLlmConfidenceHigh_ShouldUseLlmResult()
        {
            // Arrange
            var userInput = "打开客厅的灯";
            var llmResult = new IntentRecognitionResult
            {
                PrimaryIntent = "ControlLight",
                Confidence = 0.9,
                OriginalInput = userInput
            };

            _mockLlm.Setup(x => x.RecognizeAsync(userInput, default))
                .ReturnsAsync(llmResult);

            // Act
            var result = await _sut.RecognizeAsync(userInput);

            // Assert
            result.Should().NotBeNull();
            result.PrimaryIntent.Should().Be("ControlLight");
            result.Confidence.Should().Be(0.9);
            result.Tags.Should().ContainKey("recognition_method");
            result.Tags["recognition_method"].Should().Be("llm");
            _mockRuleRecognizer.Verify(x => x.RecognizeAsync(It.IsAny<string>(), default), Times.Never);
        }

        [Fact]
        public async Task RecognizeAsync_WhenLlmConfidenceLow_ShouldUseRuleResult()
        {
            // Arrange
            var userInput = "打开客厅的灯";
            var llmResult = new IntentRecognitionResult
            {
                PrimaryIntent = "Unknown",
                Confidence = 0.5,
                OriginalInput = userInput
            };
            var ruleResult = new IntentRecognitionResult
            {
                PrimaryIntent = "ControlLight",
                Confidence = 0.8,
                OriginalInput = userInput
            };

            _mockLlm.Setup(x => x.RecognizeAsync(userInput, default))
                .ReturnsAsync(llmResult);
            _mockRuleRecognizer.Setup(x => x.RecognizeAsync(userInput, default))
                .ReturnsAsync(ruleResult);

            // Act
            var result = await _sut.RecognizeAsync(userInput);

            // Assert
            result.Should().NotBeNull();
            result.PrimaryIntent.Should().Be("ControlLight");
            result.Confidence.Should().Be(0.8);
            result.Tags["recognition_method"].Should().Be("rule_fallback");
            result.Tags["llm_confidence"].Should().Be("0.50");
        }

        [Fact]
        public async Task RecognizeAsync_WhenLlmFails_ShouldFallbackToRule()
        {
            // Arrange
            var userInput = "打开客厅的灯";
            var ruleResult = new IntentRecognitionResult
            {
                PrimaryIntent = "ControlLight",
                Confidence = 0.8,
                OriginalInput = userInput
            };

            _mockLlm.Setup(x => x.RecognizeAsync(userInput, default))
                .ThrowsAsync(new Exception("LLM service unavailable"));
            _mockRuleRecognizer.Setup(x => x.RecognizeAsync(userInput, default))
                .ReturnsAsync(ruleResult);

            // Act
            var result = await _sut.RecognizeAsync(userInput);

            // Assert
            result.Should().NotBeNull();
            result.PrimaryIntent.Should().Be("ControlLight");
            result.Tags["recognition_method"].Should().Be("rule_fallback");
            result.Tags["llm_error"].Should().Be("failed");
        }
    }
}
