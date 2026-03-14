using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Infrastructure.Caching.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System.Diagnostics;

namespace CKY.MultiAgentFramework.Infrastructure.Caching.Redis
{
    /// <summary>
    /// Redis 缓存存储实现
    /// </summary>
    public sealed class RedisCacheStore : ICacheStore
    {
        private readonly IConnectionMultiplexer _redis;
        private readonly IDatabase _db;
        private readonly ILogger<RedisCacheStore> _logger;
        private readonly RedisCacheStoreOptions _options;

        /// <summary>
        /// 构造函数
        /// </summary>
        public RedisCacheStore(
            IConnectionMultiplexer redis,
            ILogger<RedisCacheStore> logger,
            IOptions<RedisCacheStoreOptions> options)
        {
            _redis = redis ?? throw new ArgumentNullException(nameof(redis));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options?.Value ?? new RedisCacheStoreOptions();

            // 获取数据库实例（默认数据库 0）
            _db = _redis.GetDatabase(_options.DatabaseId);

            _logger.LogInformation("RedisCacheStore initialized (DatabaseId: {DatabaseId})", _options.DatabaseId);
        }

        #region ICacheStore 实现

        /// <summary>
        /// 获取缓存值
        /// </summary>
        public async Task<T?> GetAsync<T>(
            string key,
            CancellationToken ct = default) where T : class
        {
            try
            {
                var stopwatch = Stopwatch.StartNew();

                var value = await _db.StringGetAsync(key);

                if (_options.EnableVerboseLogging)
                {
                    _logger.LogDebug("GetAsync: {Key} - Found: {Found}, Latency: {Latency}ms",
                        key, value.HasValue, stopwatch.ElapsedMilliseconds);
                }

                if (!value.HasValue)
                    return null;

                var result = Deserialize<T>(value);
                return result;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                HandleException(ex, key, "GetAsync");
                return null; // 降级：返回 null 而非抛出异常
            }
        }

        /// <summary>
        /// 设置缓存值
        /// </summary>
        public async Task SetAsync<T>(
            string key,
            T value,
            TimeSpan? expiry = null,
            CancellationToken ct = default) where T : class
        {
            try
            {
                var stopwatch = Stopwatch.StartNew();

                var json = Serialize(value);

                // 直接使用 expiry 参数，提供完整的参数列表
                await _db.StringSetAsync(key, json, expiry, When.Always, CommandFlags.None);

                if (_options.EnableVerboseLogging)
                {
                    _logger.LogDebug("SetAsync: {Key}, Expiry: {Expiry}s, Latency: {Latency}ms",
                        key, expiry?.TotalSeconds ?? -1, stopwatch.ElapsedMilliseconds);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                HandleException(ex, key, "SetAsync");
            }
        }

        /// <summary>
        /// 删除缓存值
        /// </summary>
        public async Task DeleteAsync(
            string key,
            CancellationToken ct = default)
        {
            try
            {
                var stopwatch = Stopwatch.StartNew();

                await _db.KeyDeleteAsync(key);

                if (_options.EnableVerboseLogging)
                {
                    _logger.LogDebug("DeleteAsync: {Key}, Latency: {Latency}ms",
                        key, stopwatch.ElapsedMilliseconds);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                HandleException(ex, key, "DeleteAsync");
            }
        }

        /// <summary>
        /// 批量获取缓存值
        /// </summary>
        public async Task<Dictionary<string, T?>> GetBatchAsync<T>(
            IEnumerable<string> keys,
            CancellationToken ct = default) where T : class
        {
            try
            {
                var stopwatch = Stopwatch.StartNew();

                var keyArray = keys.ToArray();
                var redisKeys = keyArray.Select(k => (RedisKey)k).ToArray();

                // 批量获取（单次网络往返）
                var values = await _db.StringGetAsync(redisKeys);

                var result = new Dictionary<string, T?>(keyArray.Length);
                for (int i = 0; i < keyArray.Length; i++)
                {
                    var key = keyArray[i];
                    var value = values[i];

                    result[key] = value.HasValue ? Deserialize<T>(value!) : null;
                }

                if (_options.EnableVerboseLogging)
                {
                    _logger.LogDebug("GetBatchAsync: {Count} keys, Latency: {Latency}ms",
                        keyArray.Length, stopwatch.ElapsedMilliseconds);
                }

                return result;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                HandleException(ex, "GetBatchAsync", "GetBatchAsync");
                return new Dictionary<string, T?>();
            }
        }

        /// <summary>
        /// 检查键是否存在
        /// </summary>
        public async Task<bool> ExistsAsync(
            string key,
            CancellationToken ct = default)
        {
            try
            {
                return await _db.KeyExistsAsync(key);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                HandleException(ex, key, "ExistsAsync");
                return false;
            }
        }

        #endregion

        #region 私有辅助方法

        /// <summary>
        /// 序列化对象为 JSON
        /// </summary>
        private string Serialize<T>(T value) where T : class
        {
            try
            {
                return System.Text.Json.JsonSerializer.Serialize(value, _options.JsonSerializerOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to serialize value of type {Type}", typeof(T).Name);
                throw new CacheException("Serialize", typeof(T).Name, "Serialization failed", ex);
            }
        }

        /// <summary>
        /// 反序列化 JSON 为对象
        /// </summary>
        private T? Deserialize<T>(RedisValue json) where T : class
        {
            try
            {
                // 明确调用 string 版本的重载
                return System.Text.Json.JsonSerializer.Deserialize<T>(json.ToString()!, _options.JsonSerializerOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to deserialize JSON to type {Type}", typeof(T).Name);
                return null; // 降级：返回 null 而非抛出异常
            }
        }

        /// <summary>
        /// 统一异常处理
        /// </summary>
        private void HandleException(Exception ex, string key, string operation)
        {
            var exceptionType = ex.GetType().Name;

            _logger.LogError(ex, "Redis operation failed: {Operation}, Key: {Key}, Exception: {ExceptionType}",
                operation, key, exceptionType);

            // 根据异常类型采取不同策略
            switch (ex)
            {
                case RedisConnectionException:
                case RedisTimeoutException:
                    // 连接问题：降级处理，记录监控指标
                    RecordFailureMetric(operation, exceptionType);
                    break;

                case RedisException:
                    // Redis 服务器错误：降级处理
                    RecordFailureMetric(operation, exceptionType);
                    break;

                default:
                    // 其他异常：记录并降级
                    RecordFailureMetric(operation, exceptionType);
                    break;
            }
        }

        /// <summary>
        /// 记录失败指标（用于监控）
        /// </summary>
        private void RecordFailureMetric(string operation, string exceptionType)
        {
            // TODO: 集成 Prometheus
            // _failureCounter.WithLabels(operation, exceptionType).Inc();
        }

        #endregion
    }
}
