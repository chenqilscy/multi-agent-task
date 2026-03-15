namespace CKY.MultiAgentFramework.Core.Abstractions
{
    /// <summary>
    /// 缓存存储抽象接口
    /// 支持多种实现：Redis、MemoryCache、NCache等
    /// </summary>
    /// <remarks>
    /// <para><b>默认推荐实现：</b>RedisCacheStore</para>
    /// <para><b>特性：</b></para>
    /// <list type="bullet">
    ///   <item>分布式缓存，支持多实例部署</item>
    ///   <item>高性能键值存储（毫秒级响应）</item>
    ///   <item>支持过期时间、批量操作</item>
    /// </list>
    /// <para><b>部署要求：</b></para>
    /// <list type="bullet">
    ///   <item>需要 Redis 服务（Docker 或本地安装）</item>
    ///   <item>配置连接字符串：ConnectionStrings:Redis</item>
    /// </list>
    /// <para><b>替代方案：</b>MemoryCacheStore（仅用于单元测试）</para>
    /// </remarks>
    public interface ICacheStore
    {
        /// <summary>
        /// 获取缓存值
        /// </summary>
        Task<T?> GetAsync<T>(
            string key,
            CancellationToken ct = default) where T : class;

        /// <summary>
        /// 设置缓存值
        /// </summary>
        Task SetAsync<T>(
            string key,
            T value,
            TimeSpan? expiry = null,
            CancellationToken ct = default) where T : class;

        /// <summary>
        /// 删除缓存值
        /// </summary>
        Task DeleteAsync(
            string key,
            CancellationToken ct = default);

        /// <summary>
        /// 批量获取
        /// </summary>
        Task<Dictionary<string, T?>> GetBatchAsync<T>(
            IEnumerable<string> keys,
            CancellationToken ct = default) where T : class;

        /// <summary>
        /// 检查键是否存在
        /// </summary>
        Task<bool> ExistsAsync(
            string key,
            CancellationToken ct = default);
    }
}
