using CKY.MultiAgentFramework.Core.Models.Session;

namespace CKY.MultiAgentFramework.Core.Abstractions
{
    /// <summary>
    /// L1 会话缓存接口（内存层专用）
    /// </summary>
    /// <remarks>
    /// 与 <see cref="IMafAiSessionStore"/> 分离，避免 MafAiSessionManager 将自身
    /// 作为 L1 传给写入策略时产生递归调用。
    ///
    /// 职责：
    /// - 提供同步的内存级会话存取
    /// - 缓存容量管理和过期清理
    /// - 缓存统计信息
    /// </remarks>
    public interface IL1SessionCache
    {
        /// <summary>从缓存获取会话</summary>
        MafSessionState? Get(string sessionId);

        /// <summary>添加或更新缓存中的会话</summary>
        void Add(string sessionId, MafSessionState session);

        /// <summary>从缓存移除会话</summary>
        bool Remove(string sessionId);

        /// <summary>检查缓存是否包含指定会话</summary>
        bool Contains(string sessionId);

        /// <summary>清理过期会话并执行容量控制</summary>
        int CleanupExpiredSessions();

        /// <summary>获取缓存统计信息</summary>
        (int Count, int ActiveCount, int ExpiredCount) GetStats();

        /// <summary>按用户ID获取会话列表</summary>
        List<MafSessionState> GetByUserId(string userId);
    }
}
