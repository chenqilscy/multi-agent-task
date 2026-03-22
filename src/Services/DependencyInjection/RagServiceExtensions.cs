using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Services.RAG;
using Microsoft.Extensions.DependencyInjection;

namespace CKY.MultiAgentFramework.Services.DependencyInjection;

/// <summary>
/// RAG 服务注册扩展方法
/// </summary>
public static class RagServiceExtensions
{
    /// <summary>
    /// 注册 RAG 管线服务（分块器、检索器、管线）
    /// </summary>
    /// <remarks>
    /// <para>需要事先注册 IEmbeddingService（Infrastructure 层）和 IVectorStore。</para>
    /// </remarks>
    public static IServiceCollection AddMafRagServices(this IServiceCollection services)
    {
        services.AddSingleton<IDocumentChunker, FixedSizeDocumentChunker>();
        services.AddScoped<IRagRetriever, DefaultRagRetriever>();
        services.AddScoped<IRagPipeline, DefaultRagPipeline>();

        return services;
    }
}
