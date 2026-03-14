namespace CKY.MultiAgentFramework.Infrastructure.Vectorization.Qdrant
{
    /// <summary>
    /// Qdrant 向量存储配置选项
    /// </summary>
    public class QdrantVectorStoreOptions
    {
        /// <summary>Qdrant 服务器地址</summary>
        public string Host { get; set; } = "localhost";

        /// <summary>Qdrant 端口</summary>
        public int Port { get; set; } = 6333;

        /// <summary>是否使用 HTTPS</summary>
        public bool UseHttps { get; set; } = false;

        /// <summary>API 密钥（可选）</summary>
        public string? ApiKey { get; set; }

        /// <summary>连接超时（毫秒）</summary>
        public int TimeoutMs { get; set; } = 5000;

        /// <summary>启用详细日志</summary>
        public bool EnableVerboseLogging { get; set; } = false;

        /// <summary>向量距离度量方式</summary>
        public string DistanceMetric { get; set; } = "Cosine";
    }
}
