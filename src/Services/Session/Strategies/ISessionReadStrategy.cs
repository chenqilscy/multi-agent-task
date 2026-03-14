using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Core.Models.Session;

namespace CKY.MultiAgentFramework.Services.Session.Strategies
{
    /// <summary>
    /// 会话读取策略接口（三层缓存：L1 → L2 → L3）
    /// </summary>
    /// <remarks>
    /// 定义会话读取的标准策略，实现类应：
    /// 1. 按优先级从 L1 → L2 → L3 逐级查找
    /// 2. 支持缓存回填（将上层缓存的数据回填到下层）
    /// 3. 检查会话过期状态，返回有效会话
    /// 4. 处理异常情况，确保容错性
    ///
    /// 设计优势：
    /// - 策略模式：可替换不同的读取策略
    /// - 单一职责：专注读取逻辑
    /// - 易于测试：可使用 Mock 策略进行单元测试
    /// </remarks>
    public interface ISessionReadStrategy
    {
        /// <summary>
        /// 从三层缓存中读取会话
        /// </summary>
        /// <param name="sessionId">会话 ID</param>
        /// <param name="l1Cache">L1 缓存（内存）</param>
        /// <param name="l2Store">L2 存储（Redis，可选）</param>
        /// <param name="l3Store">L3 存储（数据库，可选）</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>会话状态，如果不存在或已过期则返回 null</returns>
        Task<MafSessionState?> ReadAsync(
            string sessionId,
            IMafAiSessionStore l1Cache,
            IMafAiSessionStore? l2Store,
            IMafAiSessionStore? l3Store,
            CancellationToken cancellationToken);
    }
}
