namespace CKY.MultiAgentFramework.Core.Models.Dialog
{
    /// <summary>
    /// 澄清策略枚举
    /// Clarification strategy enumeration
    /// </summary>
    public enum ClarificationStrategy
    {
        /// <summary>
        /// 模板澄清（高频场景）
        /// Template-based clarification (high-frequency scenarios)
        /// </summary>
        Template,

        /// <summary>
        /// 智能推断（使用历史偏好）
        /// Smart inference (using historical preferences)
        /// </summary>
        SmartInference,

        /// <summary>
        /// LLM生成（复杂场景）
        /// LLM-generated (complex scenarios)
        /// </summary>
        LLM,

        /// <summary>
        /// 混合模式
        /// Hybrid mode
        /// </summary>
        Hybrid
    }

    /// <summary>
    /// 澄清分析结果
    /// Clarification analysis result
    /// </summary>
    public class ClarificationAnalysis
    {
        /// <summary>
        /// 是否需要澄清
        /// Whether clarification is needed
        /// </summary>
        public bool NeedsClarification { get; set; }

        /// <summary>
        /// 缺失的槽位列表
        /// List of missing slots
        /// </summary>
        public List<SlotDefinition> MissingSlots { get; set; } = new();

        /// <summary>
        /// 优先级槽位列表（按依赖关系和用户习惯排序）
        /// Priority slot list (sorted by dependency and user preferences)
        /// </summary>
        public List<SlotDefinition> PrioritySlots { get; set; } = new();

        /// <summary>
        /// 选择的澄清策略
        /// Selected clarification strategy
        /// </summary>
        public ClarificationStrategy Strategy { get; set; }

        /// <summary>
        /// 置信度分数（0.0-1.0）
        /// Confidence score
        /// </summary>
        public double Confidence { get; set; }

        /// <summary>
        /// 预估需要的对话轮次
        /// Estimated number of dialog turns required
        /// </summary>
        public int EstimatedTurns { get; set; }

        /// <summary>
        /// 对话上下文
        /// Dialog context
        /// </summary>
        public DialogContext? Context { get; set; }

        /// <summary>
        /// 建议的槽位值（基于历史偏好推断）
        /// Suggested slot values (inferred from historical preferences)
        /// </summary>
        public Dictionary<string, object> SuggestedValues { get; set; } = new();

        /// <summary>
        /// 是否需要用户确认
        /// Whether user confirmation is required
        /// </summary>
        public bool RequiresConfirmation { get; set; }
    }

    /// <summary>
    /// 澄清上下文，用于跟踪多轮澄清对话
    /// Clarification context for tracking multi-turn clarification dialog
    /// </summary>
    public class ClarificationContext
    {
        /// <summary>
        /// 会话ID
        /// Session ID
        /// </summary>
        public string SessionId { get; set; } = string.Empty;

        /// <summary>
        /// 意图
        /// Intent
        /// </summary>
        public string Intent { get; set; } = string.Empty;

        /// <summary>
        /// 缺失的槽位列表
        /// List of missing slots
        /// </summary>
        public List<SlotDefinition> MissingSlots { get; set; } = new();

        /// <summary>
        /// 已填充的槽位
        /// Filled slots
        /// </summary>
        public Dictionary<string, object> FilledSlots { get; set; } = new();

        /// <summary>
        /// 对话轮次计数
        /// Dialog turn count
        /// </summary>
        public int TurnCount { get; set; }

        /// <summary>
        /// 当前使用的澄清策略
        /// Current clarification strategy
        /// </summary>
        public ClarificationStrategy Strategy { get; set; }

        /// <summary>
        /// 上一次询问的槽位
        /// Last asked slot
        /// </summary>
        public SlotDefinition? LastAskedSlot { get; set; }

        /// <summary>
        /// 是否已完成澄清
        /// Whether clarification is completed
        /// </summary>
        public bool IsCompleted { get; set; }
    }

    /// <summary>
    /// 澄清响应，表示用户对澄清问题的响应
    /// Clarification response representing user's response to clarification question
    /// </summary>
    public class ClarificationResponse
    {
        /// <summary>
        /// 是否已完成所有槽位填充
        /// Whether all slot filling is completed
        /// </summary>
        public bool Completed { get; set; }

        /// <summary>
        /// 更新的槽位值
        /// Updated slot values
        /// </summary>
        public Dictionary<string, object> UpdatedSlots { get; set; } = new();

        /// <summary>
        /// 仍然缺失的槽位
        /// Still missing slots
        /// </summary>
        public List<SlotDefinition> StillMissing { get; set; } = new();

        /// <summary>
        /// 响应消息
        /// Response message
        /// </summary>
        public string? Message { get; set; }

        /// <summary>
        /// 是否需要进一步澄清
        /// Whether further clarification is needed
        /// </summary>
        public bool NeedsFurtherClarification { get; set; }

        /// <summary>
        /// 用户确认的建议值
        /// User-confirmed suggested values
        /// </summary>
        public Dictionary<string, object> ConfirmedSuggestions { get; set; } = new();
    }
}
