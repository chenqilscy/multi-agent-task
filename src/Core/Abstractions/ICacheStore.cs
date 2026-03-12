namespace CKY.MultiAgentFramework.Core.Abstractions
{
    /// <summary>
    /// 缓存存储抽象接口
    /// 支持多种实现：Redis、MemoryCache、NCache等
    /// </summary>
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
