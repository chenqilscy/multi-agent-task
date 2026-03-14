using CKY.MultiAgentFramework.Core.Abstractions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace CKY.MultiAgentFramework.Infrastructure.Caching.Memory
{
    /// <summary>
    /// 内存缓存存储实现（用于测试/开发）
    /// </summary>
    public sealed class MemoryCacheStore : ICacheStore
    {
        private readonly IMemoryCache _cache;
        private readonly ILogger<MemoryCacheStore> _logger;

        public MemoryCacheStore(
            IMemoryCache cache,
            ILogger<MemoryCacheStore> logger)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<T?> GetAsync<T>(
            string key,
            CancellationToken ct = default) where T : class
        {
            return await Task.FromResult(_cache.Get<T?>(key));
        }

        public async Task SetAsync<T>(
            string key,
            T value,
            TimeSpan? expiry = null,
            CancellationToken ct = default) where T : class
        {
            var options = new MemoryCacheEntryOptions();

            if (expiry.HasValue)
            {
                options.SetAbsoluteExpiration(expiry.Value);
            }

            _cache.Set(key, value, options);
            await Task.CompletedTask;
        }

        public async Task DeleteAsync(
            string key,
            CancellationToken ct = default)
        {
            _cache.Remove(key);
            await Task.CompletedTask;
        }

        public async Task<Dictionary<string, T?>> GetBatchAsync<T>(
            IEnumerable<string> keys,
            CancellationToken ct = default) where T : class
        {
            var result = new Dictionary<string, T?>();

            foreach (var key in keys)
            {
                result[key] = _cache.Get<T?>(key);
            }

            return await Task.FromResult(result);
        }

        public async Task<bool> ExistsAsync(
            string key,
            CancellationToken ct = default)
        {
            return await Task.FromResult(_cache.TryGetValue(key, out _));
        }
    }
}
