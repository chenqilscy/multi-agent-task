using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Core.Models.LLM;
using CKY.MultiAgentFramework.Services.NLP;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace CKY.MultiAgentFramework.Tests.NLP
{
    public class LlmIntentRecognizerTests
    {
        private readonly LlmIntentRecognizer _sut;
        private readonly Mock<ILlmAgentRegistry> _mockLlmRegistry;
        private readonly Mock<ILlmAgent> _mockLlmAgent;
        private readonly TestIntentKeywordProvider _keywordProvider;

        public LlmIntentRecognizerTests()
        {
            _mockLlmRegistry = new Mock<ILlmAgentRegistry>();
            _mockLlmAgent = new Mock<ILlmAgent>();
            _keywordProvider = new TestIntentKeywordProvider();

            // Setup registry to return the mock agent
            _mockLlmRegistry
                .Setup(x => x.GetBestAgentAsync(LlmScenario.Intent, It.IsAny<CancellationToken>()))
                .ReturnsAsync(_mockLlmAgent.Object);

            // Setup default model ID
            _mockLlmAgent
                .Setup(x => x.GetCurrentModelId())
                .Returns("test-model");

            _sut = new LlmIntentRecognizer(
                _mockLlmRegistry.Object,
                _keywordProvider,
                NullLogger<LlmIntentRecognizer>.Instance);
        }

        [Fact]
        public async Task RecognizeAsync_ShouldCallLlmAndParseResponse()
        {
            // Arrange
            var userInput = "打开客厅的灯";
            var llmResponse = @"{""primary_intent"": ""ControlLight"", ""confidence"": 0.95}";

            _mockLlmAgent.Setup(x => x.ExecuteAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(llmResponse);

            // Act
            var result = await _sut.RecognizeAsync(userInput);

            // Assert
            result.Should().NotBeNull();
            result.PrimaryIntent.Should().Be("ControlLight");
            result.Confidence.Should().Be(0.95);
            result.OriginalInput.Should().Be(userInput);
            _mockLlmRegistry.Verify(x => x.GetBestAgentAsync(
                LlmScenario.Intent,
                It.IsAny<CancellationToken>()),
                Times.Once);
            _mockLlmAgent.Verify(x => x.ExecuteAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task RecognizeAsync_WhenLlmFails_ShouldThrowException()
        {
            // Arrange
            var userInput = "测试输入";
            _mockLlmAgent.Setup(x => x.ExecuteAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("LLM service unavailable"));

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => _sut.RecognizeAsync(userInput));
        }

        [Fact]
        public async Task RecognizeAsync_WhenLlmReturnsInvalidJson_ShouldReturnUnknown()
        {
            // Arrange
            var userInput = "无法理解的输入";
            var llmResponse = "这不是有效的JSON响应";

            _mockLlmAgent.Setup(x => x.ExecuteAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(llmResponse);

            // Act
            var result = await _sut.RecognizeAsync(userInput);

            // Assert
            result.Should().NotBeNull();
            result.PrimaryIntent.Should().Be("Unknown");
            result.Confidence.Should().Be(0.0);
        }
    }
}
