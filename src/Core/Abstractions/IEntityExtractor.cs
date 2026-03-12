namespace CKY.MultiAgentFramework.Core.Abstractions
{
    /// <summary>
    /// 实体提取结果
    /// </summary>
    public class EntityExtractionResult
    {
        /// <summary>实体字典（类型 -> 值）</summary>
        public Dictionary<string, object> Entities { get; set; } = new();

        /// <summary>详细实体列表</summary>
        public List<Entity> ExtractedEntities { get; set; } = new();
    }

    /// <summary>
    /// 实体
    /// </summary>
    public class Entity
    {
        /// <summary>实体类型（Room、Device、Action、Value等）</summary>
        public string EntityType { get; set; } = string.Empty;

        /// <summary>实体值</summary>
        public string EntityValue { get; set; } = string.Empty;

        /// <summary>在原文中的起始位置</summary>
        public int StartPosition { get; set; }

        /// <summary>在原文中的结束位置</summary>
        public int EndPosition { get; set; }

        /// <summary>提取置信度（0.0-1.0）</summary>
        public double Confidence { get; set; }
    }

    /// <summary>
    /// 实体提取器接口
    /// </summary>
    public interface IEntityExtractor
    {
        /// <summary>
        /// 提取用户输入中的实体
        /// </summary>
        Task<EntityExtractionResult> ExtractAsync(
            string userInput,
            CancellationToken ct = default);
    }
}
