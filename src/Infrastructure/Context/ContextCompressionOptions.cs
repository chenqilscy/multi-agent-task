namespace CKY.MultiAgentFramework.Infrastructure.Context
{
    /// <summary>
    /// 上下文压缩配置选项
    /// </summary>
    public class ContextCompressionOptions
    {
        /// <summary>
        /// 最大 Token 数量阈值（默认 4000）
        /// </summary>
        public int MaxTokens { get; set; } = 4000;

        /// <summary>
        /// 是否启用压缩（默认 true）
        /// </summary>
        public bool EnableCompression { get; set; } = true;

        /// <summary>
        /// 压缩模式（默认 Smart）
        /// </summary>
        public CompressionMode Mode { get; set; } = CompressionMode.Smart;

        /// <summary>
        /// 最小保留消息数（默认 2，即至少保留 1 轮对话）
        /// </summary>
        public int MinMessagesToKeep { get; set; } = 2;
    }
}
