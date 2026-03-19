using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using Microsoft.EntityFrameworkCore;

using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Infrastructure.Caching.Memory;
using CKY.MultiAgentFramework.Infrastructure.Caching.Redis;
using CKY.MultiAgentFramework.Infrastructure.Vectorization.Memory;
using CKY.MultiAgentFramework.Infrastructure.Vectorization.Qdrant;
using CKY.MultiAgentFramework.Infrastructure.Repository.Relational;
using CKY.MultiAgentFramework.Infrastructure.Repository.Data;
using CKY.MultiAgentFramework.Infrastructure.Repository.Repositories;
using CKY.MultiAgentFramework.Infrastructure.Dapper;

namespace CKY.MultiAgentFramework.Infrastructure.DependencyInjection;

/// <summary>
/// 自动服务注册扩展方法
/// </summary>
public static class MafServiceRegistrationExtensions
{
    private const string CacheConfigKey = "MafServices:Implementations:ICacheStore";
    private const string MemoryCacheImplementation = "MemoryCacheStore";
    private const string RedisCacheImplementation = "RedisCacheStore";

    private const string VectorConfigKey = "MafServices:Implementations:IVectorStore";
    private const string MemoryVectorImplementation = "MemoryVectorStore";
    private const string QdrantVectorImplementation = "QdrantVectorStore";

    private const string DatabaseConfigKey = "MafServices:Implementations:IRelationalDatabase";

    private const string SessionConfigKey = "MafServices:Implementations:IMafAiSessionStore";
    private const string DatabaseSessionImplementation = "DatabaseMafAiSessionStore";
    private const string RedisSessionImplementation = "RedisMafAiSessionStore";

    /// <summary>
    /// 自动注册所有 Infrastructure 层服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置对象</param>
    /// <returns>服务集合（链式调用）</returns>
    public static IServiceCollection AddMafInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ========================================
        // 缓存服务注册
        // ========================================
        var cacheImpl = configuration[CacheConfigKey];

        if (string.IsNullOrEmpty(cacheImpl) || cacheImpl == MemoryCacheImplementation)
        {
            // 配置未指定或指定内存实现 → 使用内存实现
            services.AddSingleton<ICacheStore, MemoryCacheStore>();
        }
        else if (cacheImpl == RedisCacheImplementation)
        {
            services.AddSingleton<ICacheStore, RedisCacheStore>();
        }
        else
        {
            // 配置值无效，静默使用默认实现
            services.AddSingleton<ICacheStore, MemoryCacheStore>();
        }

        // ========================================
        // 向量存储服务注册
        // ========================================
        var vectorImpl = configuration[VectorConfigKey];

        if (string.IsNullOrEmpty(vectorImpl) || vectorImpl == MemoryVectorImplementation)
        {
            // 默认: 内存实现
            services.AddSingleton<IVectorStore, MemoryVectorStore>();
        }
        else if (vectorImpl == QdrantVectorImplementation)
        {
            services.AddSingleton<IVectorStore, QdrantVectorStore>();
        }
        else
        {
            // 配置值无效，静默使用默认实现
            services.AddSingleton<IVectorStore, MemoryVectorStore>();
        }

        // ========================================
        // 关系数据库服务注册
        // ========================================
        // 注意：此服务默认使用 EfCoreRelationalDatabase (SQLite via EF Core)
        // 如需其他实现，可在此扩展配置驱动选择
        services.AddScoped<IRelationalDatabase, EfCoreRelationalDatabase>();

        // ========================================
        // Session 存储服务注册
        // ========================================
        var sessionImpl = configuration[SessionConfigKey];
        if (string.IsNullOrEmpty(sessionImpl) || sessionImpl == DatabaseSessionImplementation)
        {
            services.AddScoped<IMafAiSessionStore, DatabaseMafAiSessionStore>();
        }
        else if (sessionImpl == RedisSessionImplementation)
        {
            services.AddSingleton<IMafAiSessionStore, RedisMafAiSessionStore>();
        }
        else
        {
            services.AddScoped<IMafAiSessionStore, DatabaseMafAiSessionStore>();
        }

        return services;
    }

    /// <summary>
    /// 添加 CKY.MAF 内置推荐实现（简化版一键注册）
    /// </summary>
    /// <remarks>
    /// <para><b>自动注册的服务：</b></para>
    /// <list type="bullet">
    ///   <item>ICacheStore → RedisCacheStore（带自动连接和日志）</item>
    ///   <item>IVectorStore → MemoryVectorStore</item>
    ///   <item>IRelationalDatabase → EfCoreRelationalDatabase (SQLite/PostgreSQL)</item>
    /// </list>
    /// <para><b>配置示例：</b></para>
    /// <code>
    /// // appsettings.json
    /// {
    ///   "ConnectionStrings": {
    ///     "Redis": "localhost:6379",
    ///     "PostgreSQL": "Host=localhost;Port=5432;Database=mafdb;Username=maf;Password=***"
    ///   },
    ///   "MafStorage": {
    ///     "UseBuiltinImplementations": true,
    ///     "RelationalDatabase": {
    ///       "Provider": "SQLite",  // 或 "PostgreSQL"
    ///       "SqlitePath": "maf.db"
    ///     }
    ///   }
    /// }
    /// </code>
    /// </remarks>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置对象</param>
    /// <returns>服务集合（链式调用）</returns>
    public static IServiceCollection AddMafBuiltinServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // 注册内置存储实现
        services.AddMafStorageImplementations(configuration);

        return services;
    }

    /// <summary>
    /// 注册 MAF 存储实现（内置逻辑）
    /// </summary>
    private static void AddMafStorageImplementations(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var useBuiltin = configuration.GetValue<bool>(
            "MafStorage:UseBuiltinImplementations", true);

        if (!useBuiltin)
        {
            // 生产环境：使用现有的配置驱动注册
            services.AddMafInfrastructureServices(configuration);
            return;
        }

        // 注册 ASP.NET Core 基础服务（必需）
        services.AddMemoryCache();  // 注册 IMemoryCache
        services.AddLogging();       // 确保 ILogger 可用

        // 注册 ICacheStore → 优先 Redis，失败时降级到 MemoryCache
        var redisConnectionString = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrEmpty(redisConnectionString))
        {
            try
            {
                services.AddRedisCacheStore(configuration);
            }
            catch
            {
                // Redis 连接失败，降级到内存缓存
                services.AddSingleton<ICacheStore, MemoryCacheStore>();
            }
        }
        else
        {
            // 没有配置 Redis，直接使用内存缓存
            services.AddSingleton<ICacheStore, MemoryCacheStore>();
        }

        // 注册 IVectorStore → MemoryVectorStore
        services.AddSingleton<IVectorStore, MemoryVectorStore>();

        // 注册 IRelationalDatabase → EfCoreRelationalDatabase（增强版：支持 PostgreSQL）
        services.AddEfCoreRelationalDatabase(configuration);

        // 注册 IMafAiSessionStore → DatabaseMafAiSessionStore（带工厂方法）
        services.AddScoped<IMafAiSessionStore>(sp =>
        {
            var dbContext = sp.GetRequiredService<MafDbContext>();
            var logger = sp.GetRequiredService<ILogger<DatabaseMafAiSessionStore>>();
            var options = sp.GetService<IOptions<DatabaseSessionOptions>>()?.Value;
            return new DatabaseMafAiSessionStore(() => dbContext, logger, options);
        });
    }

    /// <summary>
    /// 注册 Redis 缓存存储（增强版：带连接管理和日志）
    /// </summary>
    private static void AddRedisCacheStore(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<RedisCacheStore>>();
            var connectionString = configuration.GetConnectionString("Redis");

            if (string.IsNullOrEmpty(connectionString))
            {
                logger.LogWarning("Redis connection string not configured. Using fallback: localhost:6379");
                connectionString = "localhost:6379";
            }

            try
            {
                return ConnectionMultiplexer.Connect(connectionString);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to connect to Redis at {ConnectionString}", connectionString);
                throw;
            }
        });

        // 注册配置选项（必须在使用 RedisCacheStore 之前注册）
        services.Configure<RedisCacheStoreOptions>(options =>
            configuration.GetSection("RedisCache").Bind(options));

        services.AddSingleton<ICacheStore, RedisCacheStore>();
    }

    /// <summary>
    /// 注册 EF Core 关系数据库（增强版：支持 PostgreSQL）
    /// </summary>
    private static void AddEfCoreRelationalDatabase(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var provider = configuration.GetValue<string>(
            "MafStorage:RelationalDatabase:Provider", "SQLite");

        services.AddDbContext<MafDbContext>(options =>
        {
            if (provider.Equals("SQLite", StringComparison.OrdinalIgnoreCase))
            {
                // SQLite: 文件数据库，零配置
                var dbPath = configuration.GetValue<string>(
                    "MafStorage:RelationalDatabase:SqlitePath", "maf.db");
                options.UseSqlite($"Data Source={dbPath}");
            }
            else if (provider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase))
            {
                // PostgreSQL: 生产环境
                var connectionString = configuration.GetConnectionString("PostgreSQL");
                if (string.IsNullOrEmpty(connectionString))
                {
                    throw new InvalidOperationException(
                        "PostgreSQL connection string is required when Provider is set to PostgreSQL");
                }
                options.UseNpgsql(connectionString);
            }
            else
            {
                throw new NotSupportedException(
                    $"Database provider '{provider}' is not supported. " +
                    "Supported providers: SQLite, PostgreSQL");
            }
        });

        // 注册 IRelationalDatabase，使用工厂方法确保 DbContext 正确注入
        services.AddScoped<IRelationalDatabase>(sp =>
        {
            var dbContext = sp.GetRequiredService<MafDbContext>();
            var logger = sp.GetRequiredService<ILogger<EfCoreRelationalDatabase>>();
            return new EfCoreRelationalDatabase(dbContext, logger);
        });
    }
}
