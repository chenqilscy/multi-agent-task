namespace CKY.MultiAgentFramework.Core.Models.Session
{
    /// <summary>
    /// MAF Agent 会话状态数据
    /// 用于存储 AI Agent 的对话状态和上下文信息
    /// </summary>
    /// <remarks>
    /// 设计原则：
    /// - 纯数据模型（POCO），不继承任何框架类
    /// - 可独立序列化和持久化
    /// - 与 MS AF 的 AgentSession 完全分离
    ///
    /// 功能特性：
    /// - 会话标识：唯一标识一个会话
    /// - 对话历史：存储多轮对话记录
    /// - 元数据：存储会话级别的自定义数据
    /// - 生命周期管理：创建时间、过期时间、活跃时间
    /// - Token 统计：跟踪 Token 使用量
    ///
    /// 使用方式：
    /// - 由 MafAgentSession 持有和操作
    /// - 通过 IMafAiSessionStore 进行持久化
    /// - 可独立于 MS AF 框架使用
    /// </remarks>
    public class MafSessionState
    {
        /// <summary>
        /// 会话唯一标识符
        /// </summary>
        public string SessionId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// 用户标识符（可选，用于关联用户）
        /// </summary>
        public string? UserId { get; set; }

        /// <summary>
        /// 会话元数据（用于存储自定义数据）
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new();

        /// <summary>
        /// 会话数据（兼容 MS AF 的 AgentSession）
        /// </summary>
        public IDictionary<string, object>? Items { get; set; }

        /// <summary>
        /// 会话创建时间
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 最后活跃时间
        /// </summary>
        public DateTime LastActivityAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 会话过期时间（可选，null 表示永不过期）
        /// </summary>
        public DateTime? ExpiresAt { get; set; }

        /// <summary>
        /// 累计 Token 使用量
        /// </summary>
        public long TotalTokensUsed { get; set; }

        /// <summary>
        /// 对话轮次计数
        /// </summary>
        public int TurnCount { get; set; }

        /// <summary>
        /// 会话状态
        /// </summary>
        public SessionStatus Status { get; set; } = SessionStatus.Active;

        /// <summary>
        /// 检查会话是否已过期
        /// </summary>
        public bool IsExpired => ExpiresAt.HasValue && DateTime.UtcNow > ExpiresAt.Value;

        /// <summary>
        /// 检查会话是否活跃
        /// </summary>
        public bool IsActive => Status == SessionStatus.Active && !IsExpired;

        /// <summary>
        /// 更新活跃时间
        /// </summary>
        public void UpdateActivity()
        {
            LastActivityAt = DateTime.UtcNow;
        }

        /// <summary>
        /// 增加 Token 使用量
        /// </summary>
        public void AddTokens(int tokenCount)
        {
            TotalTokensUsed += tokenCount;
        }

        /// <summary>
        /// 增加对话轮次
        /// </summary>
        public void IncrementTurn()
        {
            TurnCount++;
        }

        /// <summary>
        /// 标记会话为已结束
        /// </summary>
        public void MarkAsCompleted()
        {
            Status = SessionStatus.Completed;
            UpdateActivity();
        }

        /// <summary>
        /// 标记会话为已暂停
        /// </summary>
        public void MarkAsSuspended()
        {
            Status = SessionStatus.Suspended;
            UpdateActivity();
        }

        /// <summary>
        /// 恢复已暂停的会话
        /// </summary>
        public void Resume()
        {
            if (Status == SessionStatus.Suspended)
            {
                Status = SessionStatus.Active;
                UpdateActivity();
            }
        }

        /// <summary>
        /// 获取会话持续时间
        /// </summary>
        public TimeSpan GetDuration() => DateTime.UtcNow - CreatedAt;

        /// <summary>
        /// 获取会话闲置时间
        /// </summary>
        public TimeSpan GetIdleTime() => DateTime.UtcNow - LastActivityAt;

        /// <summary>
        /// 获取会话摘要（用于日志和调试）
        /// </summary>
        public string GetSummary()
        {
            return $"Session[{SessionId}] User={UserId} Status={Status} Turns={TurnCount} Tokens={TotalTokensUsed} Duration={GetDuration():hh\\:mm\\:ss}";
        }
    }

    /// <summary>
    /// 会话状态枚举
    /// </summary>
    public enum SessionStatus
    {
        /// <summary>活跃中</summary>
        Active,

        /// <summary>已暂停（可恢复）</summary>
        Suspended,

        /// <summary>已完成</summary>
        Completed,

        /// <summary>已取消</summary>
        Cancelled,

        /// <summary>已过期</summary>
        Expired,

        /// <summary>错误状态</summary>
        Error
    }
}
