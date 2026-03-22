using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Core.Models.Dialog;
using CKY.MultiAgentFramework.Services.NLP;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CKY.MultiAgentFramework.Tests.Services.NLP;

public class MafCoreferenceResolverTests
{
    private readonly Mock<IMafSessionStorage> _sessionStorage = new();
    private readonly Mock<ILogger<MafCoreferenceResolver>> _logger = new();

    private MafCoreferenceResolver CreateSut() =>
        new(_sessionStorage.Object, _logger.Object);

    [Fact]
    public void Constructor_NullArgs_ShouldThrow()
    {
        Assert.Throws<ArgumentNullException>(() => new MafCoreferenceResolver(null!, _logger.Object));
        Assert.Throws<ArgumentNullException>(() => new MafCoreferenceResolver(_sessionStorage.Object, null!));
    }

    [Fact]
    public async Task ResolveAsync_NoPronoun_ShouldReturnOriginal()
    {
        var sut = CreateSut();
        var result = await sut.ResolveAsync("打开客厅的灯", "session-1");
        result.Should().Be("打开客厅的灯");
    }

    [Fact]
    public async Task ResolveAsync_WithPronoun_SessionExists_ShouldTryResolve()
    {
        var mockSession = new Mock<IAgentSession>();
        mockSession.Setup(x => x.SessionId).Returns("session-1");
        _sessionStorage.Setup(x => x.LoadSessionAsync("session-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockSession.Object);

        var sut = CreateSut();
        var result = await sut.ResolveAsync("把它关掉", "session-1");

        // Current implementation returns original text (simplified)
        result.Should().Be("把它关掉");
        _sessionStorage.Verify(x => x.LoadSessionAsync("session-1", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ResolveAsync_WithPronoun_SessionLoadFails_ShouldReturnOriginal()
    {
        _sessionStorage.Setup(x => x.LoadSessionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("session not found"));

        var sut = CreateSut();
        var result = await sut.ResolveAsync("把那个打开", "session-1");

        result.Should().Be("把那个打开");
    }

    [Fact]
    public async Task ResolveCoreferencesWithLlmAsync_NoPronoun_ShouldReturnOriginal()
    {
        var sut = CreateSut();
        var context = new DialogContext { SessionId = "s1" };

        var result = await sut.ResolveCoreferencesWithLlmAsync(
            "打开灯", context, new Dictionary<string, object> { ["device"] = "灯" });

        result.Should().Be("打开灯");
    }

    [Fact]
    public async Task ResolveCoreferencesWithLlmAsync_NoEntities_ShouldReturnOriginal()
    {
        var sut = CreateSut();
        var context = new DialogContext { SessionId = "s1" };

        var result = await sut.ResolveCoreferencesWithLlmAsync(
            "把它关掉", context, new Dictionary<string, object>());

        result.Should().Be("把它关掉");
    }

    [Fact]
    public async Task ResolveCoreferencesWithLlmAsync_WithPronounAndEntities_ShouldResolve()
    {
        var sut = CreateSut();
        var context = new DialogContext
        {
            SessionId = "s1",
            HistoricalSlots = new Dictionary<string, object> { ["device"] = "空调" }
        };
        var entities = new Dictionary<string, object> { ["device"] = "空调" };

        var result = await sut.ResolveCoreferencesWithLlmAsync("把它关掉", context, entities);

        // Rule-based resolution should replace the pronoun with the last historical slot value
        result.Should().Contain("空调");
    }
}
