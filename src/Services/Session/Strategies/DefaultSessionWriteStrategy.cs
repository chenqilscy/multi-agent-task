using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Core.Models.Session;
using Microsoft.Extensions.Logging;

namespace CKY.MultiAgentFramework.Services.Session.Strategies
{
    /// <summary>
    /// 默认会话写入策略实现（同步 L1 + 异步 L2/L3）
    /// </summary>
    /// <remarks>
    /// 写入策略：
    /// 1. 同步写入 L1（内存）- 确保数据立即可用
    /// 2. 异步写入 L2（Redis）- 分布式缓存，用于跨节点访问
    /// 3. 异步写入 L3（数据库）- 持久化存储，用于长期保存
    ///
    /// 容错机制：
    /// - L2/L3 写入失败不影响主流程
    /// - 失败时记录警告日志
    /// - 保证 L1 写入成功即可返回
    ///
    /// 性能优化：
    /// - L2/L3 并行异步写入，不阻塞主线程
    /// - 使用 Task.Run 避免阻塞调用方
    /// </remarks>
    public class DefaultSessionWriteStrategy : ISessionWriteStrategy
    {
        private readonly ILogger<DefaultSessionWriteStrategy> _logger;

        public DefaultSessionWriteStrategy(ILogger<DefaultSessionWriteStrategy> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 将会话写入三层缓存（同步 L1 + 异步 L2/L3）
        /// </summary>
        /// <param name="session">会话状态</param>
        /// <param name="l1Cache">L1 缓存（内存）</param>
        /// <param name="l2Store">L2 存储（Redis，可选）</param>
        /// <param name="l3Store">L3 存储（数据库，可选）</param>
        /// <param name="cancellationToken">取消令牌</param>
        public async Task WriteAsync(
            MafSessionState session,
            IL1SessionCache l1Cache,
            IMafAiSessionStore? l2Store,
            IMafAiSessionStore? l3Store,
            CancellationToken cancellationToken)
        {
            // 1. 保存到 L1（内存）- 同步，最高优先级
            l1Cache.Add(session.SessionId, session);

            // 2. 保存到 L2（Redis）- 异步，用于分布式访问
            if (l2Store != null)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await l2Store.SaveAsync(session, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "[WriteStrategy] Failed to save session to L2 (Redis): {SessionId}", session.SessionId);
                    }
                }, cancellationToken);
            }

            // 3. 保存到 L3（数据库）- 异步，用于长期存储
            if (l3Store != null)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await l3Store.SaveAsync(session, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "[WriteStrategy] Failed to save session to L3 (Database): {SessionId}", session.SessionId);
                    }
                }, cancellationToken);
            }

            _logger.LogDebug("[WriteStrategy] Saved session to L1: {SessionId}", session.SessionId);
        }
    }
}
