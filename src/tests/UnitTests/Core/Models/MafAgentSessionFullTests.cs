using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Core.Models.Session;
using FluentAssertions;
using Moq;
using Xunit;

namespace CKY.MultiAgentFramework.Tests.Core.Models;

public class MafAgentSessionTests
{
    [Fact]
    public void Constructor_NoArgs_ShouldCreateSession()
    {
        var session = new MafAgentSession();
        session.Should().NotBeNull();
    }

    [Fact]
    public void MafSession_ShouldAutoCreate()
    {
        var session = new MafAgentSession();
        var mafSession = session.MafSession;

        mafSession.Should().NotBeNull();
        mafSession.SessionId.Should().NotBeNullOrEmpty();
        mafSession.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void MafSession_ShouldReturnSameInstance()
    {
        var session = new MafAgentSession();
        var first = session.MafSession;
        var second = session.MafSession;
        first.Should().BeSameAs(second);
    }

    [Fact]
    public void IsExpired_NoSession_ShouldReturnFalse()
    {
        var session = new MafAgentSession();
        // Before accessing MafSession, _mafSession is null → IsExpired = false
        session.IsExpired.Should().BeFalse();
    }

    [Fact]
    public void IsActive_NoSession_ShouldReturnFalse()
    {
        var session = new MafAgentSession();
        session.IsActive.Should().BeFalse();
    }

    [Fact]
    public void IsActive_WithSession_ShouldReturnTrue()
    {
        var session = new MafAgentSession();
        _ = session.MafSession; // trigger auto-create
        session.IsActive.Should().BeTrue();
    }

    [Fact]
    public void UpdateActivity_WithTokens_ShouldUpdateSessionState()
    {
        var session = new MafAgentSession();
        _ = session.MafSession;

        session.UpdateActivity(100);

        session.MafSession.TotalTokensUsed.Should().Be(100);
    }

    [Fact]
    public void IncrementTurn_ShouldIncreaseTurnCount()
    {
        var session = new MafAgentSession();
        _ = session.MafSession;

        session.IncrementTurn();
        session.IncrementTurn();

        session.MafSession.TurnCount.Should().Be(2);
    }

    [Fact]
    public async Task LoadAsync_NoStore_ShouldReturnFalse()
    {
        var session = new MafAgentSession();
        var result = await session.LoadAsync("test-session");
        result.Should().BeFalse();
    }

    [Fact]
    public async Task LoadAsync_WithStore_SessionFound_ShouldReturnTrue()
    {
        var store = new Mock<IMafAiSessionStore>();
        var existingState = new MafSessionState { SessionId = "existing" };
        store.Setup(x => x.LoadAsync("existing", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingState);

        var session = new MafAgentSession(store.Object);
        var result = await session.LoadAsync("existing");

        result.Should().BeTrue();
        session.MafSession.SessionId.Should().Be("existing");
    }

    [Fact]
    public async Task LoadAsync_WithStore_SessionNotFound_ShouldReturnFalse()
    {
        var store = new Mock<IMafAiSessionStore>();
        store.Setup(x => x.LoadAsync("missing", It.IsAny<CancellationToken>()))
            .ReturnsAsync((MafSessionState?)null);

        var session = new MafAgentSession(store.Object);
        var result = await session.LoadAsync("missing");

        result.Should().BeFalse();
    }

    [Fact]
    public async Task SaveAsync_NoStore_ShouldNotThrow()
    {
        var session = new MafAgentSession();
        await session.Invoking(s => s.SaveAsync()).Should().NotThrowAsync();
    }

    [Fact]
    public async Task SaveAsync_WithStore_ShouldCallStore()
    {
        var store = new Mock<IMafAiSessionStore>();
        var session = new MafAgentSession(store.Object);
        _ = session.MafSession; // trigger auto-create

        await session.SaveAsync();

        store.Verify(x => x.SaveAsync(It.IsAny<MafSessionState>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void ChatMessageEntity_DefaultValues()
    {
        var entity = new ChatMessageEntity();
        entity.SessionId.Should().BeEmpty();
        entity.Role.Should().BeEmpty();
        entity.Content.Should().BeEmpty();
        entity.CreatedAt.Should().Be(default);
    }

    [Fact]
    public void ChatMessageEntity_SetProperties()
    {
        var now = DateTime.UtcNow;
        var entity = new ChatMessageEntity
        {
            SessionId = "sess-1",
            Role = "user",
            Content = "hello",
            CreatedAt = now
        };

        entity.SessionId.Should().Be("sess-1");
        entity.Role.Should().Be("user");
        entity.Content.Should().Be("hello");
        entity.CreatedAt.Should().Be(now);
    }

    [Fact]
    public async Task LoadChatHistoryAsync_NoCache_ShouldReturnEmptyList()
    {
        var session = new MafAgentSession();
        var history = await session.LoadChatHistoryAsync();
        history.Should().BeEmpty();
    }

    [Fact]
    public async Task ClearChatHistoryAsync_NoCache_ShouldNotThrow()
    {
        var session = new MafAgentSession();
        await session.Invoking(s => s.ClearChatHistoryAsync()).Should().NotThrowAsync();
    }
}
