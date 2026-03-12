using System.Collections.Concurrent;
using System.Text.Json;
using CKY.MultiAgentFramework.Core.Abstractions;

namespace CKY.MultiAgentFramework.Infrastructure.Caching
{
    /// <summary>
    /// 内存缓存存储实现（用于开发和测试）
    /// </summary>
    public class MemoryCacheStore : ICacheStore
    {
        private readonly ConcurrentDictionary<string, (string Value, DateTime? Expiry)> _store = new();

        /// <inheritdoc />
        public Task<T?> GetAsync<T>(string key, CancellationToken ct = default) where T : class
        {
            if (_store.TryGetValue(key, out var entry))
            {
                if (entry.Expiry.HasValue && DateTime.UtcNow > entry.Expiry.Value)
                {
                    _store.TryRemove(key, out _);
                    return Task.FromResult<T?>(null);
                }

                try
                {
                    var result = JsonSerializer.Deserialize<T>(entry.Value);
                    return Task.FromResult(result);
                }
                catch
                {
                    return Task.FromResult<T?>(null);
                }
            }

            return Task.FromResult<T?>(null);
        }

        /// <inheritdoc />
        public Task SetAsync<T>(
            string key,
            T value,
            TimeSpan? expiry = null,
            CancellationToken ct = default) where T : class
        {
            var serialized = JsonSerializer.Serialize(value);
            var expiryTime = expiry.HasValue ? DateTime.UtcNow.Add(expiry.Value) : (DateTime?)null;
            _store[key] = (serialized, expiryTime);
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task DeleteAsync(string key, CancellationToken ct = default)
        {
            _store.TryRemove(key, out _);
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public async Task<Dictionary<string, T?>> GetBatchAsync<T>(
            IEnumerable<string> keys,
            CancellationToken ct = default) where T : class
        {
            var result = new Dictionary<string, T?>();
            foreach (var key in keys)
            {
                result[key] = await GetAsync<T>(key, ct);
            }
            return result;
        }

        /// <inheritdoc />
        public Task<bool> ExistsAsync(string key, CancellationToken ct = default)
        {
            if (_store.TryGetValue(key, out var entry))
            {
                if (entry.Expiry.HasValue && DateTime.UtcNow > entry.Expiry.Value)
                {
                    _store.TryRemove(key, out _);
                    return Task.FromResult(false);
                }
                return Task.FromResult(true);
            }
            return Task.FromResult(false);
        }
    }
}
