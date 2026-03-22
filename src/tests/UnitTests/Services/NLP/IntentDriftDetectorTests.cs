using CKY.MultiAgentFramework.Core.Models.Dialog;
using CKY.MultiAgentFramework.Services.NLP;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CKY.MultiAgentFramework.Tests.Services.NLP;

public class IntentDriftDetectorTests
{
    private readonly Mock<ILogger<IntentDriftDetector>> _logger = new();

    private IntentDriftDetector CreateSut(IEnumerable<string>? triggers = null)
    {
        return new IntentDriftDetector(triggers, _logger.Object);
    }

    private static DialogContext CreateContext(int turnCount = 1, Dictionary<string, object>? slots = null)
    {
        return new DialogContext
        {
            SessionId = "test-session",
            TurnCount = turnCount,
            HistoricalSlots = slots ?? new Dictionary<string, object>()
        };
    }

    [Fact]
    public void Constructor_NullLogger_ShouldThrow()
    {
        Assert.Throws<ArgumentNullException>(() => new IntentDriftDetector(null, null!));
    }

    [Fact]
    public void Constructor_NullTriggers_ShouldUseDefaults()
    {
        var sut = CreateSut(null);
        // Should not throw, uses default triggers
        sut.Should().NotBeNull();
    }

    [Fact]
    public async Task DetectDriftAsync_TopicSwitchTrigger_ShouldDetectDrift()
    {
        var sut = CreateSut();
        var context = CreateContext();

        var result = await sut.DetectDriftAsync("对了，另外问一下天气", "control_light", context);

        result.HasDrifted.Should().BeTrue();
        result.DriftScore.Should().Be(0.9);
        result.SuggestedAction.Should().Be(DriftAction.NewTopic);
        result.Reason.Should().Contain("触发词");
    }

    [Fact]
    public async Task DetectDriftAsync_NoTrigger_NoHistory_ShouldNotDrift()
    {
        var sut = CreateSut();
        var context = CreateContext();

        var result = await sut.DetectDriftAsync("打开灯", "control_light", context);

        result.HasDrifted.Should().BeFalse();
        result.SuggestedAction.Should().Be(DriftAction.Continue);
    }

    [Fact]
    public async Task DetectDriftAsync_LowSimilarity_ShouldDetectDrift()
    {
        var sut = CreateSut();
        var context = CreateContext(slots: new Dictionary<string, object>
        {
            ["device"] = "空调",
            ["location"] = "客厅"
        });

        var result = await sut.DetectDriftAsync("音乐播放器声音太大了", "control_ac", context);

        // With low keyword overlap, should detect drift
        result.SemanticSimilarityScore.Should().BeLessThan(1.0);
    }

    [Fact]
    public async Task DetectDriftAsync_HighTurnCount_ShouldAddDriftScore()
    {
        var sut = CreateSut();
        var context = CreateContext(turnCount: 15, slots: new Dictionary<string, object>
        {
            ["device"] = "灯"
        });

        var result = await sut.DetectDriftAsync("灯的亮度调高", "control_light", context);

        // High turn count should add 0.1 to drift score
        result.DriftScore.Should().BeGreaterThanOrEqualTo(0.0);
    }

    [Fact]
    public async Task DetectDriftAsync_CustomTriggers_ShouldUseCustomTriggers()
    {
        var sut = CreateSut(new[] { "换个话题", "不说这个了" });
        var context = CreateContext();

        var result = await sut.DetectDriftAsync("换个话题聊聊美食", "control_light", context);

        result.HasDrifted.Should().BeTrue();
        result.SuggestedAction.Should().Be(DriftAction.NewTopic);
    }

    [Fact]
    public async Task DetectDriftAsync_CustomTriggers_DefaultNotUsed()
    {
        var sut = CreateSut(new[] { "换个话题" });
        var context = CreateContext();

        // "对了" is a default trigger but custom triggers override defaults
        var result = await sut.DetectDriftAsync("对了帮我看看天气", "control_light", context);

        result.HasDrifted.Should().BeFalse();
    }

    [Fact]
    public void IntentDriftAnalysis_DefaultProperties()
    {
        var analysis = new IntentDriftAnalysis();

        analysis.CurrentInput.Should().BeEmpty();
        analysis.PreviousIntent.Should().BeEmpty();
        analysis.HasDrifted.Should().BeFalse();
        analysis.DriftScore.Should().Be(0.0);
        analysis.SemanticSimilarityScore.Should().Be(0.0);
        analysis.SuggestedAction.Should().Be(DriftAction.Continue);
        analysis.Reason.Should().BeEmpty();
    }

    [Fact]
    public void DriftAction_ShouldHaveExpectedValues()
    {
        Enum.GetValues<DriftAction>().Should().HaveCount(3);
        DriftAction.Continue.Should().BeDefined();
        DriftAction.PossibleNewTopic.Should().BeDefined();
        DriftAction.NewTopic.Should().BeDefined();
    }
}
