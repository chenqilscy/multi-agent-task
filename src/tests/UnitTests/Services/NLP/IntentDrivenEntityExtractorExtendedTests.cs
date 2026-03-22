using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Core.Models.LLM;
using CKY.MultiAgentFramework.Services.NLP;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace CKY.MAF.Tests.Services.NLP;

/// <summary>
/// IntentDrivenEntityExtractor 扩展测试 — 覆盖 ShouldEnableLlm、MergeResults、ParseLlmResponse 等路径
/// </summary>
public class IntentDrivenEntityExtractorExtendedTests
{
    private readonly Mock<IIntentRecognizer> _recognizerMock = new();
    private readonly Mock<IIntentProviderMapping> _mappingMock = new();
    private readonly Mock<IMafAiAgentRegistry> _registryMock = new();
    private readonly Mock<IServiceProvider> _spMock = new();
    private readonly Mock<ILogger<IntentDrivenEntityExtractor>> _loggerMock = new();

    private IntentDrivenEntityExtractor CreateSut() =>
        new(_recognizerMock.Object, _mappingMock.Object, _registryMock.Object, _spMock.Object, _loggerMock.Object);

    [Fact]
    public void Constructor_NullIntentRecognizer_Throws()
    {
        var act = () => new IntentDrivenEntityExtractor(null!, _mappingMock.Object, _registryMock.Object, _spMock.Object, _loggerMock.Object);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_NullMapping_Throws()
    {
        var act = () => new IntentDrivenEntityExtractor(_recognizerMock.Object, null!, _registryMock.Object, _spMock.Object, _loggerMock.Object);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_NullRegistry_Throws()
    {
        var act = () => new IntentDrivenEntityExtractor(_recognizerMock.Object, _mappingMock.Object, null!, _spMock.Object, _loggerMock.Object);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_NullServiceProvider_Throws()
    {
        var act = () => new IntentDrivenEntityExtractor(_recognizerMock.Object, _mappingMock.Object, _registryMock.Object, null!, _loggerMock.Object);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_NullLogger_Throws()
    {
        var act = () => new IntentDrivenEntityExtractor(_recognizerMock.Object, _mappingMock.Object, _registryMock.Object, _spMock.Object, null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task ExtractAsync_NoProviderFound_ReturnsEmpty()
    {
        // Intent recognizer returns a result, but no provider mapping
        _recognizerMock.Setup(x => x.RecognizeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IntentRecognitionResult { PrimaryIntent = "unknown", Confidence = 0.9 });
        _mappingMock.Setup(x => x.GetProviderType("unknown")).Returns((Type?)null);

        var sut = CreateSut();
        var result = await sut.ExtractAsync("some input");

        result.Entities.Should().BeEmpty();
        result.ExtractedEntities.Should().BeEmpty();
    }

    [Fact]
    public async Task ExtractAsync_ProviderTypeNotInDI_ReturnsEmpty()
    {
        _recognizerMock.Setup(x => x.RecognizeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IntentRecognitionResult { PrimaryIntent = "light", Confidence = 0.9 });
        _mappingMock.Setup(x => x.GetProviderType("light")).Returns(typeof(IEntityPatternProvider));
        _spMock.Setup(x => x.GetService(typeof(IEntityPatternProvider))).Returns(null);

        var sut = CreateSut();
        var result = await sut.ExtractAsync("开灯");

        result.Entities.Should().BeEmpty();
    }

    [Fact]
    public async Task ExtractAsync_ShortInputWithHighCoverage_ReturnsKeywordOnly()
    {
        // Short input (<= 20 chars), high coverage (>= 40%), no vague words -> ShouldEnableLlm = false
        var providerMock = new Mock<IEntityPatternProvider>();
        providerMock.Setup(x => x.GetSupportedEntityTypes()).Returns(new[] { "device" });
        providerMock.Setup(x => x.GetPatterns("device")).Returns(new string?[] { "灯" });

        _recognizerMock.Setup(x => x.RecognizeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IntentRecognitionResult { PrimaryIntent = "light", Confidence = 0.9 });
        _mappingMock.Setup(x => x.GetProviderType("light")).Returns(typeof(IEntityPatternProvider));
        _spMock.Setup(x => x.GetService(typeof(IEntityPatternProvider))).Returns(providerMock.Object);

        var sut = CreateSut();
        var result = await sut.ExtractAsync("开灯");

        result.Entities.Should().ContainKey("device");
        result.Entities["device"].Should().Be("灯");
    }

    [Fact]
    public async Task ExtractAsync_LongInputTriggersLlm_CircuitBreakerOpen_ReturnsKeyword()
    {
        // Long input triggers LLM, but circuit breaker is open (after 3 failures)
        var providerMock = new Mock<IEntityPatternProvider>();
        providerMock.Setup(x => x.GetSupportedEntityTypes()).Returns(new[] { "device", "action", "location" });
        providerMock.Setup(x => x.GetPatterns(It.IsAny<string>())).Returns(new string?[] { });
        providerMock.Setup(x => x.GetFewShotExamples()).Returns("example");

        _recognizerMock.Setup(x => x.RecognizeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IntentRecognitionResult { PrimaryIntent = "light", Confidence = 0.9 });
        _mappingMock.Setup(x => x.GetProviderType("light")).Returns(typeof(IEntityPatternProvider));
        _spMock.Setup(x => x.GetService(typeof(IEntityPatternProvider))).Returns(providerMock.Object);
        _registryMock.Setup(x => x.GetBestAgentAsync(It.IsAny<LlmScenario>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("No agent"));

        var sut = CreateSut();
        var longInput = "请帮我把客厅那边所有的灯都打开，我需要全部打开";

        // First 3 calls will fail and open the circuit breaker
        // On 4th call, circuit breaker should be open and return keyword result
        for (int i = 0; i < 4; i++)
        {
            var result = await sut.ExtractAsync(longInput);
            result.Should().NotBeNull();
        }
    }

    [Fact]
    public async Task ExtractAsync_VagueWordsInput_TriggersLlm()
    {
        // Input contains vague words like "那边", "所有" etc.
        var providerMock = new Mock<IEntityPatternProvider>();
        providerMock.Setup(x => x.GetSupportedEntityTypes()).Returns(new[] { "device" });
        providerMock.Setup(x => x.GetPatterns("device")).Returns(new string?[] { "灯" });
        providerMock.Setup(x => x.GetFewShotExamples()).Returns("example");

        _recognizerMock.Setup(x => x.RecognizeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IntentRecognitionResult { PrimaryIntent = "light", Confidence = 0.9 });
        _mappingMock.Setup(x => x.GetProviderType("light")).Returns(typeof(IEntityPatternProvider));
        _spMock.Setup(x => x.GetService(typeof(IEntityPatternProvider))).Returns(providerMock.Object);
        _registryMock.Setup(x => x.GetBestAgentAsync(It.IsAny<LlmScenario>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("No agent"));

        var sut = CreateSut();
        // "那边" is a vague word -> triggers LLM path (which will fail and fall back)
        var result = await sut.ExtractAsync("那边灯");

        result.Entities.Should().ContainKey("device");
    }

    [Fact]
    public async Task ExtractAsync_KeywordExtraction_MultiplePatterns()
    {
        var providerMock = new Mock<IEntityPatternProvider>();
        providerMock.Setup(x => x.GetSupportedEntityTypes()).Returns(new[] { "device", "action" });
        providerMock.Setup(x => x.GetPatterns("device")).Returns(new string?[] { "灯", "空调" });
        providerMock.Setup(x => x.GetPatterns("action")).Returns(new string?[] { "打开", null, "" });

        _recognizerMock.Setup(x => x.RecognizeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IntentRecognitionResult { PrimaryIntent = "control", Confidence = 0.9 });
        _mappingMock.Setup(x => x.GetProviderType("control")).Returns(typeof(IEntityPatternProvider));
        _spMock.Setup(x => x.GetService(typeof(IEntityPatternProvider))).Returns(providerMock.Object);

        var sut = CreateSut();
        var result = await sut.ExtractAsync("打开灯");

        result.Entities.Should().ContainKey("device");
        result.Entities.Should().ContainKey("action");
        result.ExtractedEntities.Should().HaveCountGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task ExtractAsync_NullPatterns_Skipped()
    {
        var providerMock = new Mock<IEntityPatternProvider>();
        providerMock.Setup(x => x.GetSupportedEntityTypes()).Returns(new[] { "device" });
        providerMock.Setup(x => x.GetPatterns("device")).Returns((string?[]?)null);

        _recognizerMock.Setup(x => x.RecognizeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IntentRecognitionResult { PrimaryIntent = "light", Confidence = 0.9 });
        _mappingMock.Setup(x => x.GetProviderType("light")).Returns(typeof(IEntityPatternProvider));
        _spMock.Setup(x => x.GetService(typeof(IEntityPatternProvider))).Returns(providerMock.Object);

        var sut = CreateSut();
        var result = await sut.ExtractAsync("开灯");

        result.Entities.Should().BeEmpty();
    }
}
