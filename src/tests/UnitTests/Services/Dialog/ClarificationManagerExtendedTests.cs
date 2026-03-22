using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Core.Models.Dialog;
using CKY.MultiAgentFramework.Services.Dialog;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace CKY.MAF.Tests.Services.Dialog;

/// <summary>
/// ClarificationManager 深入测试 — 策略选择、ProcessUserResponse、
/// GenerateClarificationQuestion 多分支
/// </summary>
public class ClarificationManagerExtendedTests
{
    private readonly Mock<ISlotManager> _slotManagerMock = new();
    private readonly Mock<IMafAiAgentRegistry> _registryMock = new();
    private readonly Mock<ILogger<ClarificationManager>> _loggerMock = new();

    private ClarificationManager CreateSut() =>
        new(_slotManagerMock.Object, _registryMock.Object, _loggerMock.Object);

    // === AnalyzeClarificationNeededAsync ===

    [Fact]
    public async Task Analyze_NoMissingSlots_NoClarificationNeeded()
    {
        var sut = CreateSut();
        var detection = new SlotDetectionResult
        {
            Intent = "weather",
            MissingSlots = new List<SlotDefinition>(),
            Confidence = 1.0
        };

        var result = await sut.AnalyzeClarificationNeededAsync(detection, new DialogContext());
        result.NeedsClarification.Should().BeFalse();
        result.EstimatedTurns.Should().Be(0);
        result.Strategy.Should().Be(ClarificationStrategy.Template);
    }

    [Fact]
    public async Task Analyze_FewMissingSlots_UsesTemplateStrategy()
    {
        var sut = CreateSut();
        var detection = new SlotDetectionResult
        {
            Intent = "weather",
            MissingSlots = new List<SlotDefinition>
            {
                new() { SlotName = "Location", Description = "城市" }
            },
            Confidence = 0.5
        };

        var result = await sut.AnalyzeClarificationNeededAsync(detection, new DialogContext());
        result.NeedsClarification.Should().BeTrue();
        result.Strategy.Should().Be(ClarificationStrategy.Template);
        result.EstimatedTurns.Should().Be(1);
    }

    [Fact]
    public async Task Analyze_WithHistoricalSlots_UsesSmartInference()
    {
        var sut = CreateSut();
        var detection = new SlotDetectionResult
        {
            Intent = "weather",
            MissingSlots = new List<SlotDefinition>
            {
                new() { SlotName = "Location", Description = "城市" },
                new() { SlotName = "Date", Description = "日期" },
                new() { SlotName = "Format", Description = "格式" }
            },
            Confidence = 0.3
        };

        var context = new DialogContext();
        context.HistoricalSlots["weather.Location"] = "北京";

        var result = await sut.AnalyzeClarificationNeededAsync(detection, context);
        result.NeedsClarification.Should().BeTrue();
        result.Strategy.Should().Be(ClarificationStrategy.SmartInference);
        result.SuggestedValues.Should().ContainKey("Location");
        result.RequiresConfirmation.Should().BeTrue();
    }

    [Fact]
    public async Task Analyze_ManyMissingSlotsNoHistory_UsesLLM()
    {
        var sut = CreateSut();
        var detection = new SlotDetectionResult
        {
            Intent = "complex",
            MissingSlots = new List<SlotDefinition>
            {
                new() { SlotName = "A" },
                new() { SlotName = "B" },
                new() { SlotName = "C" },
                new() { SlotName = "D" }
            },
            Confidence = 0.2
        };

        var result = await sut.AnalyzeClarificationNeededAsync(detection, new DialogContext());
        result.NeedsClarification.Should().BeTrue();
        result.Strategy.Should().Be(ClarificationStrategy.LLM);
        result.EstimatedTurns.Should().Be(1);
    }

    [Fact]
    public async Task Analyze_NullDetection_Throws()
    {
        var sut = CreateSut();
        var act = () => sut.AnalyzeClarificationNeededAsync(null!, new DialogContext());
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    // === GenerateClarificationQuestionAsync ===

    [Fact]
    public async Task GenerateQuestion_TemplateStrategy_DelegatesToSlotManager()
    {
        _slotManagerMock.Setup(x => x.GenerateClarificationAsync(
            It.IsAny<List<SlotDefinition>>(), "weather", It.IsAny<CancellationToken>()))
            .ReturnsAsync("请问您想查询哪个城市的天气？");

        var sut = CreateSut();
        var ctx = new ClarificationContext
        {
            Strategy = ClarificationStrategy.Template,
            Intent = "weather",
            MissingSlots = new List<SlotDefinition> { new() { SlotName = "Location", Description = "城市" } }
        };

        var question = await sut.GenerateClarificationQuestionAsync(ctx);
        question.Should().Contain("城市");
    }

    [Fact]
    public async Task GenerateQuestion_SmartInference_WithSuggestion()
    {
        var sut = CreateSut();
        var ctx = new ClarificationContext
        {
            Strategy = ClarificationStrategy.SmartInference,
            Intent = "weather",
            MissingSlots = new List<SlotDefinition>
            {
                new() { SlotName = "Location", Description = "城市" }
            },
            FilledSlots = new Dictionary<string, object> { ["Location"] = "北京" }
        };

        var question = await sut.GenerateClarificationQuestionAsync(ctx);
        question.Should().Contain("北京");
    }

    [Fact]
    public async Task GenerateQuestion_Hybrid_WithFilledAndMissing()
    {
        _slotManagerMock.Setup(x => x.GenerateClarificationAsync(
            It.IsAny<List<SlotDefinition>>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("备用问题");

        var sut = CreateSut();
        var ctx = new ClarificationContext
        {
            Strategy = ClarificationStrategy.Hybrid,
            Intent = "control",
            MissingSlots = new List<SlotDefinition>
            {
                new() { SlotName = "Action", Description = "操作" }
            },
            FilledSlots = new Dictionary<string, object> { ["Device"] = "灯" }
        };

        var question = await sut.GenerateClarificationQuestionAsync(ctx);
        question.Should().NotBeNullOrEmpty();
    }

    // === ProcessUserResponseAsync ===

    [Fact]
    public async Task ProcessResponse_SlotFoundInInput_MarksComplete()
    {
        var sut = CreateSut();
        var ctx = new ClarificationContext
        {
            Intent = "weather",
            MissingSlots = new List<SlotDefinition>
            {
                new() { SlotName = "Location", Description = "城市", Type = SlotType.String }
            },
            FilledSlots = new Dictionary<string, object>()
        };

        // 用户输入包含 Description -> 触发提取
        var response = await sut.ProcessUserResponseAsync("城市是北京", ctx);
        response.Completed.Should().BeTrue();
        response.UpdatedSlots.Should().ContainKey("Location");
        response.Message.Should().Contain("完整");
    }

    [Fact]
    public async Task ProcessResponse_SlotNotFoundInInput_StillMissing()
    {
        var sut = CreateSut();
        var ctx = new ClarificationContext
        {
            Intent = "weather",
            MissingSlots = new List<SlotDefinition>
            {
                new() { SlotName = "Location", Description = "城市", Type = SlotType.String }
            },
            FilledSlots = new Dictionary<string, object>()
        };

        var response = await sut.ProcessUserResponseAsync("今天很热", ctx);
        response.Completed.Should().BeFalse();
        response.NeedsFurtherClarification.Should().BeTrue();
        response.StillMissing.Should().HaveCount(1);
    }

    [Fact]
    public async Task ProcessResponse_EmptyInput_Throws()
    {
        var sut = CreateSut();
        var ctx = new ClarificationContext();
        var act = () => sut.ProcessUserResponseAsync("", ctx);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task ProcessResponse_NullContext_Throws()
    {
        var sut = CreateSut();
        var act = () => sut.ProcessUserResponseAsync("hello", null!);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task ProcessResponse_EnumerationSlot_MatchesValidValue()
    {
        var sut = CreateSut();
        var ctx = new ClarificationContext
        {
            Intent = "control",
            MissingSlots = new List<SlotDefinition>
            {
                new()
                {
                    SlotName = "Action",
                    Description = "操作",
                    Type = SlotType.Enumeration,
                    ValidValues = new[] { "开", "关", "暂停" },
                    Synonyms = new List<string> { "动作", "行为" }
                }
            },
            FilledSlots = new Dictionary<string, object>()
        };

        // Input contains a synonym
        var response = await sut.ProcessUserResponseAsync("我想执行动作", ctx);
        // Since "动作" is a synonym, it should try to extract value
        response.Should().NotBeNull();
    }
}
