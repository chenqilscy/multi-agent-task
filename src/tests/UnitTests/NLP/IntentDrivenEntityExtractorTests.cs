using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Services.NLP;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CKY.MultiAgentFramework.Tests.NLP
{
    public class IntentDrivenEntityExtractorTests
    {
        [Fact]
        public async Task ExtractAsync_WhenProviderNotFound_ShouldReturnEmptyResult()
        {
            // Arrange
            var mockIntentRecognizer = new Mock<IIntentRecognizer>();
            var mockMapping = new Mock<IIntentProviderMapping>();
            var mockLlmService = new Mock<ILlmService>();
            var mockServiceProvider = new Mock<IServiceProvider>();
            var mockLogger = new Mock<ILogger<IntentDrivenEntityExtractor>>();

            mockIntentRecognizer
                .Setup(x => x.RecognizeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new IntentRecognitionResult { PrimaryIntent = "UnknownIntent" });

            mockMapping
                .Setup(x => x.GetProviderType("UnknownIntent"))
                .Returns((Type?)null);

            var extractor = new IntentDrivenEntityExtractor(
                mockIntentRecognizer.Object,
                mockMapping.Object,
                mockLlmService.Object,
                mockServiceProvider.Object,
                mockLogger.Object);

            // Act
            var result = await extractor.ExtractAsync("test input");

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Entities);
            Assert.Empty(result.ExtractedEntities);
        }

        [Fact]
        public async Task ExtractAsync_WithValidProvider_ShouldExtractKeywords()
        {
            // Arrange
            var mockIntentRecognizer = new Mock<IIntentRecognizer>();
            var mockMapping = new Mock<IIntentProviderMapping>();
            var mockLlmService = new Mock<ILlmService>();
            var mockServiceProvider = new Mock<IServiceProvider>();
            var mockProvider = new Mock<IEntityPatternProvider>();
            var mockLogger = new Mock<ILogger<IntentDrivenEntityExtractor>>();

            mockIntentRecognizer
                .Setup(x => x.RecognizeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new IntentRecognitionResult { PrimaryIntent = "ControlLight" });

            mockMapping
                .Setup(x => x.GetProviderType("ControlLight"))
                .Returns(typeof(DummyLightControlEntityPatternProvider));

            mockServiceProvider
                .Setup(x => x.GetService(It.IsAny<Type>()))
                .Returns(mockProvider.Object);

            mockProvider
                .Setup(x => x.GetSupportedEntityTypes())
                .Returns(new[] { "Room", "Device", "Action" });

            mockProvider
                .Setup(x => x.GetPatterns("Room"))
                .Returns(new[] { "客厅" });
            mockProvider
                .Setup(x => x.GetPatterns("Device"))
                .Returns(new[] { "灯" });
            mockProvider
                .Setup(x => x.GetPatterns("Action"))
                .Returns(new[] { "打开" });

            var extractor = new IntentDrivenEntityExtractor(
                mockIntentRecognizer.Object,
                mockMapping.Object,
                mockLlmService.Object,
                mockServiceProvider.Object,
                mockLogger.Object);

            // Act
            var result = await extractor.ExtractAsync("打开客厅的灯");

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Entities.Count > 0);
        }

        [Fact]
        public async Task ExtractAsync_WithLongInput_ShouldTriggerLlm()
        {
            // Arrange
            var mockIntentRecognizer = new Mock<IIntentRecognizer>();
            var mockMapping = new Mock<IIntentProviderMapping>();
            var mockLlmService = new Mock<ILlmService>();
            var mockServiceProvider = new Mock<IServiceProvider>();
            var mockProvider = new Mock<IEntityPatternProvider>();
            var mockLogger = new Mock<ILogger<IntentDrivenEntityExtractor>>();

            mockIntentRecognizer
                .Setup(x => x.RecognizeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new IntentRecognitionResult { PrimaryIntent = "ControlLight" });

            mockMapping
                .Setup(x => x.GetProviderType("ControlLight"))
                .Returns(typeof(DummyLightControlEntityPatternProvider));

            mockServiceProvider
                .Setup(x => x.GetService(It.IsAny<Type>()))
                .Returns(mockProvider.Object);

            mockProvider
                .Setup(x => x.GetSupportedEntityTypes())
                .Returns(new[] { "Room", "Device", "Action", "Brightness" });

            mockProvider
                .Setup(x => x.GetPatterns("Room"))
                .Returns(new[] { "客厅" });
            mockProvider
                .Setup(x => x.GetPatterns("Device"))
                .Returns(new[] { "灯" });

            // 模拟 LLM 返回
            mockLlmService
                .Setup(x => x.CompleteAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(@"{""Room"": ""客厅"", ""Device"": ""灯"", ""Action"": ""调亮"", ""Brightness"": ""80%""}");

            var extractor = new IntentDrivenEntityExtractor(
                mockIntentRecognizer.Object,
                mockMapping.Object,
                mockLlmService.Object,
                mockServiceProvider.Object,
                mockLogger.Object);

            // Act - 长输入触发 LLM（> 20 字）
            var longInput = "帮我把客厅的灯调亮一点，设置到80%的亮度";
            var result = await extractor.ExtractAsync(longInput);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Entities.ContainsKey("Room") || result.Entities.ContainsKey("Device"));
            mockLlmService.Verify(
                x => x.CompleteAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task ExtractAsync_WhenLlmFails_ShouldFallbackToKeywords()
        {
            // Arrange
            var mockIntentRecognizer = new Mock<IIntentRecognizer>();
            var mockMapping = new Mock<IIntentProviderMapping>();
            var mockLlmService = new Mock<ILlmService>();
            var mockServiceProvider = new Mock<IServiceProvider>();
            var mockProvider = new Mock<IEntityPatternProvider>();
            var mockLogger = new Mock<ILogger<IntentDrivenEntityExtractor>>();

            mockIntentRecognizer
                .Setup(x => x.RecognizeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new IntentRecognitionResult { PrimaryIntent = "ControlLight" });

            mockMapping
                .Setup(x => x.GetProviderType("ControlLight"))
                .Returns(typeof(DummyLightControlEntityPatternProvider));

            mockServiceProvider
                .Setup(x => x.GetService(It.IsAny<Type>()))
                .Returns(mockProvider.Object);

            mockProvider
                .Setup(x => x.GetSupportedEntityTypes())
                .Returns(new[] { "Room", "Device", "Action" });

            mockProvider
                .Setup(x => x.GetPatterns("Room"))
                .Returns(new[] { "客厅" });
            mockProvider
                .Setup(x => x.GetPatterns("Device"))
                .Returns(new[] { "灯" });
            mockProvider
                .Setup(x => x.GetPatterns("Action"))
                .Returns(new[] { "打开" });

            // 模拟 LLM 失败
            mockLlmService
                .Setup(x => x.CompleteAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new HttpRequestException("LLM service unavailable"));

            var extractor = new IntentDrivenEntityExtractor(
                mockIntentRecognizer.Object,
                mockMapping.Object,
                mockLlmService.Object,
                mockServiceProvider.Object,
                mockLogger.Object);

            // Act
            var result = await extractor.ExtractAsync("帮我把客厅和卧室的灯都打开");

            // Assert - 应该降级到关键字结果
            Assert.NotNull(result);
            Assert.True(result.Entities.Count > 0, "Should have keyword extraction results even when LLM fails");
        }

        [Fact]
        public async Task ExtractAsync_WithVagueWords_ShouldTriggerLlm()
        {
            // Arrange
            var mockIntentRecognizer = new Mock<IIntentRecognizer>();
            var mockMapping = new Mock<IIntentProviderMapping>();
            var mockLlmService = new Mock<ILlmService>();
            var mockServiceProvider = new Mock<IServiceProvider>();
            var mockProvider = new Mock<IEntityPatternProvider>();
            var mockLogger = new Mock<ILogger<IntentDrivenEntityExtractor>>();

            mockIntentRecognizer
                .Setup(x => x.RecognizeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new IntentRecognitionResult { PrimaryIntent = "ControlLight" });

            mockMapping
                .Setup(x => x.GetProviderType("ControlLight"))
                .Returns(typeof(DummyLightControlEntityPatternProvider));

            mockServiceProvider
                .Setup(x => x.GetService(It.IsAny<Type>()))
                .Returns(mockProvider.Object);

            mockProvider
                .Setup(x => x.GetSupportedEntityTypes())
                .Returns(new[] { "Room", "Device", "Action" });

            mockProvider
                .Setup(x => x.GetPatterns("Action"))
                .Returns(new[] { "打开" });

            var extractor = new IntentDrivenEntityExtractor(
                mockIntentRecognizer.Object,
                mockMapping.Object,
                mockLlmService.Object,
                mockServiceProvider.Object,
                mockLogger.Object);

            // Act - 包含模糊词"那边"
            var result = await extractor.ExtractAsync("把那边的灯打开");

            // Assert - 短输入但有模糊词，应该触发 LLM
            mockLlmService.Verify(
                x => x.CompleteAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        /// <summary>
        /// Dummy provider class for testing - represents LightControlEntityPatternProvider
        /// </summary>
        private class DummyLightControlEntityPatternProvider : IEntityPatternProvider
        {
            public string?[]? GetPatterns(string entityType)
            {
                return entityType switch
                {
                    "Room" => new[] { "客厅", "卧室", "厨房" },
                    "Device" => new[] { "灯", "空调" },
                    "Action" => new[] { "打开", "关闭" },
                    _ => null
                };
            }

            public IEnumerable<string> GetSupportedEntityTypes()
            {
                return new[] { "Room", "Device", "Action" };
            }

            public string GetFewShotExamples()
            {
                return "Example few-shot content for testing";
            }
        }
    }
}
