using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Core.Models.Session;
using Microsoft.Extensions.Logging;

namespace CKY.MultiAgentFramework.Services.Session.Strategies
{
    /// <summary>
    /// 默认会话读取策略实现（三层缓存：L1 → L2 → L3）
    /// </summary>
    /// <remarks>
    /// 读取策略：
    /// 1. 优先从 L1（内存）读取 - 最快访问
    /// 2. 未命中时从 L2（Redis）读取 - 分布式缓存
    /// 3. 仍未命中时从 L3（数据库）读取 - 持久化存储
    ///
    /// 回填机制：
    /// - 从 L2 加载后，回填到 L1
    /// - 从 L3 加载后，回填到 L1 和 L2（异步）
    ///
    /// 设计原则：
    /// - 确保数据一致性（检查过期状态）
    /// - 优化访问速度（优先使用缓存）
    /// - 容错处理（L2 回填失败不影响主流程）
    /// </remarks>
    public class DefaultSessionReadStrategy : ISessionReadStrategy
    {
        private readonly ILogger<DefaultSessionReadStrategy> _logger;

        public DefaultSessionReadStrategy(ILogger<DefaultSessionReadStrategy> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 从三层缓存中读取会话（L1 → L2 → L3）
        /// </summary>
        /// <param name="sessionId">会话 ID</param>
        /// <param name="l1Cache">L1 缓存（内存）</param>
        /// <param name="l2Store">L2 存储（Redis，可选）</param>
        /// <param name="l3Store">L3 存储（数据库，可选）</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>会话状态，如果不存在或已过期则返回 null</returns>
        public async Task<MafSessionState?> ReadAsync(
            string sessionId,
            IL1SessionCache l1Cache,
            IMafAiSessionStore? l2Store,
            IMafAiSessionStore? l3Store,
            CancellationToken cancellationToken)
        {
            // 1. 尝试从 L1（内存）加载
            var l1Session = l1Cache.Get(sessionId);
            if (l1Session != null && !l1Session.IsExpired)
            {
                _logger.LogDebug("[ReadStrategy] Loaded session from L1: {SessionId}", sessionId);
                return l1Session;
            }

            // 2. 尝试从 L2（Redis）加载
            if (l2Store != null)
            {
                var l2Session = await l2Store.LoadAsync(sessionId, cancellationToken);
                if (l2Session != null && !l2Session.IsExpired)
                {
                    // 回填 L1
                    l1Cache.Add(sessionId, l2Session);
                    _logger.LogDebug("[ReadStrategy] Loaded session from L2 (Redis): {SessionId}", sessionId);
                    return l2Session;
                }
            }

            // 3. 尝试从 L3（数据库）加载
            if (l3Store != null)
            {
                var l3Session = await l3Store.LoadAsync(sessionId, cancellationToken);
                if (l3Session != null && !l3Session.IsExpired)
                {
                    // 回填 L1 和 L2
                    l1Cache.Add(sessionId, l3Session);
                    if (l2Store != null)
                    {
                        await BackfillL2Async(sessionId, l3Session, l2Store, cancellationToken);
                    }
                    _logger.LogDebug("[ReadStrategy] Loaded session from L3 (Database): {SessionId}", sessionId);
                    return l3Session;
                }
            }

            _logger.LogDebug("[ReadStrategy] Session not found: {SessionId}", sessionId);
            return null;
        }

        /// <summary>
        /// 回填 L2 缓存（内部辅助方法）
        /// </summary>
        /// <param name="sessionId">会话 ID</param>
        /// <param name="session">会话状态</param>
        /// <param name="l2Store">L2 存储</param>
        /// <param name="ct">取消令牌</param>
        private async Task BackfillL2Async(
            string sessionId,
            MafSessionState session,
            IMafAiSessionStore l2Store,
            CancellationToken ct)
        {
            try
            {
                await l2Store.SaveAsync(session, ct);
            }
            catch (Exception ex)
            {
                // L2 回填失败不影响主流程
                _logger.LogWarning(ex, "[ReadStrategy] Failed to backfill L2 for session: {SessionId}", sessionId);
            }
        }
    }
}
