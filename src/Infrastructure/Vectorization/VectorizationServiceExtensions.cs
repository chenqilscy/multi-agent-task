using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Infrastructure.Vectorization.Memory;
using CKY.MultiAgentFramework.Infrastructure.Vectorization.Qdrant;
using Qdrant.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CKY.MultiAgentFramework.Infrastructure.Vectorization
{
    /// <summary>
    /// 向量存储服务依赖注入扩展
    /// </summary>
    public static class VectorizationServiceExtensions
    {
        /// <summary>
        /// 添加向量存储服务
        /// </summary>
        public static IServiceCollection AddVectorStore(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // 注册 QdrantClient（单例）
            services.AddSingleton<QdrantClient>(sp =>
            {
                var host = configuration["Qdrant:Host"] ?? "localhost";
                var port = configuration.GetValue<int>("Qdrant:Port", 6334);  // Qdrant gRPC 默认端口

                // Qdrant 客户端默认使用 gRPC，端口 6334
                var client = new QdrantClient(host, port);
                return client;
            });

            // 注册配置选项
            services.Configure<QdrantVectorStoreOptions>(options =>
            {
                configuration.GetSection("Qdrant").Bind(options);
            });

            // 注册 IVectorStore（根据环境选择实现）
            var useMemoryVectorStore = configuration.GetValue<bool>("UseMemoryVectorStore", false);

            if (useMemoryVectorStore)
            {
                // 开发/测试环境：使用内存向量存储
                services.AddSingleton<IVectorStore, MemoryVectorStore>();
            }
            else
            {
                // 生产环境：使用 Qdrant
                services.AddSingleton<IVectorStore, QdrantVectorStore>();
            }

            return services;
        }
    }
}
