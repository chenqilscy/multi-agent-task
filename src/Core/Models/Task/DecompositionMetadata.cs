namespace CKY.MultiAgentFramework.Core.Models.Task
{
    /// <summary>
    /// 分解元数据
    /// </summary>
    public class DecompositionMetadata
    {
        /// <summary>分解时间</summary>
        public DateTime DecomposedAt { get; set; } = DateTime.UtcNow;

        /// <summary>分解用时（毫秒）</summary>
        public long ElapsedMs { get; set; }

        /// <summary>使用的策略</summary>
        public string? Strategy { get; set; }
    }
}
