using CKY.MultiAgentFramework.Core.Models.Task;

namespace CKY.MultiAgentFramework.Core.Models.Dialog
{
    /// <summary>
    /// 对话上下文，包含会话历史和用户偏好信息
    /// Dialog context containing session history and user preferences
    /// </summary>
    public class DialogContext
    {
        /// <summary>
        /// 会话ID
        /// Session ID
        /// </summary>
        public string SessionId { get; set; } = string.Empty;

        /// <summary>
        /// 用户ID
        /// User ID
        /// </summary>
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// 历史槽位值（用于推断用户偏好）
        /// Historical slot values for inferring user preferences
        /// Key: Intent+SlotName (e.g., "control_device.Location"), Value: historical value
        /// </summary>
        public Dictionary<string, object> HistoricalSlots { get; set; } = new();

        /// <summary>
        /// 对话轮次计数
        /// Dialog turn count
        /// </summary>
        public int TurnCount { get; set; }

        /// <summary>
        /// 上一次的意图
        /// Previous intent
        /// </summary>
        public string? PreviousIntent { get; set; }

        /// <summary>
        /// 上一次的槽位值
        /// Previous slot values
        /// </summary>
        public Dictionary<string, object>? PreviousSlots { get; set; }

        /// <summary>
        /// 待处理的澄清信息
        /// Pending clarification information
        /// </summary>
        public PendingClarificationInfo? PendingClarification { get; set; }

        /// <summary>
        /// 待处理的任务计划（SubAgent槽位缺失时）
        /// Pending task plan when SubAgent slots are missing
        /// </summary>
        public PendingTaskInfo? PendingTask { get; set; }

        /// <summary>
        /// 创建时间
        /// Creation time
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 最后更新时间
        /// Last update time
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// 待处理的澄清信息
    /// Pending clarification information
    /// </summary>
    public class PendingClarificationInfo
    {
        /// <summary>意图</summary>
        public string Intent { get; set; } = string.Empty;

        /// <summary>已检测到的槽位</summary>
        public Dictionary<string, object> DetectedSlots { get; set; } = new();

        /// <summary>缺失的槽位定义</summary>
        public List<SlotDefinition> MissingSlots { get; set; } = new();

        /// <summary>创建时间</summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// 待处理的任务信息
    /// Pending task information
    /// </summary>
    public class PendingTaskInfo
    {
        /// <summary>执行计划</summary>
        public ExecutionPlan Plan { get; set; } = null!;

        /// <summary>已填充的槽位</summary>
        public Dictionary<string, object> FilledSlots { get; set; } = new();

        /// <summary>仍然缺失的槽位</summary>
        public List<SlotDefinition> StillMissing { get; set; } = new();

        /// <summary>创建时间</summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
