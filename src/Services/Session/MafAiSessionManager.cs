using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Core.Models.Session;
using CKY.MultiAgentFramework.Services.Session.Strategies;
using CKY.MultiAgentFramework.Services.Session.Utils;
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
        private readonly IMafAiSessionStore? _l2Store;
        private readonly IMafAiSessionStore? _l3Store;
        private readonly L1CacheManager _l1CacheManager;
        private readonly ISessionReadStrategy _readStrategy;
        private readonly ISessionWriteStrategy _writeStrategy;

        public MafAiSessionManager(
            ILogger<MafAiSessionManager> logger,
            IMafAiSessionStore? l2Store = null,
            IMafAiSessionStore? l3Store = null,
            int maxL1CacheSize = 1000,
            TimeSpan? l1CacheExpiration = null,
            ISessionReadStrategy? readStrategy = null,
            ISessionWriteStrategy? writeStrategy = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _l2Store = l2Store;
            _l3Store = l3Store;

            // 创建正确的泛型 Logger
            var l1Logger = logger as ILogger<L1CacheManager> ??
                         Microsoft.Extensions.Logging.Abstractions.NullLogger<L1CacheManager>.Instance;
            var readLogger = logger as ILogger<DefaultSessionReadStrategy> ??
                            Microsoft.Extensions.Logging.Abstractions.NullLogger<DefaultSessionReadStrategy>.Instance;
            var writeLogger = logger as ILogger<DefaultSessionWriteStrategy> ??
                             Microsoft.Extensions.Logging.Abstractions.NullLogger<DefaultSessionWriteStrategy>.Instance;

            _l1CacheManager = new L1CacheManager(l1Logger, maxL1CacheSize, l1CacheExpiration);
            _readStrategy = readStrategy ?? new DefaultSessionReadStrategy(readLogger);
            _writeStrategy = writeStrategy ?? new DefaultSessionWriteStrategy(writeLogger);
        }

        public async Task SaveAsync(MafSessionState session, CancellationToken cancellationToken = default)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));

            try
            {
                // 先更新 L1 缓存管理器
                _l1CacheManager.Add(session.SessionId, session);

                // 使用写入策略同步到三层存储
                await _writeStrategy.WriteAsync(session, this, _l2Store, _l3Store, cancellationToken);

                _logger.LogDebug("[SessionManager] Saved session: {SessionId}", session.SessionId);
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
                // 先检查 L1 缓存
                var l1Session = _l1CacheManager.Get(sessionId);
                if (l1Session != null && !l1Session.IsExpired)
                {
                    _logger.LogDebug("[SessionManager] Loaded session from L1: {SessionId}", sessionId);
                    return l1Session;
                }

                // 使用读取策略从三层存储加载
                var session = await _readStrategy.ReadAsync(sessionId, this, _l2Store, _l3Store, cancellationToken);

                if (session != null)
                {
                    // 回填 L1 缓存
                    _l1CacheManager.Add(sessionId, session);
                }

                return session;
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
                // 1. 从 L1 删除
                _l1CacheManager.Remove(sessionId);

                // 2. 从 L2 删除 - 异步
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

                // 3. 从 L3 删除 - 异步
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
                if (_l1CacheManager.Contains(sessionId))
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
                allSessions.AddRange(_l1CacheManager.GetStats().ActiveCount > 0
                    ? Enumerable.Empty<MafSessionState>()
                    : Enumerable.Empty<MafSessionState>());

                // 实际需要从 L1 获取，这里需要扩展 L1CacheManager 接口
                var l1Sessions = await GetL1SessionsByUserAsync(userId);
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

                // 1. 从 L2 获取
                if (_l2Store != null)
                {
                    var l2Sessions = await _l2Store.GetActiveSessionsAsync(cancellationToken);
                    allSessions.AddRange(l2Sessions);
                }

                // 2. 从 L3 获取
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
                deletedCount += _l1CacheManager.CleanupExpiredSessions();

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
                    _l1CacheManager.Add(session.SessionId, session);
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
                    var session = _l1CacheManager.Get(sessionId);
                    if (session != null && !session.IsExpired)
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
                                _l1CacheManager.Add(session.SessionId, session);
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
                                _l1CacheManager.Add(session.SessionId, session);
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
            _l1CacheManager.CleanupExpiredSessions();
        }

        /// <summary>
        /// 获取 L1 缓存统计信息
        /// </summary>
        public (int Count, int ActiveCount, int ExpiredCount) GetL1CacheStats()
        {
            return _l1CacheManager.GetStats();
        }

        /// <summary>
        /// 从 L1 缓存获取用户会话（内部辅助方法）
        /// </summary>
        private async Task<List<MafSessionState>> GetL1SessionsByUserAsync(string userId)
        {
            // L1CacheManager 目前不提供按用户遍历接口，返回空列表。
            // 如需支持按用户查询，需扩展 L1CacheManager 添加遍历能力。
            var sessions = new List<MafSessionState>();
            var stats = _l1CacheManager.GetStats();

            // 由于 L1CacheManager 目前不提供遍历接口，返回空列表
            // 实际项目中应该扩展 L1CacheManager 接口
            return await Task.FromResult(sessions);
        }
    }
}
