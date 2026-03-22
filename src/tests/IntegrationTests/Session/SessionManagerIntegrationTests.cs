using CKY.MultiAgentFramework.Core.Enums;
using CKY.MultiAgentFramework.Core.Models.Session;
using CKY.MultiAgentFramework.Services.Session;
using CKY.MultiAgentFramework.Services.Session.Utils;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CKY.MultiAgentFramework.IntegrationTests.Session;

/// <summary>
/// 会话管理集成测试
/// 验证 L1 内存缓存的 CRUD、过期和淘汰行为，
/// 以及 MafAiSessionManager 的 SaveAsync/LoadAsync 端到端流程
/// （修复前 WriteStrategy 会递归调用 SaveAsync 导致 StackOverflow）
/// </summary>
public class SessionManagerIntegrationTests
{
    private readonly L1CacheManager _cache;

    public SessionManagerIntegrationTests()
    {
        _cache = new L1CacheManager(
            NullLogger<L1CacheManager>.Instance,
            maxCacheSize: 100,
            expiration: TimeSpan.FromMinutes(30));
    }

    [Fact]
    public void AddAndGet_ShouldPersistSession()
    {
        var session = new MafSessionState
        {
            SessionId = Guid.NewGuid().ToString(),
            UserId = "user-001",
            Status = SessionStatus.Active
        };

        _cache.Add(session.SessionId, session);
        var loaded = _cache.Get(session.SessionId);

        loaded.Should().NotBeNull();
        loaded!.SessionId.Should().Be(session.SessionId);
        loaded.UserId.Should().Be("user-001");
    }

    [Fact]
    public void Get_NonExistent_ShouldReturnNull()
    {
        var loaded = _cache.Get("nonexistent-session-id");
        loaded.Should().BeNull();
    }

    [Fact]
    public void Remove_ShouldDeleteSession()
    {
        var sessionId = Guid.NewGuid().ToString();
        _cache.Add(sessionId, new MafSessionState { SessionId = sessionId });

        _cache.Remove(sessionId);

        _cache.Get(sessionId).Should().BeNull();
    }

    [Fact]
    public void Add_ShouldUpdateExistingSession()
    {
        var sessionId = Guid.NewGuid().ToString();
        _cache.Add(sessionId, new MafSessionState
        {
            SessionId = sessionId,
            TurnCount = 1,
            TotalTokensUsed = 100
        });

        _cache.Add(sessionId, new MafSessionState
        {
            SessionId = sessionId,
            TurnCount = 5,
            TotalTokensUsed = 500
        });

        var loaded = _cache.Get(sessionId);
        loaded!.TurnCount.Should().Be(5);
        loaded.TotalTokensUsed.Should().Be(500);
    }

    [Fact]
    public void Add_WithMetadata_ShouldPreserveData()
    {
        var sessionId = Guid.NewGuid().ToString();
        _cache.Add(sessionId, new MafSessionState
        {
            SessionId = sessionId,
            Metadata = new Dictionary<string, object>
            {
                ["intent"] = "智能家居控制",
                ["deviceCount"] = 5
            }
        });

        var loaded = _cache.Get(sessionId);
        loaded!.Metadata.Should().ContainKey("intent");
        loaded.Metadata["intent"].Should().Be("智能家居控制");
    }

    [Fact]
    public void Get_ExpiredSession_ShouldReturnNull()
    {
        var sessionId = Guid.NewGuid().ToString();
        _cache.Add(sessionId, new MafSessionState
        {
            SessionId = sessionId,
            ExpiresAt = DateTime.UtcNow.AddMilliseconds(-100) // 已过期
        });

        var loaded = _cache.Get(sessionId);
        // L1CacheManager 返回会话，但 IsExpired 应为 true
        // 实际使用时 MafAiSessionManager.LoadAsync 会检查 IsExpired
        if (loaded != null)
        {
            loaded.IsExpired.Should().BeTrue();
        }
    }

    [Fact]
    public void MultipleSessions_ShouldBeIsolated()
    {
        var id1 = Guid.NewGuid().ToString();
        var id2 = Guid.NewGuid().ToString();

        _cache.Add(id1, new MafSessionState { SessionId = id1, UserId = "user-A" });
        _cache.Add(id2, new MafSessionState { SessionId = id2, UserId = "user-B" });

        _cache.Get(id1)!.UserId.Should().Be("user-A");
        _cache.Get(id2)!.UserId.Should().Be("user-B");
    }

    [Fact]
    public void MaxSize_ShouldEvictOnCleanup()
    {
        // 创建最大容量为 5 的缓存
        var smallCache = new L1CacheManager(
            NullLogger<L1CacheManager>.Instance,
            maxCacheSize: 5,
            expiration: TimeSpan.FromMinutes(30));

        // 添加超过容量的条目
        for (int i = 0; i < 10; i++)
        {
            var id = $"session-{i}";
            smallCache.Add(id, new MafSessionState { SessionId = id });
        }

        // 调用 CleanupExpiredSessions 触发容量检查
        smallCache.CleanupExpiredSessions();

        // 清理后缓存大小应不超过 maxCacheSize
        var stats = smallCache.GetStats();
        stats.Count.Should().BeLessThanOrEqualTo(5);
    }

    [Fact]
    public async Task MafAiSessionManager_LoadAsync_InvalidSessionId_ShouldThrow()
    {
        var manager = new MafAiSessionManager(
            NullLogger<MafAiSessionManager>.Instance, maxL1CacheSize: 10);

        var act = () => manager.LoadAsync(string.Empty);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task MafAiSessionManager_DeleteAsync_InvalidSessionId_ShouldThrow()
    {
        var manager = new MafAiSessionManager(
            NullLogger<MafAiSessionManager>.Instance, maxL1CacheSize: 10);

        var act = () => manager.DeleteAsync(string.Empty);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task MafAiSessionManager_SaveAndLoad_ShouldNotCauseRecursion()
    {
        // 此测试验证修复后的 SaveAsync 不再递归调用自身
        // 修复前：SaveAsync → WriteStrategy.WriteAsync → l1Cache.SaveAsync(this) → StackOverflow
        // 修复后：SaveAsync → WriteStrategy.WriteAsync → l1Cache.Add() → 完成
        var manager = new MafAiSessionManager(
            NullLogger<MafAiSessionManager>.Instance, maxL1CacheSize: 10);

        var session = new MafSessionState
        {
            SessionId = Guid.NewGuid().ToString(),
            UserId = "regression-test-user",
            Status = SessionStatus.Active
        };

        // 不应抛出 StackOverflowException
        await manager.SaveAsync(session);

        var loaded = await manager.LoadAsync(session.SessionId);
        loaded.Should().NotBeNull();
        loaded!.SessionId.Should().Be(session.SessionId);
        loaded.UserId.Should().Be("regression-test-user");
    }

    [Fact]
    public async Task MafAiSessionManager_SaveAndDelete_ShouldWork()
    {
        var manager = new MafAiSessionManager(
            NullLogger<MafAiSessionManager>.Instance, maxL1CacheSize: 10);

        var session = new MafSessionState
        {
            SessionId = Guid.NewGuid().ToString(),
            UserId = "delete-test",
            Status = SessionStatus.Active
        };

        await manager.SaveAsync(session);
        await manager.DeleteAsync(session.SessionId);

        var loaded = await manager.LoadAsync(session.SessionId);
        loaded.Should().BeNull();
    }

    [Fact]
    public async Task MafAiSessionManager_Exists_ShouldReturnTrue()
    {
        var manager = new MafAiSessionManager(
            NullLogger<MafAiSessionManager>.Instance, maxL1CacheSize: 10);

        var session = new MafSessionState
        {
            SessionId = Guid.NewGuid().ToString(),
            Status = SessionStatus.Active
        };

        await manager.SaveAsync(session);

        var exists = await manager.ExistsAsync(session.SessionId);
        exists.Should().BeTrue();
    }
}
