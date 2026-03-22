using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Demos.SmartHome;
using CKY.MultiAgentFramework.Services.NLP;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace CKY.MultiAgentFramework.IntegrationTests.NLP;

/// <summary>
/// NLP管道集成测试
/// 验证 RuleBasedIntentRecognizer → HybridIntentRecognizer 的完整识别链路
/// </summary>
public class NlpPipelineIntegrationTests
{
    private readonly SmartHomeIntentKeywordProvider _keywordProvider;
    private readonly RuleBasedIntentRecognizer _ruleRecognizer;

    public NlpPipelineIntegrationTests()
    {
        _keywordProvider = new SmartHomeIntentKeywordProvider();
        _ruleRecognizer = new RuleBasedIntentRecognizer(
            _keywordProvider,
            NullLogger<RuleBasedIntentRecognizer>.Instance);
    }

    // ========================================
    // RuleBasedIntentRecognizer 集成测试
    // ========================================

    [Theory]
    [InlineData("打开客厅的灯", "ControlLight")]
    [InlineData("把温度调到26度", "AdjustClimate")]
    [InlineData("播放轻音乐", "PlayMusic")]
    [InlineData("锁门", "SecurityControl")]
    [InlineData("今天北京天气预报", "QueryWeather")]
    [InlineData("开启睡眠模式", "SleepMode")]
    [InlineData("开启会客模式", "GuestMode")]
    [InlineData("阅读模式", "ReadingMode")]
    public async Task RuleRecognizer_WithSmartHomeKeywords_ShouldRecognizeCorrectIntent(
        string input, string expectedIntent)
    {
        var result = await _ruleRecognizer.RecognizeAsync(input);

        result.PrimaryIntent.Should().Be(expectedIntent);
        result.Confidence.Should().BeGreaterThan(0);
        result.OriginalInput.Should().Be(input);
    }

    [Fact]
    public async Task RuleRecognizer_UnknownInput_ShouldReturnUnknown()
    {
        var result = await _ruleRecognizer.RecognizeAsync("吃饭去哪里好呢");

        result.PrimaryIntent.Should().Be("Unknown");
        result.Confidence.Should().Be(0.0);
    }

    [Fact]
    public async Task RuleRecognizer_BatchRecognize_ShouldReturnAllResults()
    {
        var inputs = new List<string> { "开灯", "播放歌曲", "锁门", "随便聊聊吧" };

        var results = await _ruleRecognizer.RecognizeBatchAsync(inputs);

        results.Should().HaveCount(4);
        results[0].PrimaryIntent.Should().Be("ControlLight");
        results[1].PrimaryIntent.Should().Be("PlayMusic");
        results[2].PrimaryIntent.Should().Be("SecurityControl");
        results[3].PrimaryIntent.Should().Be("Unknown");
    }

    [Fact]
    public async Task RuleRecognizer_MultipleKeywordMatch_ShouldReturnHighestScore()
    {
        // "灯" 和 "开灯" 同时命中 ControlLight → 更高分
        var result = await _ruleRecognizer.RecognizeAsync("开灯开灯");

        result.PrimaryIntent.Should().Be("ControlLight");
        result.Confidence.Should().BeGreaterThan(0);
    }

    // ========================================
    // HybridIntentRecognizer 集成测试
    // ========================================

    [Fact]
    public async Task HybridRecognizer_WhenLlmConfidenceHigh_ShouldUseLlmResult()
    {
        var mockLlm = new Mock<IIntentRecognizer>();
        mockLlm.Setup(r => r.RecognizeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IntentRecognitionResult
            {
                PrimaryIntent = "ControlLight",
                Confidence = 0.95,
                OriginalInput = "打开灯"
            });

        var hybrid = new HybridIntentRecognizer(
            mockLlm.Object,
            _ruleRecognizer,
            NullLogger<HybridIntentRecognizer>.Instance)
        {
            ConfidenceThreshold = 0.7
        };

        var result = await hybrid.RecognizeAsync("打开灯");

        result.PrimaryIntent.Should().Be("ControlLight");
        result.Confidence.Should().Be(0.95);
        result.Tags.Should().Contain("recognition_method:llm");
    }

    [Fact]
    public async Task HybridRecognizer_WhenLlmConfidenceLow_ShouldFallbackToRule()
    {
        var mockLlm = new Mock<IIntentRecognizer>();
        mockLlm.Setup(r => r.RecognizeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IntentRecognitionResult
            {
                PrimaryIntent = "Unknown",
                Confidence = 0.1,
                OriginalInput = "开灯关灯"
            });

        var hybrid = new HybridIntentRecognizer(
            mockLlm.Object,
            _ruleRecognizer,
            NullLogger<HybridIntentRecognizer>.Instance)
        {
            ConfidenceThreshold = 0.7
        };

        var result = await hybrid.RecognizeAsync("开灯关灯");

        // Rule-based "开灯关灯" matches ControlLight keywords "开灯" and "关灯" (2/6 ≈ 0.33 > 0.1)
        result.PrimaryIntent.Should().Be("ControlLight");
    }

    [Fact]
    public async Task HybridRecognizer_WhenLlmThrows_ShouldFallbackToRule()
    {
        var mockLlm = new Mock<IIntentRecognizer>();
        mockLlm.Setup(r => r.RecognizeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("LLM service unavailable"));

        var hybrid = new HybridIntentRecognizer(
            mockLlm.Object,
            _ruleRecognizer,
            NullLogger<HybridIntentRecognizer>.Instance);

        var result = await hybrid.RecognizeAsync("播放音乐");

        // HybridIntentRecognizer catches LLM exceptions and falls back to rule
        result.PrimaryIntent.Should().Be("PlayMusic");
    }

    // ========================================
    // KeywordProvider 集成测试
    // ========================================

    [Fact]
    public void KeywordProvider_AllIntentsHaveKeywords()
    {
        var intents = _keywordProvider.GetSupportedIntents().ToList();

        intents.Should().NotBeEmpty();
        intents.Should().HaveCountGreaterThanOrEqualTo(10);

        foreach (var intent in intents)
        {
            var keywords = _keywordProvider.GetKeywords(intent);
            keywords.Should().NotBeNull($"intent '{intent}' should have keywords");
            keywords.Should().NotBeEmpty($"intent '{intent}' should have at least one keyword");
        }
    }

    [Fact]
    public void KeywordProvider_CaseInsensitiveLookup()
    {
        var lower = _keywordProvider.GetKeywords("controllight");
        var upper = _keywordProvider.GetKeywords("CONTROLLIGHT");
        var mixed = _keywordProvider.GetKeywords("ControlLight");

        lower.Should().BeEquivalentTo(mixed);
        upper.Should().BeEquivalentTo(mixed);
    }
}
