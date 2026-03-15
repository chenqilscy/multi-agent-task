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
        public string Key { get; set; } = string.Empty;
        public object Value { get; set; } = new();
        public TimeSpan Expiry { get; set; } = TimeSpan.FromHours(24);
        public string Reason { get; set; } = string.Empty;
    }

    /// <summary>
    /// 长期记忆
    /// Long-term memory
    /// </summary>
    public class LongTermMemory
    {
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public double ImportanceScore { get; set; }
        public List<string> Tags { get; set; } = new();
        public string Reason { get; set; } = string.Empty;
    }

    /// <summary>
    /// 遗忘候选
    /// Forgetting candidate
    /// </summary>
    public class ForgettingCandidate
    {
        public string MemoryId { get; set; } = string.Empty;
        public ForgettingDecision Decision { get; set; }
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
