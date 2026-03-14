using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Core.Models.Session;

namespace CKY.MultiAgentFramework.Services.Session.Strategies
{
    /// <summary>
    /// 会话写入策略接口（同步 L1 + 异步 L2/L3）
    /// </summary>
    /// <remarks>
    /// 定义会话写入的标准策略，实现类应：
    /// 1. 同步写入 L1（内存），确保数据立即可用
    /// 2. 异步写入 L2（Redis），用于分布式访问
    /// 3. 异步写入 L3（数据库），用于长期存储
    /// 4. L2/L3 写入失败不影响主流程（容错处理）
    ///
    /// 设计优势：
    /// - 策略模式：可替换不同的写入策略
    /// - 性能优化：异步写入不阻塞主线程
    /// - 容错设计：L2/L3 失败不影响 L1 写入
    /// - 易于扩展：可支持不同的存储后端
    /// </remarks>
    public interface ISessionWriteStrategy
    {
        /// <summary>
        /// 将会话写入三层缓存
        /// </summary>
        /// <param name="session">会话状态</param>
        /// <param name="l1Cache">L1 缓存（内存）</param>
        /// <param name="l2Store">L2 存储（Redis，可选）</param>
        /// <param name="l3Store">L3 存储（数据库，可选）</param>
        /// <param name="cancellationToken">取消令牌</param>
        Task WriteAsync(
            MafSessionState session,
            IMafAiSessionStore l1Cache,
            IMafAiSessionStore? l2Store,
            IMafAiSessionStore? l3Store,
            CancellationToken cancellationToken);
    }
}
