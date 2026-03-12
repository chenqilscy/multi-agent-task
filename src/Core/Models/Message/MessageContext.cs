namespace CKY.MultiAgentFramework.Core.Models.Message
{
    /// <summary>
    /// 消息上下文 - CKY.MAF存储模型（用于会话持久化）
    /// 注意：运行时使用的是MS AF的消息类型系统
    /// </summary>
    public class MessageContext
    {
        /// <summary>消息唯一标识（GUID格式字符串）</summary>
        public string MessageId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>消息角色（如 "User"、"Assistant"、"System"）</summary>
        public string Role { get; set; } = string.Empty;

        /// <summary>
        /// 消息内容 - 支持多类型
        /// - text: 纯文本字符串
        /// - image: 图片URL或Base64数据
        /// - file: 文件路径
        /// - structured: JSON结构化数据
        /// </summary>
        public object? Content { get; set; }

        /// <summary>
        /// 内容类型标识
        /// 支持: "text", "image", "file", "audio", "video", "structured"
        /// </summary>
        public string ContentType { get; set; } = "text";

        /// <summary>消息时间戳（UTC）</summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>扩展元数据</summary>
        public Dictionary<string, object> Metadata { get; set; } = new();
    }
}
