using CKY.MultiAgentFramework.Core.Models.Session;

namespace CKY.MultiAgentFramework.Core.Abstractions
{
    /// <summary>
    /// MAF Agent 会话状态存储接口
    /// </summary>
    /// <remarks>
    /// 职责：
    /// - 保存会话状态到持久化存储
    /// - 从持久化存储加载会话状态
    /// - 删除过期的会话状态
    /// - 支持会话的查询和枚举
    ///
    /// 存储策略：
    /// - L1 (内存): 快速访问，当前活跃会话
    /// - L2 (Redis): 分布式缓存，24小时 TTL
    /// - L3 (数据库): 长期存储，用于分析和恢复
    ///
    /// 注意：
    /// - 存储的是 MafSessionState（纯数据模型）
    /// - 与 MS AF 的 AgentSession 完全分离
    /// </remarks>
    public interface IMafAiSessionStore
    {
        /// <summary>
        /// 保存会话状态
        /// </summary>
        /// <param name="session">会话状态</param>
        /// <param name="cancellationToken">取消令牌</param>
        Task SaveAsync(MafSessionState session, CancellationToken cancellationToken = default);

        /// <summary>
        /// 加载会话状态
        /// </summary>
        /// <param name="sessionId">会话 ID</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>会话状态，不存在则返回 null</returns>
        Task<MafSessionState?> LoadAsync(string sessionId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 删除会话状态
        /// </summary>
        /// <param name="sessionId">会话 ID</param>
        /// <param name="cancellationToken">取消令牌</param>
        Task DeleteAsync(string sessionId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 检查会话是否存在
        /// </summary>
        /// <param name="sessionId">会话 ID</param>
        /// <param name="cancellationToken">取消令牌</param>
        Task<bool> ExistsAsync(string sessionId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取用户的所有会话
        /// </summary>
        /// <param name="userId">用户 ID</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>会话列表</returns>
        Task<List<MafSessionState>> GetSessionsByUserAsync(string userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取所有活跃会话
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>会话列表</returns>
        Task<List<MafSessionState>> GetActiveSessionsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 删除所有过期会话
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>删除的会话数量</returns>
        Task<int> DeleteExpiredSessionsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 批量保存会话
        /// </summary>
        Task SaveBatchAsync(IEnumerable<MafSessionState> sessions, CancellationToken cancellationToken = default);

        /// <summary>
        /// 批量加载会话
        /// </summary>
        Task<List<MafSessionState>> LoadBatchAsync(IEnumerable<string> sessionIds, CancellationToken cancellationToken = default);
    }
}
