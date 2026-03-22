using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Core.Models.Task;
using CKY.MultiAgentFramework.Services.NLP;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CKY.MultiAgentFramework.Tests.Services.NLP;

public class HybridIntentRecognizerTests
{
    private readonly Mock<IIntentRecognizer> _llmRecognizer;
    private readonly Mock<IIntentRecognizer> _ruleRecognizer;
    private readonly Mock<ILogger<HybridIntentRecognizer>> _logger;

    public HybridIntentRecognizerTests()
    {
        _llmRecognizer = new Mock<IIntentRecognizer>();
        _ruleRecognizer = new Mock<IIntentRecognizer>();
        _logger = new Mock<ILogger<HybridIntentRecognizer>>();
    }

    private HybridIntentRecognizer CreateSut(double threshold = 0.7)
    {
        return new HybridIntentRecognizer(_llmRecognizer.Object, _ruleRecognizer.Object, _logger.Object)
        {
            ConfidenceThreshold = threshold
        };
    }

    [Fact]
    public void Constructor_NullArgs_ShouldThrow()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new HybridIntentRecognizer(null!, _ruleRecognizer.Object, _logger.Object));
        Assert.Throws<ArgumentNullException>(() =>
            new HybridIntentRecognizer(_llmRecognizer.Object, null!, _logger.Object));
        Assert.Throws<ArgumentNullException>(() =>
            new HybridIntentRecognizer(_llmRecognizer.Object, _ruleRecognizer.Object, null!));
    }

    [Fact]
    public async Task RecognizeAsync_HighConfidenceLlm_ShouldReturnLlmResult()
    {
        var llmResult = new IntentRecognitionResult
        {
            PrimaryIntent = "control_light",
            Confidence = 0.95,
            Tags = new List<string>()
        };
        _llmRecognizer.Setup(x => x.RecognizeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(llmResult);

        var sut = CreateSut();
        var result = await sut.RecognizeAsync("打开客厅的灯");

        result.PrimaryIntent.Should().Be("control_light");
        result.Confidence.Should().Be(0.95);
        result.Tags.Should().Contain("recognition_method:llm");
        _ruleRecognizer.Verify(x => x.RecognizeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RecognizeAsync_LowLlmConfidence_HigherRule_ShouldReturnRuleResult()
    {
        var llmResult = new IntentRecognitionResult
        {
            PrimaryIntent = "control_light",
            Confidence = 0.4,
            Tags = new List<string>()
        };
        var ruleResult = new IntentRecognitionResult
        {
            PrimaryIntent = "control_ac",
            Confidence = 0.8,
            Tags = new List<string>()
        };
        _llmRecognizer.Setup(x => x.RecognizeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(llmResult);
        _ruleRecognizer.Setup(x => x.RecognizeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ruleResult);

        var sut = CreateSut();
        var result = await sut.RecognizeAsync("调节空调温度");

        result.PrimaryIntent.Should().Be("control_ac");
        result.Tags.Should().Contain("recognition_method:rule_fallback");
    }

    [Fact]
    public async Task RecognizeAsync_LowLlmConfidence_LowerRule_ShouldReturnLlmResult()
    {
        var llmResult = new IntentRecognitionResult
        {
            PrimaryIntent = "query_weather",
            Confidence = 0.5,
            Tags = new List<string>(),
            AlternativeIntents = new Dictionary<string, double>()
        };
        var ruleResult = new IntentRecognitionResult
        {
            PrimaryIntent = "Unknown",
            Confidence = 0.3,
            Tags = new List<string>()
        };
        _llmRecognizer.Setup(x => x.RecognizeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(llmResult);
        _ruleRecognizer.Setup(x => x.RecognizeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ruleResult);

        var sut = CreateSut();
        var result = await sut.RecognizeAsync("今天天气怎样");

        result.PrimaryIntent.Should().Be("query_weather");
        result.Tags.Should().Contain("recognition_method:llm_low_confidence");
    }

    [Fact]
    public async Task RecognizeAsync_LlmFails_ShouldFallbackToRule()
    {
        _llmRecognizer.Setup(x => x.RecognizeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("LLM failed"));
        var ruleResult = new IntentRecognitionResult
        {
            PrimaryIntent = "control_light",
            Confidence = 0.85,
            Tags = new List<string>()
        };
        _ruleRecognizer.Setup(x => x.RecognizeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ruleResult);

        var sut = CreateSut();
        var result = await sut.RecognizeAsync("开灯");

        result.PrimaryIntent.Should().Be("control_light");
        result.Tags.Should().Contain("recognition_method:rule_fallback");
        result.Tags.Should().Contain("llm_error:failed");
    }

    [Fact]
    public async Task RecognizeBatchAsync_ShouldProcessAllInputs()
    {
        _llmRecognizer.Setup(x => x.RecognizeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IntentRecognitionResult
            {
                PrimaryIntent = "test",
                Confidence = 0.9,
                Tags = new List<string>()
            });

        var sut = CreateSut();
        var results = await sut.RecognizeBatchAsync(new List<string> { "a", "b", "c" });

        results.Should().HaveCount(3);
        results.Should().AllSatisfy(r => r.PrimaryIntent.Should().Be("test"));
    }

    [Fact]
    public async Task RecognizeAsync_LowLlm_RuleReturnsRealIntent_ShouldAddAlternative()
    {
        var llmResult = new IntentRecognitionResult
        {
            PrimaryIntent = "control_light",
            Confidence = 0.5,
            Tags = new List<string>(),
            AlternativeIntents = new Dictionary<string, double>()
        };
        var ruleResult = new IntentRecognitionResult
        {
            PrimaryIntent = "control_ac",
            Confidence = 0.3,
            Tags = new List<string>()
        };
        _llmRecognizer.Setup(x => x.RecognizeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(llmResult);
        _ruleRecognizer.Setup(x => x.RecognizeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ruleResult);

        var sut = CreateSut();
        var result = await sut.RecognizeAsync("test input");

        result.AlternativeIntents.Should().ContainKey("control_ac");
    }
}
