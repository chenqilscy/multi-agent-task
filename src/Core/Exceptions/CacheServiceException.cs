namespace CKY.MultiAgentFramework.Core.Exceptions
{
    /// <summary>
    /// 缓存服务异常（缓存操作失败）
    /// </summary>
    /// <remarks>
    /// 使用场景：
    /// - L1/L2/L3 缓存读写失败
    /// - Redis 连接失败或超时
    /// - 缓存数据损坏或格式错误
    /// - 缓存容量超限
    /// - 缓存序列化/反序列化失败
    ///
    /// 错误处理建议：
    /// - IsRetryable = true: 启用重试机制（指数退避）
    /// - L1 缓存失败: 降级到 L2/L3
    /// - L2 缓存失败: 降级到 L3
    /// - 全部失败: 返回默认值或抛出异常
    ///
    /// 设计原则：
    /// - 缓存失败不应影响主流程
    /// - 优雅降级，优先保证可用性
    /// - 记录详细的错误日志用于诊断
    ///
    /// 示例：
    /// <code>
    /// try
    /// {
    ///     await _cache.SetAsync(key, value);
    /// }
    /// catch (CacheServiceException ex)
    /// {
    ///     _logger.LogWarning(ex, "缓存写入失败，降级到数据库");
    ///     // 降级到数据库存储
    ///     await _database.SaveAsync(value);
    /// }
    /// </code>
    /// </remarks>
    public class CacheServiceException : MafException
    {
        /// <summary>
        /// 初始化 CacheServiceException 类的新实例
        /// </summary>
        /// <param name="message">错误消息</param>
        public CacheServiceException(string message)
            : base(MafErrorCode.CacheServiceError, message, isRetryable: true, component: "CacheStore")
        {
        }
    }
}
