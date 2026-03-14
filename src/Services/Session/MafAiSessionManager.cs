using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Core.Models.Session;
using Microsoft.Extensions.Logging;

namespace CKY.MultiAgentFramework.Services.Session
{
    /// <summary>
    /// MafAiAgent 会话管理器（三层存储策略）
    /// </summary>
    /// <remarks>
    /// 存储层级：
    /// - L1 (内存): 当前活跃会话，最快访问
    /// - L2 (Redis): 分布式缓存，24小时 TTL
    /// - L3 (数据库): 长期存储，用于分析和恢复
    ///
    /// 读写策略：
    /// - 读: L1 → L2 → L3（逐级查找）
    /// - 写: 同时写入 L1, L2, L3（异步写入）
    /// - 更新: 更新 L1，异步同步到 L2, L3
    /// </remarks>
    public class MafAiSessionManager : IMafAiSessionStore
    {
        private readonly ILogger<MafAiSessionManager> _logger;
        private readonly IMafAiSessionStore? _l2Store; // Redis
        private readonly IMafAiSessionStore? _l3Store; // Database
        private readonly Dictionary<string, MafSessionState> _l1Cache; // Memory

        // L1 缓存配置
        private readonly int _maxL1CacheSize;
        private readonly TimeSpan _l1CacheExpiration;

        public MafAiSessionManager(
            ILogger<MafAiSessionManager> logger,
            IMafAiSessionStore? l2Store = null,
            IMafAiSessionStore? l3Store = null,
            int maxL1CacheSize = 1000,
            TimeSpan? l1CacheExpiration = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _l2Store = l2Store; // Optional Redis store
            _l3Store = l3Store; // Optional Database store
            _l1Cache = new Dictionary<string, MafSessionState>();
            _maxL1CacheSize = maxL1CacheSize;
            _l1CacheExpiration = l1CacheExpiration ?? TimeSpan.FromMinutes(30);
        }

        public async Task SaveAsync(MafSessionState session, CancellationToken cancellationToken = default)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));

            try
            {
                // 1. 保存到 L1（内存）- 同步，最高优先级
                _l1Cache[session.SessionId] = session;

                // 2. 保存到 L2（Redis）- 异步，用于分布式访问
                if (_l2Store != null)
                {
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await _l2Store.SaveAsync(session, cancellationToken);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "[SessionManager] Failed to save session to L2 (Redis): {SessionId}", session.SessionId);
                        }
                    }, cancellationToken);
                }

                // 3. 保存到 L3（数据库）- 异步，用于长期存储
                if (_l3Store != null)
                {
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await _l3Store.SaveAsync(session, cancellationToken);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "[SessionManager] Failed to save session to L3 (Database): {SessionId}", session.SessionId);
                        }
                    }, cancellationToken);
                }

                _logger.LogDebug("[SessionManager] Saved session to L1: {SessionId}", session.SessionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SessionManager] Failed to save session: {SessionId}", session.SessionId);
                throw;
            }
        }

        public async Task<MafSessionState?> LoadAsync(string sessionId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(sessionId))
                throw new ArgumentException("Session ID cannot be null or empty", nameof(sessionId));

            try
            {
                // 1. 尝试从 L1（内存）加载
                if (_l1Cache.TryGetValue(sessionId, out var l1Session))
                {
                    if (!l1Session.IsExpired)
                    {
                        _logger.LogDebug("[SessionManager] Loaded session from L1: {SessionId}", sessionId);
                        return l1Session;
                    }
                    else
                    {
                        // L1 中的会话已过期，移除
                        _l1Cache.Remove(sessionId);
                    }
                }

                // 2. 尝试从 L2（Redis）加载
                if (_l2Store != null)
                {
                    var l2Session = await _l2Store.LoadAsync(sessionId, cancellationToken);
                    if (l2Session != null && !l2Session.IsExpired)
                    {
                        // 回填 L1 缓存
                        _l1Cache[sessionId] = l2Session;
                        _logger.LogDebug("[SessionManager] Loaded session from L2 (Redis): {SessionId}", sessionId);
                        return l2Session;
                    }
                }

                // 3. 尝试从 L3（数据库）加载
                if (_l3Store != null)
                {
                    var l3Session = await _l3Store.LoadAsync(sessionId, cancellationToken);
                    if (l3Session != null && !l3Session.IsExpired)
                    {
                        // 回填 L1 缓存和 L2（如果存在）
                        _l1Cache[sessionId] = l3Session;
                        if (_l2Store != null)
                        {
                            _ = Task.Run(async () =>
                            {
                                try
                                {
                                    await _l2Store.SaveAsync(l3Session, cancellationToken);
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogWarning(ex, "[SessionManager] Failed to backfill L2 (Redis) for session: {SessionId}", sessionId);
                                }
                            }, cancellationToken);
                        }

                        _logger.LogDebug("[SessionManager] Loaded session from L3 (Database): {SessionId}", sessionId);
                        return l3Session;
                    }
                }

                _logger.LogDebug("[SessionManager] Session not found: {SessionId}", sessionId);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SessionManager] Failed to load session: {SessionId}", sessionId);
                return null;
            }
        }

        public async Task DeleteAsync(string sessionId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(sessionId))
                throw new ArgumentException("Session ID cannot be null or empty", nameof(sessionId));

            try
            {
                // 1. 从 L1（内存）删除
                _l1Cache.Remove(sessionId);

                // 2. 从 L2（Redis）删除 - 异步
                if (_l2Store != null)
                {
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await _l2Store.DeleteAsync(sessionId, cancellationToken);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "[SessionManager] Failed to delete session from L2 (Redis): {SessionId}", sessionId);
                        }
                    }, cancellationToken);
                }

                // 3. 从 L3（数据库）删除 - 异步
                if (_l3Store != null)
                {
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await _l3Store.DeleteAsync(sessionId, cancellationToken);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "[SessionManager] Failed to delete session from L3 (Database): {SessionId}", sessionId);
                        }
                    }, cancellationToken);
                }

                _logger.LogDebug("[SessionManager] Deleted session from L1: {SessionId}", sessionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SessionManager] Failed to delete session: {SessionId}", sessionId);
                throw;
            }
        }

        public async Task<bool> ExistsAsync(string sessionId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(sessionId))
                return false;

            try
            {
                // 1. 检查 L1
                if (_l1Cache.ContainsKey(sessionId))
                    return true;

                // 2. 检查 L2
                if (_l2Store != null)
                {
                    var existsInL2 = await _l2Store.ExistsAsync(sessionId, cancellationToken);
                    if (existsInL2)
                        return true;
                }

                // 3. 检查 L3
                if (_l3Store != null)
                {
                    var existsInL3 = await _l3Store.ExistsAsync(sessionId, cancellationToken);
                    return existsInL3;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SessionManager] Failed to check session existence: {SessionId}", sessionId);
                return false;
            }
        }

        public async Task<List<MafSessionState>> GetSessionsByUserAsync(string userId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(userId))
                return new List<MafSessionState>();

            try
            {
                var allSessions = new List<MafSessionState>();

                // 1. 从 L1 获取
                var l1Sessions = _l1Cache.Values.Where(s => s.UserId == userId && !s.IsExpired);
                allSessions.AddRange(l1Sessions);

                // 2. 从 L2 获取（去重）
                if (_l2Store != null)
                {
                    var l2Sessions = await _l2Store.GetSessionsByUserAsync(userId, cancellationToken);
                    var l2SessionIds = new HashSet<string>(l2Sessions.Select(s => s.SessionId));
                    allSessions.AddRange(l2Sessions.Where(s => !l2SessionIds.Contains(s.SessionId)));
                }

                // 3. 从 L3 获取（去重）
                if (_l3Store != null)
                {
                    var l3Sessions = await _l3Store.GetSessionsByUserAsync(userId, cancellationToken);
                    var l3SessionIds = new HashSet<string>(allSessions.Select(s => s.SessionId));
                    allSessions.AddRange(l3Sessions.Where(s => !l3SessionIds.Contains(s.SessionId)));
                }

                return allSessions.OrderByDescending(s => s.LastActivityAt).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SessionManager] Failed to get sessions for user: {UserId}", userId);
                return new List<MafSessionState>();
            }
        }

        public async Task<List<MafSessionState>> GetActiveSessionsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var allSessions = new List<MafSessionState>();

                // 1. 从 L1 获取
                var l1Sessions = _l1Cache.Values.Where(s => s.IsActive);
                allSessions.AddRange(l1Sessions);

                // 2. 从 L2 获取（去重）
                if (_l2Store != null)
                {
                    var l2Sessions = await _l2Store.GetActiveSessionsAsync(cancellationToken);
                    var l2SessionIds = new HashSet<string>(l2Sessions.Select(s => s.SessionId));
                    allSessions.AddRange(l2Sessions.Where(s => !l2SessionIds.Contains(s.SessionId)));
                }

                // 3. 从 L3 获取（去重）
                if (_l3Store != null)
                {
                    var l3Sessions = await _l3Store.GetActiveSessionsAsync(cancellationToken);
                    var l3SessionIds = new HashSet<string>(allSessions.Select(s => s.SessionId));
                    allSessions.AddRange(l3Sessions.Where(s => !l3SessionIds.Contains(s.SessionId)));
                }

                return allSessions.OrderByDescending(s => s.LastActivityAt).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SessionManager] Failed to get active sessions");
                return new List<MafSessionState>();
            }
        }

        public async Task<int> DeleteExpiredSessionsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var deletedCount = 0;

                // 1. 清理 L1 中的过期会话
                var expiredL1Sessions = _l1Cache.Where(kvp => kvp.Value.IsExpired).ToList();
                foreach (var kvp in expiredL1Sessions)
                {
                    _l1Cache.Remove(kvp.Key);
                    deletedCount++;
                }

                // 2. 清理 L2
                if (_l2Store != null)
                {
                    deletedCount += await _l2Store.DeleteExpiredSessionsAsync(cancellationToken);
                }

                // 3. 清理 L3
                if (_l3Store != null)
                {
                    deletedCount += await _l3Store.DeleteExpiredSessionsAsync(cancellationToken);
                }

                if (deletedCount > 0)
                {
                    _logger.LogInformation("[SessionManager] Deleted {Count} expired sessions", deletedCount);
                }

                return deletedCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SessionManager] Failed to delete expired sessions");
                return 0;
            }
        }

        public async Task SaveBatchAsync(IEnumerable<MafSessionState> sessions, CancellationToken cancellationToken = default)
        {
            var sessionList = sessions.ToList();
            if (sessionList.Count == 0) return;

            try
            {
                // 1. 保存到 L1
                foreach (var session in sessionList)
                {
                    _l1Cache[session.SessionId] = session;
                }

                // 2. 保存到 L2 和 L3 - 异步并行
                var saveTasks = new List<Task>();

                if (_l2Store != null)
                {
                    saveTasks.Add(Task.Run(async () =>
                    {
                        try
                        {
                            await _l2Store.SaveBatchAsync(sessionList, cancellationToken);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "[SessionManager] Failed to save batch sessions to L2 (Redis)");
                        }
                    }, cancellationToken));
                }

                if (_l3Store != null)
                {
                    saveTasks.Add(Task.Run(async () =>
                    {
                        try
                        {
                            await _l3Store.SaveBatchAsync(sessionList, cancellationToken);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "[SessionManager] Failed to save batch sessions to L3 (Database)");
                        }
                    }, cancellationToken));
                }

                await Task.WhenAll(saveTasks);
                _logger.LogInformation("[SessionManager] Saved {Count} sessions in batch", sessionList.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SessionManager] Failed to save batch sessions");
                throw;
            }
        }

        public async Task<List<MafSessionState>> LoadBatchAsync(IEnumerable<string> sessionIds, CancellationToken cancellationToken = default)
        {
            var sessionIdList = sessionIds.ToList();
            if (sessionIdList.Count == 0) return new List<MafSessionState>();

            try
            {
                var result = new List<MafSessionState>();
                var missingIds = new List<string>();

                // 1. 从 L1 加载
                foreach (var sessionId in sessionIdList)
                {
                    if (_l1Cache.TryGetValue(sessionId, out var session) && !session.IsExpired)
                    {
                        result.Add(session);
                    }
                    else
                    {
                        missingIds.Add(sessionId);
                    }
                }

                // 2. 从 L2 和 L3 加载缺失的会话
                if (missingIds.Count > 0)
                {
                    if (_l2Store != null)
                    {
                        var l2Sessions = await _l2Store.LoadBatchAsync(missingIds, cancellationToken);
                        foreach (var session in l2Sessions)
                        {
                            if (session != null && !session.IsExpired)
                            {
                                result.Add(session);
                                _l1Cache[session.SessionId] = session; // 回填 L1
                                missingIds.Remove(session.SessionId);
                            }
                        }
                    }

                    if (_l3Store != null && missingIds.Count > 0)
                    {
                        var l3Sessions = await _l3Store.LoadBatchAsync(missingIds, cancellationToken);
                        foreach (var session in l3Sessions)
                        {
                            if (session != null && !session.IsExpired)
                            {
                                result.Add(session);
                                _l1Cache[session.SessionId] = session; // 回填 L1
                            }
                        }
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SessionManager] Failed to load batch sessions");
                return new List<MafSessionState>();
            }
        }

        /// <summary>
        /// 清理 L1 缓存中的过期会话（内存管理）
        /// </summary>
        public void CleanupL1Cache()
        {
            var expiredSessions = _l1Cache.Where(kvp => kvp.Value.IsExpired).Select(kvp => kvp.Key).ToList();
            foreach (var sessionId in expiredSessions)
            {
                _l1Cache.Remove(sessionId);
            }

            // 如果缓存大小超过限制，移除最旧的会话
            if (_l1Cache.Count > _maxL1CacheSize)
            {
                var sessionsToRemove = _l1Cache
                    .OrderBy(kvp => kvp.Value.LastActivityAt)
                    .Take(_l1Cache.Count - _maxL1CacheSize)
                    .Select(kvp => kvp.Key)
                    .ToList();

                foreach (var sessionId in sessionsToRemove)
                {
                    _l1Cache.Remove(sessionId);
                }

                _logger.LogDebug("[SessionManager] Cleaned up L1 cache: removed {Count} old sessions", sessionsToRemove.Count);
            }
        }

        /// <summary>
        /// 获取 L1 缓存统计信息
        /// </summary>
        public (int Count, int ActiveCount, int ExpiredCount) GetL1CacheStats()
        {
            var sessions = _l1Cache.Values.ToList();
            return (
                Count: sessions.Count,
                ActiveCount: sessions.Count(s => s.IsActive),
                ExpiredCount: sessions.Count(s => s.IsExpired)
            );
        }
    }
}
