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
    }
}
