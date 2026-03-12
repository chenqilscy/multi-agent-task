namespace CKY.MultiAgentFramework.Core.Abstractions
{
    /// <summary>
    /// 情景记忆
    /// </summary>
    public class EpisodicMemory
    {
        /// <summary>记忆ID</summary>
        public string MemoryId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>记忆摘要</summary>
        public string Summary { get; set; } = string.Empty;

        /// <summary>记录时间</summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>标签列表</summary>
        public List<string> Tags { get; set; } = new();
    }

    /// <summary>
    /// 语义记忆
    /// </summary>
    public class SemanticMemory
    {
        /// <summary>记忆键</summary>
        public string Key { get; set; } = string.Empty;

        /// <summary>记忆值</summary>
        public string Value { get; set; } = string.Empty;

        /// <summary>向量嵌入</summary>
        public float[]? Embedding { get; set; }

        /// <summary>标签列表</summary>
        public List<string> Tags { get; set; } = new();
    }

    /// <summary>
    /// 记忆上下文
    /// </summary>
    public class MemoryContext
    {
        /// <summary>情景记忆列表</summary>
        public List<EpisodicMemory> EpisodicMemories { get; set; } = new();

        /// <summary>语义记忆列表</summary>
        public List<SemanticMemory> SemanticMemories { get; set; } = new();

        /// <summary>工作记忆（短期状态）</summary>
        public Dictionary<string, object> WorkingMemory { get; set; } = new();
    }

    /// <summary>
    /// 记忆管理接口
    /// 依赖 IVectorStore 和 IRelationalDatabase 抽象接口
    /// </summary>
    public interface IMafMemoryManager
    {
        /// <summary>
        /// 获取相关记忆
        /// </summary>
        Task<MemoryContext> GetRelevantMemoryAsync(
            string userId,
            string query,
            CancellationToken ct = default);

        /// <summary>
        /// 保存情景记忆
        /// </summary>
        Task SaveEpisodicMemoryAsync(
            string userId,
            string conversationId,
            string summary,
            CancellationToken ct = default);

        /// <summary>
        /// 保存语义记忆
        /// </summary>
        Task SaveSemanticMemoryAsync(
            string userId,
            string key,
            string value,
            List<string> tags,
            CancellationToken ct = default);
    }
}
