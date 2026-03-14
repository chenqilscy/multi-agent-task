using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Core.Models.Session;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace CKY.MultiAgentFramework.Infrastructure.Caching.Redis
{
    /// <summary>
    /// 基于 Redis 的 MafAiAgent 会话存储实现
    /// </summary>
    /// <remarks>
    /// 特性：
    /// - 分布式会话存储（支持多实例部署）
    /// - 自动过期机制（TTL）
    /// - 序列化/反序列化会话对象
    /// - 支持批量操作
    ///
    /// 数据结构：
    /// - Key: maf:session:{sessionId}
    /// - Value: JSON 序列化的会话对象
    /// - TTL: 24小时（可配置）
    /// </remarks>
    public class RedisMafAiSessionStore : IMafAiSessionStore
    {
        private readonly ILogger<RedisMafAiSessionStore> _logger;
        private readonly IDatabase _database;
        private readonly RedisSessionOptions _options;

        // Redis Key 前缀
        private const string SessionKeyPrefix = "maf:session:";

        public RedisMafAiSessionStore(
            IDatabase database,
            ILogger<RedisMafAiSessionStore> logger,
            RedisSessionOptions? options = null)
        {
            _database = database ?? throw new ArgumentNullException(nameof(database));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options ?? new RedisSessionOptions();
        }

        public async Task SaveAsync(MafSessionState session, CancellationToken cancellationToken = default)
        {
            try
            {
                var key = GetSessionKey(session.SessionId);
                var json = System.Text.Json.JsonSerializer.Serialize(session);
                var expiry = _options.SessionTimeout;

                await _database.StringSetAsync(key, json, expiry);
                _logger.LogDebug("[RedisSession] Saved session: {SessionId}, TTL: {TTL}m",
                    session.SessionId, expiry.TotalMinutes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[RedisSession] Failed to save session: {SessionId}", session.SessionId);
                throw;
            }
        }

        public async Task<MafSessionState?> LoadAsync(string sessionId, CancellationToken cancellationToken = default)
        {
            try
            {
                var key = GetSessionKey(sessionId);
                var json = await _database.StringGetAsync(key);

                if (json.IsNullOrEmpty)
                {
                    return null;
                }

                var session = System.Text.Json.JsonSerializer.Deserialize<MafSessionState>(json.ToString());
                if (session != null && session.IsExpired)
                {
                    // 会话已过期，删除并返回 null
                    await DeleteAsync(sessionId, cancellationToken);
                    return null;
                }

                _logger.LogDebug("[RedisSession] Loaded session: {SessionId}", sessionId);
                return session;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[RedisSession] Failed to load session: {SessionId}", sessionId);
                return null;
            }
        }

        public async Task DeleteAsync(string sessionId, CancellationToken cancellationToken = default)
        {
            try
            {
                var key = GetSessionKey(sessionId);
                await _database.KeyDeleteAsync(key);
                _logger.LogDebug("[RedisSession] Deleted session: {SessionId}", sessionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[RedisSession] Failed to delete session: {SessionId}", sessionId);
            }
        }

        public async Task<bool> ExistsAsync(string sessionId, CancellationToken cancellationToken = default)
        {
            try
            {
                var key = GetSessionKey(sessionId);
                return await _database.KeyExistsAsync(key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[RedisSession] Failed to check session existence: {SessionId}", sessionId);
                return false;
            }
        }

        public async Task<List<MafSessionState>> GetSessionsByUserAsync(string userId, CancellationToken cancellationToken = default)
        {
            try
            {
                var pattern = $"{SessionKeyPrefix}*";
                var keys = new List<RedisKey>();

                // 使用 ExecuteAsync 执行 KEYS 命令
                var redisKeys = await _database.ExecuteAsync("KEYS", new object[] { pattern });
                if (redisKeys != null && redisKeys.Length > 0)
                {
                    foreach (var key in (RedisKey[])redisKeys)
                    {
                        keys.Add(key);
                    }
                }

                var sessions = new List<MafSessionState>();
                foreach (var key in keys)
                {
                    var json = await _database.StringGetAsync(key);
                    if (!json.IsNullOrEmpty)
                    {
                        var session = System.Text.Json.JsonSerializer.Deserialize<MafSessionState>(json.ToString());
                        if (session != null && session.UserId == userId && !session.IsExpired)
                        {
                            sessions.Add(session);
                        }
                    }
                }

                return sessions;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[RedisSession] Failed to get sessions for user: {UserId}", userId);
                return new List<MafSessionState>();
            }
        }

        public async Task<List<MafSessionState>> GetActiveSessionsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var pattern = $"{SessionKeyPrefix}*";
                var keys = new List<RedisKey>();

                // 使用 ExecuteAsync 执行 KEYS 命令
                var redisKeys = await _database.ExecuteAsync("KEYS", new object[] { pattern });
                if (redisKeys != null && redisKeys.Length > 0)
                {
                    foreach (var key in (RedisKey[])redisKeys)
                    {
                        keys.Add(key);
                    }
                }

                var sessions = new List<MafSessionState>();
                foreach (var key in keys)
                {
                    var json = await _database.StringGetAsync(key);
                    if (!json.IsNullOrEmpty)
                    {
                        var session = System.Text.Json.JsonSerializer.Deserialize<MafSessionState>(json.ToString());
                        if (session != null && session.IsActive)
                        {
                            sessions.Add(session);
                        }
                    }
                }

                return sessions.OrderByDescending(s => s.LastActivityAt).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[RedisSession] Failed to get active sessions");
                return new List<MafSessionState>();
            }
        }

        public async Task<int> DeleteExpiredSessionsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var pattern = $"{SessionKeyPrefix}*";
                var keys = new List<RedisKey>();

                // 使用 ExecuteAsync 执行 KEYS 命令
                var redisKeys = await _database.ExecuteAsync("KEYS", new object[] { pattern });
                if (redisKeys != null && redisKeys.Length > 0)
                {
                    foreach (var key in (RedisKey[])redisKeys)
                    {
                        keys.Add(key);
                    }
                }

                var deletedCount = 0;
                foreach (var key in keys)
                {
                    var json = await _database.StringGetAsync(key);
                    if (!json.IsNullOrEmpty)
                    {
                        var session = System.Text.Json.JsonSerializer.Deserialize<MafSessionState>(json.ToString());
                        if (session != null && session.IsExpired)
                        {
                            await _database.KeyDeleteAsync(key);
                            deletedCount++;
                        }
                    }
                }

                if (deletedCount > 0)
                {
                    _logger.LogInformation("[RedisSession] Deleted {Count} expired sessions", deletedCount);
                }

                return deletedCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[RedisSession] Failed to delete expired sessions");
                return 0;
            }
        }

        public async Task SaveBatchAsync(IEnumerable<MafSessionState> sessions, CancellationToken cancellationToken = default)
        {
            var tasks = sessions.Select(session => SaveAsync(session, cancellationToken));
            await Task.WhenAll(tasks);
        }

        public async Task<List<MafSessionState>> LoadBatchAsync(IEnumerable<string> sessionIds, CancellationToken cancellationToken = default)
        {
            var tasks = sessionIds.Select(sessionId => LoadAsync(sessionId, cancellationToken));
            var results = await Task.WhenAll(tasks);
            return results.Where(s => s != null).ToList()!;
        }

        /// <summary>
        /// 生成 Redis Key
        /// </summary>
        private static string GetSessionKey(string sessionId) => $"{SessionKeyPrefix}{sessionId}";

        /// <summary>
        /// 扩展会话的过期时间
        /// </summary>
        public async Task ExtendExpirationAsync(string sessionId, TimeSpan? additionalTime = null)
        {
            try
            {
                var key = GetSessionKey(sessionId);
                var expiry = additionalTime ?? _options.SessionTimeout;
                await _database.KeyExpireAsync(key, expiry);
                _logger.LogDebug("[RedisSession] Extended expiration for: {SessionId}, TTL: {TTL}m",
                    sessionId, expiry.TotalMinutes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[RedisSession] Failed to extend expiration: {SessionId}", sessionId);
            }
        }
    }

    /// <summary>
    /// Redis 会话存储配置选项
    /// </summary>
    public class RedisSessionOptions
    {
        /// <summary>
        /// 会话超时时间（默认 24 小时）
        /// </summary>
        public TimeSpan SessionTimeout { get; set; } = TimeSpan.FromHours(24);

        /// <summary>
        /// 是否启用自动刷新（在会话活跃时自动延长过期时间）
        /// </summary>
        public bool AutoRefreshExpiration { get; set; } = true;

        /// <summary>
        /// 自动刷新的提前时间（在过期前多少时间开始刷新）
        /// </summary>
        public TimeSpan AutoRefreshAdvanceTime { get; set; } = TimeSpan.FromMinutes(5);
    }
}
