using System.Text.Json;
using System.Text.Json.Serialization;

namespace CKY.MultiAgentFramework.Infrastructure.Caching.Redis
{
    /// <summary>
    /// Redis 缓存存储配置选项
    /// </summary>
    public class RedisCacheStoreOptions
    {
        /// <summary>
        /// 数据库 ID（0-15）
        /// </summary>
        public int DatabaseId { get; set; } = 0;

        /// <summary>
        /// JSON 序列化选项
        /// </summary>
        public JsonSerializerOptions JsonSerializerOptions { get; set; } = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        /// <summary>
        /// 是否启用详细日志（记录所有操作）
        /// </summary>
        public bool EnableVerboseLogging { get; set; } = false;

        /// <summary>
        /// 操作超时时间（毫秒）
        /// </summary>
        public int OperationTimeoutMs { get; set; } = 5000;
    }
}
