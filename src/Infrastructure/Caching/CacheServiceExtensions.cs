using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Infrastructure.Caching.Memory;
using CKY.MultiAgentFramework.Infrastructure.Caching.Redis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using StackExchange.Redis;

namespace CKY.MultiAgentFramework.Infrastructure.Caching
{
    /// <summary>
    /// 缓存服务依赖注入扩展
    /// </summary>
    public static class CacheServiceExtensions
    {
        /// <summary>
        /// 添加缓存存储服务
        /// </summary>
        public static IServiceCollection AddCacheStore(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // 注册 IConnectionMultiplexer（单例）
            services.AddSingleton<IConnectionMultiplexer>(sp =>
            {
                var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
                var logger = loggerFactory.CreateLogger(typeof(CacheServiceExtensions));

                var connectionString = configuration.GetConnectionString("Redis");

                if (string.IsNullOrEmpty(connectionString))
                {
                    throw new InvalidOperationException("Redis connection string is not configured");
                }

                logger.LogInformation("Connecting to Redis: {ConnectionString}",
                    MaskConnectionString(connectionString));

                var options = ConfigurationOptions.Parse(connectionString);
                options.AbortOnConnectFail = false; // 连接失败时不终止应用
                options.ConnectRetry = 3; // 重试3次
                options.ConnectTimeout = 5000; // 5秒超时
                options.SyncTimeout = 5000;

                return ConnectionMultiplexer.Connect(options);
            });

            // 注册配置选项
            services.Configure<RedisCacheStoreOptions>(options =>
            {
                configuration.GetSection("RedisCache").Bind(options);
            });

            // 注册 ICacheStore（根据环境选择实现）
            var useMemoryCache = configuration.GetValue<bool>("UseMemoryCache", false);

            if (useMemoryCache)
            {
                // 开发/测试环境：使用内存缓存
                services.AddSingleton<ICacheStore, MemoryCacheStore>();
                services.AddSingleton<IMemoryCache, MemoryCache>();
            }
            else
            {
                // 生产环境：使用 Redis
                services.AddSingleton<ICacheStore, RedisCacheStore>();
            }

            return services;
        }

        /// <summary>
        /// 掩盖连接字符串中的密码
        /// </summary>
        private static string MaskConnectionString(string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString))
                return string.Empty;

            // 隐藏密码用于日志
            var parts = connectionString.Split(',', StringSplitOptions.TrimEntries);
            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i].Trim().StartsWith("password=", StringComparison.OrdinalIgnoreCase))
                {
                    parts[i] = "password=****";
                }
            }

            return string.Join(", ", parts);
        }
    }
}
