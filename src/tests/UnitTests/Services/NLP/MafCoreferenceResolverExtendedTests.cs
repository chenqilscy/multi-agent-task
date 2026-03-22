using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Core.Models.Dialog;
using CKY.MultiAgentFramework.Services.NLP;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace CKY.MAF.Tests.Services.NLP;

/// <summary>
/// MafCoreferenceResolver 扩展测试 — 覆盖 ResolveCoreferencesWithLlmAsync 路径
/// </summary>
public class MafCoreferenceResolverExtendedTests
{
    private readonly Mock<IMafSessionStorage> _storageMock = new();
    private readonly Mock<ILogger<MafCoreferenceResolver>> _loggerMock = new();

    private MafCoreferenceResolver CreateSut() => new(_storageMock.Object, _loggerMock.Object);

    [Fact]
    public async Task ResolveCoreferencesWithLlmAsync_NoPronounsInInput_ReturnsOriginal()
    {
        var sut = CreateSut();
        var context = new DialogContext { SessionId = "s1" };
        var entities = new Dictionary<string, object> { ["device"] = "灯" };

        var result = await sut.ResolveCoreferencesWithLlmAsync("打开灯", context, entities);

        result.Should().Be("打开灯");
    }

    [Fact]
    public async Task ResolveCoreferencesWithLlmAsync_NoEntities_ReturnsOriginal()
    {
        var sut = CreateSut();
        var context = new DialogContext { SessionId = "s1" };
        var entities = new Dictionary<string, object>();

        var result = await sut.ResolveCoreferencesWithLlmAsync("把它打开", context, entities);

        result.Should().Be("把它打开");
    }

    [Fact]
    public async Task ResolveCoreferencesWithLlmAsync_WithPronounsAndEntities_ResolvesUsingHistoricalSlots()
    {
        var sut = CreateSut();
        var context = new DialogContext
        {
            SessionId = "s1",
            HistoricalSlots = new Dictionary<string, object> { ["light.device"] = "客厅灯" }
        };
        var entities = new Dictionary<string, object> { ["device"] = "灯" };

        var result = await sut.ResolveCoreferencesWithLlmAsync("把它打开", context, entities);

        // RuleBasedResolution replaces pronoun with last historical slot value
        result.Should().Be("把客厅灯打开");
    }

    [Fact]
    public async Task ResolveCoreferencesWithLlmAsync_NoHistoricalSlots_ReturnsOriginal()
    {
        var sut = CreateSut();
        var context = new DialogContext
        {
            SessionId = "s1",
            HistoricalSlots = new Dictionary<string, object>()
        };
        var entities = new Dictionary<string, object> { ["device"] = "灯" };

        var result = await sut.ResolveCoreferencesWithLlmAsync("把它打开", context, entities);

        result.Should().Be("把它打开");
    }

    [Fact]
    public async Task ResolveAsync_NoPronounsInInput_ReturnsOriginal()
    {
        var sut = CreateSut();
        var result = await sut.ResolveAsync("打开客厅灯", "conv1");
        result.Should().Be("打开客厅灯");
    }

    [Fact]
    public async Task ResolveAsync_WithPronouns_SessionExists_ReturnsInput()
    {
        var mockSession = new Mock<IAgentSession>();
        mockSession.Setup(s => s.SessionId).Returns("conv1");
        _storageMock.Setup(x => x.LoadSessionAsync("conv1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockSession.Object);

        var sut = CreateSut();
        var result = await sut.ResolveAsync("把它打开", "conv1");

        // Currently returns original input (simplified impl)
        result.Should().Be("把它打开");
    }

    [Fact]
    public async Task ResolveAsync_WithPronouns_SessionLoadThrowsJson_ReturnsInput()
    {
        _storageMock.Setup(x => x.LoadSessionAsync("conv1", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new System.Text.Json.JsonException("parse error"));

        var sut = CreateSut();
        var result = await sut.ResolveAsync("把它关掉", "conv1");

        result.Should().Be("把它关掉");
    }

    [Fact]
    public async Task ResolveAsync_WithPronouns_SessionLoadThrowsInvalidOp_ReturnsInput()
    {
        _storageMock.Setup(x => x.LoadSessionAsync("conv1", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("storage error"));

        var sut = CreateSut();
        var result = await sut.ResolveAsync("把那个打开", "conv1");

        result.Should().Be("把那个打开");
    }

    [Fact]
    public async Task ResolveAsync_WithPronouns_SessionLoadThrowsGeneric_ReturnsInput()
    {
        _storageMock.Setup(x => x.LoadSessionAsync("conv1", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("unexpected"));

        var sut = CreateSut();
        var result = await sut.ResolveAsync("这个怎么用", "conv1");

        result.Should().Be("这个怎么用");
    }

    [Fact]
    public async Task ResolveCoreferencesWithLlmAsync_MultiplePronounsReplacesFirst()
    {
        var sut = CreateSut();
        var context = new DialogContext
        {
            SessionId = "s1",
            HistoricalSlots = new Dictionary<string, object> { ["light.device"] = "台灯" }
        };
        var entities = new Dictionary<string, object> { ["device"] = "灯" };

        // "它" will be found first and replaced
        var result = await sut.ResolveCoreferencesWithLlmAsync("把它和那个都关闭", context, entities);

        result.Should().Contain("台灯");
    }
}
