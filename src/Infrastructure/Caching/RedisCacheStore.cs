using System.Text.Json;
using CKY.MultiAgentFramework.Core.Abstractions;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace CKY.MultiAgentFramework.Infrastructure.Caching
{
    /// <summary>
    /// Redis缓存存储实现
    /// </summary>
    public class RedisCacheStore : ICacheStore
    {
        private readonly IConnectionMultiplexer _redis;
        private readonly IDatabase _db;
        private readonly ILogger<RedisCacheStore> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        public RedisCacheStore(
            IConnectionMultiplexer redis,
            ILogger<RedisCacheStore> logger)
        {
            _redis = redis ?? throw new ArgumentNullException(nameof(redis));
            _db = redis.GetDatabase();
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        /// <inheritdoc />
        public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default) where T : class
        {
            try
            {
                var value = await _db.StringGetAsync(key);
                if (!value.HasValue)
                {
                    return null;
                }

                return JsonSerializer.Deserialize<T>(value.ToString(), _jsonOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting key {Key} from Redis", key);
                return null;
            }
        }

        /// <inheritdoc />
        public async Task SetAsync<T>(
            string key,
            T value,
            TimeSpan? expiry = null,
            CancellationToken ct = default) where T : class
        {
            try
            {
                var serialized = JsonSerializer.Serialize(value, _jsonOptions);
                await _db.StringSetAsync(key, serialized, expiry.HasValue ? expiry.Value : (TimeSpan?)null, When.Always);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting key {Key} in Redis", key);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task DeleteAsync(string key, CancellationToken ct = default)
        {
            try
            {
                await _db.KeyDeleteAsync(key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting key {Key} from Redis", key);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<Dictionary<string, T?>> GetBatchAsync<T>(
            IEnumerable<string> keys,
            CancellationToken ct = default) where T : class
        {
            var keyArray = keys.Select(k => (RedisKey)k).ToArray();
            var values = await _db.StringGetAsync(keyArray);

            var result = new Dictionary<string, T?>();
            var keyList = keyArray.ToList();

            for (int i = 0; i < keyList.Count; i++)
            {
                var key = (string)keyList[i]!;
                if (values[i].HasValue)
                {
                    try
                    {
                        result[key] = JsonSerializer.Deserialize<T>(values[i].ToString(), _jsonOptions);
                    }
                    catch
                    {
                        result[key] = null;
                    }
                }
                else
                {
                    result[key] = null;
                }
            }

            return result;
        }

        /// <inheritdoc />
        public async Task<bool> ExistsAsync(string key, CancellationToken ct = default)
        {
            try
            {
                return await _db.KeyExistsAsync(key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking existence of key {Key} in Redis", key);
                return false;
            }
        }
    }
}
