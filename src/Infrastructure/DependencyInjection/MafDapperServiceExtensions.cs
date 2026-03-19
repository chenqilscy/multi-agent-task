using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Infrastructure.Dapper;
using CKY.MultiAgentFramework.Infrastructure.Caching.Memory;
using CKY.MultiAgentFramework.Infrastructure.Vectorization.Memory;

namespace CKY.MultiAgentFramework.Infrastructure.DependencyInjection;

/// <summary>
/// MAF Dapper 服务注册扩展（MAF 框架默认实现）
/// </summary>
public static class MafDapperServiceExtensions
{
    /// <summary>
    /// 注册 MAF 框架服务（使用 Dapper 作为关系数据库实现）
    /// 适用于需要高性能和业务层完全解耦的场景
    /// </summary>
    /// <remarks>
    /// <para><b>自动注册的服务：</b></para>
    /// <list type="bullet">
    ///   <item>ICacheStore → MemoryCacheStore（可配置降级到 Redis）</item>
    ///   <item>IVectorStore → MemoryVectorStore</item>
    ///   <item>IRelationalDatabase → DapperRelationalDatabase (高性能)</item>
    /// </list>
    /// <para><b>配置示例：</b></para>
    /// <code>
    /// // appsettings.json
    /// {
    ///   "MafStorage": {
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
    public static IServiceCollection AddMafDapperServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // 注册 ASP.NET Core 基础服务
        services.AddMemoryCache();
        services.AddLogging();

        // 注册缓存服务（使用 MemoryCacheStore）
        services.AddSingleton<ICacheStore, MemoryCacheStore>();

        // 注册向量存储
        services.AddSingleton<IVectorStore, MemoryVectorStore>();

        // 注册 Dapper 关系数据库（MAF 框架默认实现）
        services.AddDapperRelationalDatabase(configuration);

        return services;
    }
}
