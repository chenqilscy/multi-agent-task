using Microsoft.Extensions.Logging;

namespace CKY.MultiAgentFramework.Services.Session.Utils
{
    /// <summary>
    /// L1 缓存管理器（内存缓存专用）
    /// </summary>
    /// <remarks>
    /// 职责：
    /// - 管理 L1 缓存的会话数据
    /// - 自动清理过期会话
    /// - 控制缓存大小，防止内存溢出
    /// - 提供缓存统计信息
    ///
    /// 缓存策略：
    /// - 默认最大容量：1000 个会话
    /// - 默认过期时间：30 分钟
    /// - 超出容量时，按最后活动时间清理最旧的会话
    ///
    /// 线程安全：
    /// - 当前实现非线程安全
    /// - 应在单线程环境下使用（如单例 SessionManager 内部）
    /// </remarks>
    public class L1CacheManager
    {
        private readonly ILogger<L1CacheManager> _logger;
        private readonly Dictionary<string, Core.Models.Session.MafSessionState> _cache;
        private readonly int _maxCacheSize;
        private readonly TimeSpan _expiration;

        public L1CacheManager(
            ILogger<L1CacheManager> logger,
            int maxCacheSize = 1000,
            TimeSpan? expiration = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _cache = new Dictionary<string, Core.Models.Session.MafSessionState>();
            _maxCacheSize = maxCacheSize;
            _expiration = expiration ?? TimeSpan.FromMinutes(30);
        }

        /// <summary>
        /// 清理过期的会话
        /// </summary>
        public int CleanupExpiredSessions()
        {
            var expiredSessions = _cache
                .Where(kvp => kvp.Value.IsExpired)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var sessionId in expiredSessions)
            {
                _cache.Remove(sessionId);
            }

            if (expiredSessions.Count > 0)
            {
                _logger.LogDebug("[L1CacheManager] Cleaned up {Count} expired sessions", expiredSessions.Count);
            }

            // 如果缓存大小超过限制，移除最旧的会话
            if (_cache.Count > _maxCacheSize)
            {
                var oldSessionsToRemove = _cache
                    .OrderBy(kvp => kvp.Value.LastActivityAt)
                    .Take(_cache.Count - _maxCacheSize)
                    .Select(kvp => kvp.Key)
                    .ToList();

                foreach (var sessionId in oldSessionsToRemove)
                {
                    _cache.Remove(sessionId);
                }

                _logger.LogDebug("[L1CacheManager] Removed {Count} old sessions to maintain cache size", oldSessionsToRemove.Count);
            }

            return expiredSessions.Count;
        }

        /// <summary>
        /// 获取缓存统计信息
        /// </summary>
        public (int Count, int ActiveCount, int ExpiredCount) GetStats()
        {
            var sessions = _cache.Values.ToList();
            return (
                Count: sessions.Count,
                ActiveCount: sessions.Count(s => s.IsActive),
                ExpiredCount: sessions.Count(s => s.IsExpired)
            );
        }

        /// <summary>
        /// 检查缓存是否包含指定会话
        /// </summary>
        public bool Contains(string sessionId)
        {
            return _cache.ContainsKey(sessionId);
        }

        /// <summary>
        /// 从缓存获取会话（不检查过期）
        /// </summary>
        public Core.Models.Session.MafSessionState? Get(string sessionId)
        {
            _cache.TryGetValue(sessionId, out var session);
            return session;
        }

        /// <summary>
        /// 添加会话到缓存
        /// </summary>
        public void Add(string sessionId, Core.Models.Session.MafSessionState session)
        {
            _cache[sessionId] = session;
        }

        /// <summary>
        /// 从缓存移除会话
        /// </summary>
        public bool Remove(string sessionId)
        {
            return _cache.Remove(sessionId);
        }
    }
}
