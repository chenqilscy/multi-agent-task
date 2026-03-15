namespace CKY.MultiAgentFramework.Core.Models.Dialog
{
    /// <summary>
    /// 上下文压缩结果
    /// Context compression result
    /// </summary>
    public class ContextCompressionResult
    {
        /// <summary>
        /// 压缩后的摘要
        /// Compressed summary
        /// </summary>
        public string Summary { get; set; } = string.Empty;

        /// <summary>
        /// 提取的关键信息
        /// Extracted key information
        /// </summary>
        public List<KeyInformation> KeyInfos { get; set; } = new();

        /// <summary>
        /// 原始消息数量
        /// Original message count
        /// </summary>
        public int OriginalMessageCount { get; set; }

        /// <summary>
        /// 压缩后消息数量
        /// Compressed message count
        /// </summary>
        public int CompressedMessageCount { get; set; }

        /// <summary>
        /// 压缩比例
        /// Compression ratio
        /// </summary>
        public double CompressionRatio { get; set; }
    }

    /// <summary>
    /// 关键信息
    /// Key information
    /// </summary>
    public class KeyInformation
    {
        /// <summary>
        /// 信息类型
        /// Information type
        /// "Preference", "Decision", "Fact"
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// 信息内容
        /// Information content
        /// </summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// 重要性评分 (0-1)
        /// Importance score (0-1)
        /// </summary>
        public double Importance { get; set; }

        /// <summary>
        /// 标签
        /// Tags
        /// </summary>
        public List<string> Tags { get; set; } = new();
    }
}
