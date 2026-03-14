using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using CKY.MultiAgentFramework.Core.Abstractions.Interfaces;
using CKY.MultiAgentFramework.Infrastructure.Caching.Memory;
using CKY.MultiAgentFramework.Infrastructure.Caching.Redis;
using CKY.MultiAgentFramework.Infrastructure.Vectorization.Memory;
using CKY.MultiAgentFramework.Infrastructure.Vectorization.Qdrant;

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

        return services;
    }
}
