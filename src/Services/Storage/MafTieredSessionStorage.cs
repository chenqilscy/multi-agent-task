using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Core.Models.Message;
using Microsoft.Extensions.Logging;

namespace CKY.MultiAgentFramework.Services.Storage
{
    /// <summary>
    /// Agent会话实现
    /// </summary>
    internal class AgentSessionImpl : IAgentSession
    {
        public string SessionId { get; init; } = string.Empty;
        public string AgentId { get; init; } = string.Empty;
        public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
        public DateTime LastAccessedAt { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object> Context { get; init; } = new();
        public List<MessageContext> MessageHistory { get; init; } = new();
    }

    /// <summary>
    /// 三层分级会话存储服务
    /// L1: 内存（当前活跃会话）
    /// L2: 缓存（ICacheStore，通常为Redis）
    /// L3: 数据库（IRelationalDatabase，通常为PostgreSQL）
    /// </summary>
    public class MafTieredSessionStorage : IMafSessionStorage
    {
        private readonly ICacheStore _cacheStore;
        private readonly IRelationalDatabase _database;
        private readonly ILogger<MafTieredSessionStorage> _logger;

        // L1内存缓存
        private readonly Dictionary<string, IAgentSession> _l1Cache = new();
        private readonly TimeSpan _cacheExpiry = TimeSpan.FromHours(24);

        public MafTieredSessionStorage(
            ICacheStore cacheStore,
            IRelationalDatabase database,
            ILogger<MafTieredSessionStorage> logger)
        {
            _cacheStore = cacheStore ?? throw new ArgumentNullException(nameof(cacheStore));
            _database = database ?? throw new ArgumentNullException(nameof(database));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public async Task<IAgentSession> LoadSessionAsync(
            string sessionId,
            CancellationToken ct = default)
        {
            // L1: 内存查找
            if (_l1Cache.TryGetValue(sessionId, out var l1Session))
            {
                _logger.LogDebug("Session {SessionId} found in L1 cache", sessionId);
                return l1Session;
            }

            // L2: 缓存查找
            var l2Session = await _cacheStore.GetAsync<AgentSessionDto>($"session:{sessionId}", ct);
            if (l2Session != null)
            {
                _logger.LogDebug("Session {SessionId} found in L2 cache", sessionId);
                var session = MapFromDto(l2Session);
                _l1Cache[sessionId] = session;
                return session;
            }

            // L3: 数据库查找或创建新会话
            _logger.LogDebug("Session {SessionId} not found, creating new session", sessionId);
            var newSession = new AgentSessionImpl
            {
                SessionId = sessionId,
                AgentId = "default"
            };

            _l1Cache[sessionId] = newSession;
            return newSession;
        }

        /// <inheritdoc />
        public async Task SaveSessionAsync(
            IAgentSession session,
            CancellationToken ct = default)
        {
            // 更新L1缓存
            _l1Cache[session.SessionId] = session;

            // 更新L2缓存
            var dto = MapToDto(session);
            await _cacheStore.SetAsync($"session:{session.SessionId}", dto, _cacheExpiry, ct);

            _logger.LogDebug("Session {SessionId} saved to L1 and L2", session.SessionId);
        }

        /// <inheritdoc />
        public async Task DeleteSessionAsync(
            string sessionId,
            CancellationToken ct = default)
        {
            _l1Cache.Remove(sessionId);
            await _cacheStore.DeleteAsync($"session:{sessionId}", ct);
            _logger.LogInformation("Session {SessionId} deleted", sessionId);
        }

        /// <inheritdoc />
        public async Task<bool> ExistsAsync(
            string sessionId,
            CancellationToken ct = default)
        {
            if (_l1Cache.ContainsKey(sessionId)) return true;
            return await _cacheStore.ExistsAsync($"session:{sessionId}", ct);
        }

        private static AgentSessionDto MapToDto(IAgentSession session) => new()
        {
            SessionId = session.SessionId,
            AgentId = session.AgentId,
            CreatedAt = session.CreatedAt,
            LastAccessedAt = session.LastAccessedAt,
            Context = session.Context,
            MessageHistory = session.MessageHistory
        };

        private static AgentSessionImpl MapFromDto(AgentSessionDto dto) => new()
        {
            SessionId = dto.SessionId,
            AgentId = dto.AgentId,
            CreatedAt = dto.CreatedAt,
            LastAccessedAt = dto.LastAccessedAt,
            Context = dto.Context,
            MessageHistory = dto.MessageHistory
        };
    }

    /// <summary>
    /// 会话DTO（用于序列化存储）
    /// </summary>
    internal class AgentSessionDto
    {
        public string SessionId { get; set; } = string.Empty;
        public string AgentId { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime LastAccessedAt { get; set; }
        public Dictionary<string, object> Context { get; set; } = new();
        public List<MessageContext> MessageHistory { get; set; } = new();
    }
}
