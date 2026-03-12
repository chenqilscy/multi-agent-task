using CKY.MultiAgentFramework.Core.Models.Message;

namespace CKY.MultiAgentFramework.Core.Abstractions
{
    /// <summary>
    /// Agent会话接口
    /// </summary>
    public interface IAgentSession
    {
        /// <summary>会话ID</summary>
        string SessionId { get; }

        /// <summary>Agent ID</summary>
        string AgentId { get; }

        /// <summary>创建时间</summary>
        DateTime CreatedAt { get; }

        /// <summary>最后访问时间</summary>
        DateTime LastAccessedAt { get; }

        /// <summary>会话上下文</summary>
        Dictionary<string, object> Context { get; }

        /// <summary>消息历史</summary>
        List<MessageContext> MessageHistory { get; }
    }

    /// <summary>
    /// 会话存储接口
    /// 通过依赖抽象接口实现三层存储：L1内存、L2缓存、L3数据库
    /// </summary>
    public interface IMafSessionStorage
    {
        /// <summary>
        /// 加载会话
        /// </summary>
        Task<IAgentSession> LoadSessionAsync(
            string sessionId,
            CancellationToken ct = default);

        /// <summary>
        /// 保存会话
        /// </summary>
        Task SaveSessionAsync(
            IAgentSession session,
            CancellationToken ct = default);

        /// <summary>
        /// 删除会话
        /// </summary>
        Task DeleteSessionAsync(
            string sessionId,
            CancellationToken ct = default);

        /// <summary>
        /// 检查会话是否存在
        /// </summary>
        Task<bool> ExistsAsync(
            string sessionId,
            CancellationToken ct = default);
    }
}
