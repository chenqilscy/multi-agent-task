using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Core.Models.Dialog;
using CKY.MultiAgentFramework.Services.Dialog;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace CKY.MAF.Tests.Services.Dialog;

/// <summary>
/// SlotManager 深入测试 — FillSlotsAsync、GenerateClarificationAsync 多分支
/// </summary>
public class SlotManagerExtendedTests
{
    private readonly Mock<ISlotDefinitionProvider> _slotDefProviderMock = new();
    private readonly Mock<IMafAiAgentRegistry> _registryMock = new();
    private readonly Mock<ILogger<SlotManager>> _loggerMock = new();

    private SlotManager CreateSut() =>
        new(_slotDefProviderMock.Object, _registryMock.Object, _loggerMock.Object);

    // === FillSlotsAsync ===

    [Fact]
    public async Task FillSlots_NoDefinition_ReturnsProvidedSlots()
    {
        _slotDefProviderMock.Setup(x => x.GetDefinition("unknown"))
            .Returns((IntentSlotDefinition?)null);

        var sut = CreateSut();
        var provided = new Dictionary<string, object> { ["key"] = "val" };
        var result = await sut.FillSlotsAsync("unknown", provided, new DialogContext());
        result.Should().ContainKey("key");
        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task FillSlots_WithHistoricalSlots_FillsFromHistory()
    {
        var slotDef = new IntentSlotDefinition
        {
            Intent = "weather",
            RequiredSlots = new List<SlotDefinition>
            {
                new() { SlotName = "Location", Description = "城市" }
            }
        };
        _slotDefProviderMock.Setup(x => x.GetDefinition("weather")).Returns(slotDef);

        var context = new DialogContext();
        context.HistoricalSlots["weather.Location"] = "上海";

        var sut = CreateSut();
        var result = await sut.FillSlotsAsync("weather", new Dictionary<string, object>(), context);
        result.Should().ContainKey("Location");
        result["Location"].Should().Be("上海");
    }

    [Fact]
    public async Task FillSlots_WithPreviousSlots_FillsFromPrevious()
    {
        var slotDef = new IntentSlotDefinition
        {
            Intent = "weather",
            RequiredSlots = new List<SlotDefinition>
            {
                new() { SlotName = "Location", Description = "城市" }
            }
        };
        _slotDefProviderMock.Setup(x => x.GetDefinition("weather")).Returns(slotDef);

        var context = new DialogContext
        {
            PreviousIntent = "weather",
            PreviousSlots = new Dictionary<string, object> { ["Location"] = "深圳" }
        };

        var sut = CreateSut();
        var result = await sut.FillSlotsAsync("weather", new Dictionary<string, object>(), context);
        result.Should().ContainKey("Location");
        result["Location"].Should().Be("深圳");
    }

    [Fact]
    public async Task FillSlots_WithDefaultValue_UsesDefault()
    {
        var slotDef = new IntentSlotDefinition
        {
            Intent = "weather",
            RequiredSlots = new List<SlotDefinition>
            {
                new() { SlotName = "Format", Description = "格式", HasDefaultValue = true, DefaultValue = "摄氏度" }
            }
        };
        _slotDefProviderMock.Setup(x => x.GetDefinition("weather")).Returns(slotDef);

        var sut = CreateSut();
        var result = await sut.FillSlotsAsync("weather", new Dictionary<string, object>(), new DialogContext());
        result.Should().ContainKey("Format");
        result["Format"].Should().Be("摄氏度");
    }

    [Fact]
    public async Task FillSlots_AlreadyProvided_NotOverwritten()
    {
        var slotDef = new IntentSlotDefinition
        {
            Intent = "weather",
            RequiredSlots = new List<SlotDefinition>
            {
                new() { SlotName = "Location", Description = "城市", HasDefaultValue = true, DefaultValue = "北京" }
            }
        };
        _slotDefProviderMock.Setup(x => x.GetDefinition("weather")).Returns(slotDef);

        var provided = new Dictionary<string, object> { ["Location"] = "广州" };
        var sut = CreateSut();
        var result = await sut.FillSlotsAsync("weather", provided, new DialogContext());
        result["Location"].Should().Be("广州"); // 不被默认值覆盖
    }

    // === GenerateClarificationAsync ===

    [Fact]
    public async Task GenerateClarification_Empty_ReturnsEmpty()
    {
        var sut = CreateSut();
        var result = await sut.GenerateClarificationAsync(new List<SlotDefinition>(), "weather");
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GenerateClarification_SingleRequired_WithValidValues()
    {
        var sut = CreateSut();
        var slots = new List<SlotDefinition>
        {
            new()
            {
                SlotName = "Status",
                Description = "状态",
                Required = true,
                ValidValues = new[] { "开", "关" }
            }
        };

        var result = await sut.GenerateClarificationAsync(slots, "control");
        result.Should().Contain("状态");
        result.Should().Contain("开");
    }

    [Fact]
    public async Task GenerateClarification_SingleRequired_WithSynonyms()
    {
        var sut = CreateSut();
        var slots = new List<SlotDefinition>
        {
            new()
            {
                SlotName = "Device",
                Description = "设备",
                Required = true,
                Synonyms = new List<string> { "灯", "空调" }
            }
        };

        var result = await sut.GenerateClarificationAsync(slots, "control");
        result.Should().Contain("设备");
    }

    [Fact]
    public async Task GenerateClarification_MultipleRequired_CombinedPattern()
    {
        var sut = CreateSut();
        var slots = new List<SlotDefinition>
        {
            new() { SlotName = "Location", Description = "城市", Required = true },
            new() { SlotName = "Date", Description = "日期", Required = true }
        };

        var result = await sut.GenerateClarificationAsync(slots, "weather");
        // 应该匹配 TryGenerateCombinedQuestion 的 Location+Date 模式
        (result.Contains("城市") || result.Contains("天气")).Should().BeTrue();
    }

    [Fact]
    public async Task GenerateClarification_OptionalOnly_AsksIfNeeded()
    {
        var sut = CreateSut();
        var slots = new List<SlotDefinition>
        {
            new() { SlotName = "Extra", Description = "额外信息", Required = false }
        };

        var result = await sut.GenerateClarificationAsync(slots, "general");
        result.Should().Contain("可选");
    }

    // === DetectMissingSlotsAsync ===

    [Fact]
    public async Task DetectMissingSlots_EmptyInput_Throws()
    {
        var sut = CreateSut();
        var act = () => sut.DetectMissingSlotsAsync(
            "",
            new IntentRecognitionResult { PrimaryIntent = "test" },
            new EntityExtractionResult(),
            new DialogContext());
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task DetectMissingSlots_NullIntent_Throws()
    {
        var sut = CreateSut();
        var act = () => sut.DetectMissingSlotsAsync(
            "hello",
            null!,
            new EntityExtractionResult(),
            new DialogContext());
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task DetectMissingSlots_WithDefinition_AllProvided_NoMissing()
    {
        var slotDef = new IntentSlotDefinition
        {
            Intent = "weather",
            RequiredSlots = new List<SlotDefinition>
            {
                new() { SlotName = "Location", Description = "城市" }
            }
        };
        _slotDefProviderMock.Setup(x => x.GetDefinition("weather")).Returns(slotDef);

        var entities = new EntityExtractionResult();
        entities.Entities["Location"] = "北京";

        var sut = CreateSut();
        var result = await sut.DetectMissingSlotsAsync("查北京天气",
            new IntentRecognitionResult { PrimaryIntent = "weather" },
            entities,
            new DialogContext());

        result.Intent.Should().Be("weather");
        result.MissingSlots.Should().BeEmpty();
        result.DetectedSlots.Should().ContainKey("Location");
        result.Confidence.Should().Be(1.0);
    }

    [Fact]
    public async Task DetectMissingSlots_WithDefinition_SomeMissing()
    {
        var slotDef = new IntentSlotDefinition
        {
            Intent = "weather",
            RequiredSlots = new List<SlotDefinition>
            {
                new() { SlotName = "Location", Description = "城市" },
                new() { SlotName = "Date", Description = "日期" }
            }
        };
        _slotDefProviderMock.Setup(x => x.GetDefinition("weather")).Returns(slotDef);

        var entities = new EntityExtractionResult();
        entities.Entities["Location"] = "北京";

        var sut = CreateSut();
        var result = await sut.DetectMissingSlotsAsync("查北京天气",
            new IntentRecognitionResult { PrimaryIntent = "weather" },
            entities,
            new DialogContext());

        result.MissingSlots.Should().HaveCount(1);
        result.MissingSlots[0].SlotName.Should().Be("Date");
        result.Confidence.Should().Be(0.5);
    }
}
