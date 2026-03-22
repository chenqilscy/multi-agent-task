using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Core.Agents;
using CKY.MultiAgentFramework.Core.Models.LLM;
using CKY.MultiAgentFramework.Services.NLP;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CKY.MultiAgentFramework.Tests.Services.NLP;

public class IntentDrivenEntityExtractorTests
{
    private readonly Mock<IIntentRecognizer> _recognizer = new();
    private readonly Mock<IIntentProviderMapping> _mapping = new();
    private readonly Mock<IMafAiAgentRegistry> _llmRegistry = new();
    private readonly Mock<IServiceProvider> _serviceProvider = new();
    private readonly Mock<ILogger<IntentDrivenEntityExtractor>> _logger = new();

    private IntentDrivenEntityExtractor CreateSut() =>
        new(_recognizer.Object, _mapping.Object, _llmRegistry.Object, _serviceProvider.Object, _logger.Object);

    [Fact]
    public void Constructor_NullArgs_ShouldThrow()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new IntentDrivenEntityExtractor(null!, _mapping.Object, _llmRegistry.Object, _serviceProvider.Object, _logger.Object));
        Assert.Throws<ArgumentNullException>(() =>
            new IntentDrivenEntityExtractor(_recognizer.Object, null!, _llmRegistry.Object, _serviceProvider.Object, _logger.Object));
        Assert.Throws<ArgumentNullException>(() =>
            new IntentDrivenEntityExtractor(_recognizer.Object, _mapping.Object, null!, _serviceProvider.Object, _logger.Object));
        Assert.Throws<ArgumentNullException>(() =>
            new IntentDrivenEntityExtractor(_recognizer.Object, _mapping.Object, _llmRegistry.Object, null!, _logger.Object));
        Assert.Throws<ArgumentNullException>(() =>
            new IntentDrivenEntityExtractor(_recognizer.Object, _mapping.Object, _llmRegistry.Object, _serviceProvider.Object, null!));
    }

    [Fact]
    public async Task ExtractAsync_NoProviderFound_ShouldReturnEmpty()
    {
        _recognizer.Setup(x => x.RecognizeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IntentRecognitionResult { PrimaryIntent = "Unknown" });
        _mapping.Setup(x => x.GetProviderType("Unknown")).Returns((Type?)null);

        var sut = CreateSut();
        var result = await sut.ExtractAsync("hello");

        result.Should().NotBeNull();
        result.Entities.Should().BeEmpty();
    }

    [Fact]
    public async Task ExtractAsync_ProviderNotInDI_ShouldReturnEmpty()
    {
        _recognizer.Setup(x => x.RecognizeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IntentRecognitionResult { PrimaryIntent = "ControlLight" });
        _mapping.Setup(x => x.GetProviderType("ControlLight")).Returns(typeof(IEntityPatternProvider));
        _serviceProvider.Setup(x => x.GetService(typeof(IEntityPatternProvider))).Returns(null!);

        var sut = CreateSut();
        var result = await sut.ExtractAsync("turn on lights");

        result.Entities.Should().BeEmpty();
    }

    [Fact]
    public async Task ExtractAsync_KeywordOnly_ShortInput_ShouldReturnKeywordResult()
    {
        // Short input (<= 20 chars), high coverage, no vague words → keyword only
        var provider = new Mock<IEntityPatternProvider>();
        provider.Setup(x => x.GetSupportedEntityTypes()).Returns(new[] { "device" });
        provider.Setup(x => x.GetPatterns("device")).Returns(new string?[] { "灯" });

        _recognizer.Setup(x => x.RecognizeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IntentRecognitionResult { PrimaryIntent = "ControlLight" });
        _mapping.Setup(x => x.GetProviderType("ControlLight")).Returns(typeof(IEntityPatternProvider));
        _serviceProvider.Setup(x => x.GetService(typeof(IEntityPatternProvider))).Returns(provider.Object);

        var sut = CreateSut();
        var result = await sut.ExtractAsync("打开灯");

        result.Entities.Should().ContainKey("device");
        result.Entities["device"].Should().Be("灯");
    }

    [Fact]
    public async Task ExtractAsync_KeywordMatch_ShouldReturnEntityWithPosition()
    {
        var provider = new Mock<IEntityPatternProvider>();
        provider.Setup(x => x.GetSupportedEntityTypes()).Returns(new[] { "device", "action" });
        provider.Setup(x => x.GetPatterns("device")).Returns(new string?[] { "灯" });
        provider.Setup(x => x.GetPatterns("action")).Returns(new string?[] { "打开" });

        _recognizer.Setup(x => x.RecognizeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IntentRecognitionResult { PrimaryIntent = "ControlLight" });
        _mapping.Setup(x => x.GetProviderType("ControlLight")).Returns(typeof(IEntityPatternProvider));
        _serviceProvider.Setup(x => x.GetService(typeof(IEntityPatternProvider))).Returns(provider.Object);

        var sut = CreateSut();
        var result = await sut.ExtractAsync("打开灯");

        result.ExtractedEntities.Should().HaveCount(2);
        var deviceEntity = result.ExtractedEntities.First(e => e.EntityType == "device");
        deviceEntity.Confidence.Should().Be(0.9);
        deviceEntity.StartPosition.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task ExtractAsync_NullPattern_ShouldSkip()
    {
        var provider = new Mock<IEntityPatternProvider>();
        provider.Setup(x => x.GetSupportedEntityTypes()).Returns(new[] { "device" });
        provider.Setup(x => x.GetPatterns("device")).Returns(new string?[] { null, "灯" });

        _recognizer.Setup(x => x.RecognizeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IntentRecognitionResult { PrimaryIntent = "ControlLight" });
        _mapping.Setup(x => x.GetProviderType("ControlLight")).Returns(typeof(IEntityPatternProvider));
        _serviceProvider.Setup(x => x.GetService(typeof(IEntityPatternProvider))).Returns(provider.Object);

        var sut = CreateSut();
        var result = await sut.ExtractAsync("打开灯");

        result.Entities.Should().ContainKey("device");
    }

    [Fact]
    public async Task ExtractAsync_VagueWords_ShouldTriggerLlm_ButFallbackOnError()
    {
        // Input contains vague word "那个" → LLM should be triggered, but if LLM fails falls back to keyword
        var provider = new Mock<IEntityPatternProvider>();
        provider.Setup(x => x.GetSupportedEntityTypes()).Returns(new[] { "device" });
        provider.Setup(x => x.GetPatterns("device")).Returns(new string?[] { "灯" });
        provider.Setup(x => x.GetFewShotExamples()).Returns("example");

        _recognizer.Setup(x => x.RecognizeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IntentRecognitionResult { PrimaryIntent = "ControlLight" });
        _mapping.Setup(x => x.GetProviderType("ControlLight")).Returns(typeof(IEntityPatternProvider));
        _serviceProvider.Setup(x => x.GetService(typeof(IEntityPatternProvider))).Returns(provider.Object);

        // LLM will throw (no agent configured) → fall back to keyword result
        _llmRegistry.Setup(x => x.GetBestAgentAsync(LlmScenario.Intent, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("No LLM agent"));

        var sut = CreateSut();
        var result = await sut.ExtractAsync("请打开那个灯");

        // Should still have keyword result as fallback
        result.Entities.Should().ContainKey("device");
    }
}
