using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using Microsoft.EntityFrameworkCore;
using Qdrant.Client;

using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Core.Agents.Specialized;
using CKY.MultiAgentFramework.Infrastructure.Caching.Memory;
using CKY.MultiAgentFramework.Infrastructure.Caching.Redis;
using CKY.MultiAgentFramework.Infrastructure.Vectorization.Memory;
using CKY.MultiAgentFramework.Infrastructure.Vectorization.Qdrant;
using CKY.MultiAgentFramework.Infrastructure.Repository.Relational;
using CKY.MultiAgentFramework.Infrastructure.Repository.Data;
using CKY.MultiAgentFramework.Infrastructure.Repository.Repositories;
using CKY.MultiAgentFramework.Infrastructure.Dapper;
using CKY.MultiAgentFramework.Infrastructure.Embedding;
using CKY.MultiAgentFramework.Infrastructure.Parsing;

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
            // 绑定 Qdrant 配置选项
            services.Configure<QdrantVectorStoreOptions>(
                configuration.GetSection("Qdrant"));

            // 注册 QdrantClient（gRPC 客户端）
            services.AddSingleton(sp =>
            {
                var options = sp.GetRequiredService<IOptions<QdrantVectorStoreOptions>>().Value;
                return new QdrantClient(
                    host: options.Host,
                    port: options.Port,
                    https: options.UseHttps,
                    apiKey: options.ApiKey);
            });

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

        // ========================================
        // 文档解析器服务注册
        // ========================================
        services.AddMafDocumentParsers();

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

        // 注册文档解析器
        services.AddMafDocumentParsers();
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

                options.UseNpgsql(connectionString, npgsqlOptions =>
                {
                    // 连接弹性：自动重试暂时性故障
                    npgsqlOptions.EnableRetryOnFailure(
                        maxRetryCount: configuration.GetValue("MafStorage:PostgreSQL:MaxRetryCount", 3),
                        maxRetryDelay: TimeSpan.FromSeconds(
                            configuration.GetValue("MafStorage:PostgreSQL:MaxRetryDelaySeconds", 30)),
                        errorCodesToAdd: null);

                    // 命令超时
                    npgsqlOptions.CommandTimeout(
                        configuration.GetValue("MafStorage:PostgreSQL:CommandTimeoutSeconds", 30));

                    // 迁移程序集
                    npgsqlOptions.MigrationsAssembly(
                        typeof(MafDbContext).Assembly.GetName().Name);
                });
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

    /// <summary>
    /// 注册文档解析器及工厂
    /// </summary>
    public static IServiceCollection AddMafDocumentParsers(this IServiceCollection services)
    {
        services.AddSingleton<IDocumentParser, TxtDocumentParser>();
        services.AddSingleton<IDocumentParser, MarkdownDocumentParser>();
        services.AddSingleton<IDocumentParser, HtmlDocumentParser>();
        services.AddSingleton<IDocumentParser, CsvDocumentParser>();
        services.AddSingleton<DocumentParserFactory>();

        return services;
    }

    /// <summary>
    /// 注册智谱AI嵌入服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置对象</param>
    /// <returns>服务集合（链式调用）</returns>
    /// <remarks>
    /// 需要在 appsettings.json 中配置:
    /// <code>
    /// {
    ///   "ZhipuAI": {
    ///     "ApiKey": "your-api-key",
    ///     "EmbeddingModel": "embedding-3",
    ///     "EmbeddingDimension": 2048
    ///   }
    /// }
    /// </code>
    /// </remarks>
    public static IServiceCollection AddZhipuAIEmbeddingService(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var apiKey = configuration["ZhipuAI:ApiKey"];
        var model = configuration.GetValue("ZhipuAI:EmbeddingModel",
            ZhipuAIEmbeddingService.DefaultModel)!;
        var dimension = configuration.GetValue("ZhipuAI:EmbeddingDimension",
            ZhipuAIEmbeddingService.DefaultDimension);

        services.AddHttpClient(nameof(ZhipuAIEmbeddingService), client =>
        {
            client.BaseAddress = new Uri("https://open.bigmodel.cn/api/paas/v4/");
            if (!string.IsNullOrEmpty(apiKey))
            {
                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
            }
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        services.AddSingleton<IEmbeddingService>(sp =>
        {
            var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
            var httpClient = httpClientFactory.CreateClient(nameof(ZhipuAIEmbeddingService));
            var logger = sp.GetRequiredService<ILogger<ZhipuAIEmbeddingService>>();
            return new ZhipuAIEmbeddingService(httpClient, logger, model, dimension);
        });

        return services;
    }

    /// <summary>
    /// 注册 MAF 内置专业 Agent（一键注册所有 Core/Agents/Specialized 中的 Agent）
    /// </summary>
    /// <remarks>
    /// 注册的 Agent 列表：
    /// <list type="bullet">
    ///   <item>MafLeaderAgent — 通用主控编排 Agent</item>
    ///   <item>RagKnowledgeAgent — 通用 RAG 知识库 Agent</item>
    ///   <item>DialogueAgent — 多轮对话 Agent</item>
    ///   <item>IntentRecognitionAgent — 意图识别 Agent</item>
    ///   <item>EmbeddingAgent — 文本嵌入 Agent</item>
    ///   <item>SummarizationAgent — 文本摘要 Agent</item>
    ///   <item>TranslationAgent — 翻译 Agent</item>
    ///   <item>CodeAgent — 代码生成 Agent</item>
    ///   <item>ImageAgent — 图像处理 Agent</item>
    ///   <item>VideoAgent — 视频处理 Agent</item>
    /// </list>
    /// 所有 Agent 以 Singleton 注册。其依赖（如 IRagPipeline、IIntentKeywordProvider）
    /// 需由调用方另行注册。
    /// </remarks>
    public static IServiceCollection AddMafBuiltinAgents(this IServiceCollection services)
    {
        // 编排类
        services.AddSingleton<MafLeaderAgent>();
        services.AddSingleton<RagKnowledgeAgent>();

        // 功能类
        services.AddSingleton<DialogueAgent>();
        services.AddSingleton<IntentRecognitionAgent>();
        services.AddSingleton<EmbeddingAgent>();
        services.AddSingleton<SummarizationAgent>();
        services.AddSingleton<TranslationAgent>();
        services.AddSingleton<CodeAgent>();
        services.AddSingleton<ImageAgent>();
        services.AddSingleton<VideoAgent>();

        return services;
    }
}
