namespace CKY.MultiAgentFramework.Core.Models.Dialog
{
    /// <summary>
    /// 记忆分类结果
    /// Memory classification result
    /// </summary>
    public class MemoryClassificationResult
    {
        /// <summary>短期记忆列表</summary>
        public List<ShortTermMemory> ShortTermMemories { get; set; } = new();

        /// <summary>长期记忆列表</summary>
        public List<LongTermMemory> LongTermMemories { get; set; } = new();

        /// <summary>遗忘候选列表</summary>
        public List<ForgettingCandidate> ForgettingCandidates { get; set; } = new();
    }

    /// <summary>
    /// 短期记忆
    /// Short-term memory
    /// </summary>
    public class ShortTermMemory
    {
        /// <summary>记忆键（格式：intent.slot）</summary>
        public string Key { get; set; } = string.Empty;

        /// <summary>记忆值</summary>
        public object Value { get; set; } = new();

        /// <summary>过期时间（默认24小时）</summary>
        public TimeSpan Expiry { get; set; } = TimeSpan.FromHours(24);

        /// <summary>分类原因</summary>
        public string Reason { get; set; } = string.Empty;
    }

    /// <summary>
    /// 长期记忆
    /// Long-term memory
    /// </summary>
    public class LongTermMemory
    {
        /// <summary>记忆键（格式：intent.slot）</summary>
        public string Key { get; set; } = string.Empty;

        /// <summary>记忆值</summary>
        public string Value { get; set; } = string.Empty;

        /// <summary>重要性评分（0-1）</summary>
        public double ImportanceScore { get; set; }

        /// <summary>标签列表</summary>
        public List<string> Tags { get; set; } = new();

        /// <summary>分类原因</summary>
        public string Reason { get; set; } = string.Empty;
    }

    /// <summary>
    /// 遗忘候选
    /// Forgetting candidate
    /// </summary>
    public class ForgettingCandidate
    {
        /// <summary>记忆ID</summary>
        public string MemoryId { get; set; } = string.Empty;

        /// <summary>遗忘决策</summary>
        public ForgettingDecision Decision { get; set; }

        /// <summary>决策原因</summary>
        public string Reason { get; set; } = string.Empty;
    }

    /// <summary>
    /// 遗忘决策
    /// Forgetting decision
    /// </summary>
    public enum ForgettingDecision
    {
        /// <summary>保留</summary>
        Keep,

        /// <summary>降级</summary>
        Downgrade,

        /// <summary>标记待清理</summary>
        MarkForCleanup,

        /// <summary>删除</summary>
        Delete
    }
}
