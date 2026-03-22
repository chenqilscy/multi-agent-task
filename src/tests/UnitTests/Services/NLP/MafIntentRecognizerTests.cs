using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Services.NLP;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace CKY.MAF.Tests.Services.NLP;

/// <summary>
/// MafIntentRecognizer 测试 — 验证委托到 HybridIntentRecognizer
/// </summary>
public class MafIntentRecognizerTests
{
    private readonly Mock<ILogger<MafIntentRecognizer>> _loggerMock = new();

    [Fact]
    public void Constructor_NullRecognizer_Throws()
    {
        var act = () => new MafIntentRecognizer(null!, _loggerMock.Object);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_NullLogger_Throws()
    {
        var hybrid = new HybridIntentRecognizer(
            Mock.Of<IIntentRecognizer>(),
            Mock.Of<IIntentRecognizer>(),
            Mock.Of<ILogger<HybridIntentRecognizer>>());
        var act = () => new MafIntentRecognizer(hybrid, null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task RecognizeAsync_DelegatesToPrimary()
    {
        // Use a real HybridIntentRecognizer with mocked LLM recognizer returning high confidence
        var llmMock = new Mock<IIntentRecognizer>();
        llmMock.Setup(x => x.RecognizeAsync("查天气", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IntentRecognitionResult
            {
                PrimaryIntent = "weather",
                Confidence = 0.95
            });

        var ruleMock = new Mock<IIntentRecognizer>();
        var hybrid = new HybridIntentRecognizer(
            llmMock.Object,
            ruleMock.Object,
            Mock.Of<ILogger<HybridIntentRecognizer>>());

        var sut = new MafIntentRecognizer(hybrid, _loggerMock.Object);
        var result = await sut.RecognizeAsync("查天气");

        result.PrimaryIntent.Should().Be("weather");
        result.Confidence.Should().Be(0.95);
    }

    [Fact]
    public async Task RecognizeBatchAsync_DelegatesToPrimary()
    {
        var inputs = new List<string> { "查天气", "开灯" };

        // HybridIntentRecognizer.RecognizeBatchAsync calls RecognizeAsync for each input
        var llmMock = new Mock<IIntentRecognizer>();
        llmMock.Setup(x => x.RecognizeAsync("查天气", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IntentRecognitionResult { PrimaryIntent = "weather", Confidence = 0.9 });
        llmMock.Setup(x => x.RecognizeAsync("开灯", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IntentRecognitionResult { PrimaryIntent = "light_control", Confidence = 0.9 });

        var ruleMock = new Mock<IIntentRecognizer>();
        var hybrid = new HybridIntentRecognizer(
            llmMock.Object,
            ruleMock.Object,
            Mock.Of<ILogger<HybridIntentRecognizer>>());

        var sut = new MafIntentRecognizer(hybrid, _loggerMock.Object);
        var results = await sut.RecognizeBatchAsync(inputs);

        results.Should().HaveCount(2);
        results[0].PrimaryIntent.Should().Be("weather");
        results[1].PrimaryIntent.Should().Be("light_control");
    }
}
