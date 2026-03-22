using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Core.Models.Session;
using FluentAssertions;
using Microsoft.Extensions.AI;
using Moq;

namespace CKY.MAF.Tests.Core.Models;

/// <summary>
/// MafAgentSession 聊天历史管理测试 — SaveChatHistory、AddMessage、ClearChatHistory、
/// LoadChatHistory 的 L2/L3 分层逻辑
/// </summary>
public class MafAgentSessionChatHistoryTests
{
    private readonly Mock<IMafAiSessionStore> _sessionStoreMock = new();
    private readonly Mock<ICacheStore> _l2CacheMock = new();
    private readonly Mock<IRelationalDatabase> _l3DbMock = new();

    private MafAgentSession CreateSession(
        IMafAiSessionStore? store = null,
        ICacheStore? l2 = null,
        IRelationalDatabase? l3 = null)
    {
        return new MafAgentSession(store, l2, l3);
    }

    // === SaveChatHistoryAsync ===

    [Fact]
    public async Task SaveChatHistory_EmptyMessages_DoesNothing()
    {
        var session = CreateSession(null, _l2CacheMock.Object, _l3DbMock.Object);
        await session.SaveChatHistoryAsync(new List<ChatMessage>());

        _l2CacheMock.Verify(x => x.SetAsync(
            It.IsAny<string>(), It.IsAny<object>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task SaveChatHistory_NullMessages_DoesNothing()
    {
        var session = CreateSession(null, _l2CacheMock.Object, _l3DbMock.Object);
        await session.SaveChatHistoryAsync(null!);

        _l2CacheMock.Verify(x => x.SetAsync(
            It.IsAny<string>(), It.IsAny<object>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task SaveChatHistory_WithL2Cache_SavesToCacheWithTtl()
    {
        var session = CreateSession(null, _l2CacheMock.Object);
        var messages = new List<ChatMessage>
        {
            new(ChatRole.User, "你好")
        };

        await session.SaveChatHistoryAsync(messages);

        _l2CacheMock.Verify(x => x.SetAsync(
            It.Is<string>(k => k.StartsWith("maf:chat:history:")),
            It.IsAny<object>(),
            It.Is<TimeSpan>(t => t == TimeSpan.FromHours(24)),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SaveChatHistory_WithL3Db_SavesEntities()
    {
        var session = CreateSession(null, null, _l3DbMock.Object);
        var messages = new List<ChatMessage>
        {
            new(ChatRole.User, "消息1"),
            new(ChatRole.Assistant, "回复1")
        };

        await session.SaveChatHistoryAsync(messages);

        _l3DbMock.Verify(x => x.BulkInsertAsync(
            It.IsAny<IEnumerable<ChatMessageEntity>>(),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SaveChatHistory_L2Fails_ContinuesWithL3()
    {
        _l2CacheMock.Setup(x => x.SetAsync(
            It.IsAny<string>(), It.IsAny<object>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Redis down"));

        var session = CreateSession(null, _l2CacheMock.Object, _l3DbMock.Object);
        var messages = new List<ChatMessage> { new(ChatRole.User, "test") };

        await session.Invoking(s => s.SaveChatHistoryAsync(messages))
            .Should().NotThrowAsync();

        _l3DbMock.Verify(x => x.BulkInsertAsync(
            It.IsAny<IEnumerable<ChatMessageEntity>>(),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // === LoadChatHistoryAsync ===

    [Fact]
    public async Task LoadChatHistory_NoStores_ReturnsEmpty()
    {
        var session = CreateSession();
        var history = await session.LoadChatHistoryAsync();
        history.Should().BeEmpty();
    }

    [Fact]
    public async Task LoadChatHistory_L2CacheHit_ReturnsCached()
    {
        var cachedMessages = new List<ChatMessage>
        {
            new(ChatRole.User, "缓存消息")
        };

        _l2CacheMock.Setup(x => x.GetAsync<List<ChatMessage>>(
            It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedMessages);

        var session = CreateSession(null, _l2CacheMock.Object);
        var history = await session.LoadChatHistoryAsync();
        history.Should().HaveCount(1);
    }

    [Fact]
    public async Task LoadChatHistory_L2Fails_FallsToL3()
    {
        _l2CacheMock.Setup(x => x.GetAsync<List<ChatMessage>>(
            It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Cache fail"));

        var dbMessages = new List<ChatMessage>
        {
            new(ChatRole.User, "DB消息")
        };

        _l3DbMock.Setup(x => x.ExecuteSqlAsync<ChatMessage>(
            It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dbMessages);

        var session = CreateSession(null, _l2CacheMock.Object, _l3DbMock.Object);
        var history = await session.LoadChatHistoryAsync();
        history.Should().HaveCount(1);
    }

    // === ClearChatHistoryAsync ===

    [Fact]
    public async Task ClearChatHistory_ClearsL2AndL3()
    {
        var session = CreateSession(null, _l2CacheMock.Object, _l3DbMock.Object);
        await session.ClearChatHistoryAsync();

        _l2CacheMock.Verify(x => x.DeleteAsync(
            It.Is<string>(k => k.StartsWith("maf:chat:history:")),
            It.IsAny<CancellationToken>()),
            Times.Once);

        _l3DbMock.Verify(x => x.ExecuteSqlAsync<object>(
            It.Is<string>(s => s.Contains("DELETE")),
            It.IsAny<object>(),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ClearChatHistory_L2Fails_DoesNotThrow()
    {
        _l2CacheMock.Setup(x => x.DeleteAsync(
            It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Redis down"));

        var session = CreateSession(null, _l2CacheMock.Object, _l3DbMock.Object);
        await session.Invoking(s => s.ClearChatHistoryAsync())
            .Should().NotThrowAsync();
    }

    // === UpdateActivity / IncrementTurn 在无 session 时 ===

    [Fact]
    public void UpdateActivity_WithoutMafSession_DoesNotThrow()
    {
        var session = CreateSession();
        // _mafSession 是 null，UpdateActivity 应该安全处理
        session.Invoking(s => s.UpdateActivity(100))
            .Should().NotThrow();
    }

    [Fact]
    public void IncrementTurn_WithoutMafSession_DoesNotThrow()
    {
        var session = CreateSession();
        session.Invoking(s => s.IncrementTurn())
            .Should().NotThrow();
    }

    [Fact]
    public void MafSession_AccessThenUpdateActivity_Works()
    {
        var session = CreateSession();
        _ = session.MafSession; // 触发创建
        session.UpdateActivity(50);
        session.MafSession.TotalTokensUsed.Should().Be(50);
    }

    [Fact]
    public void MafSession_AccessThenIncrementTurn_Works()
    {
        var session = CreateSession();
        _ = session.MafSession; // 触发创建
        session.IncrementTurn();
        session.MafSession.TurnCount.Should().Be(1);
    }
}
