using CKY.MultiAgentFramework.Core.Models.RAG;

namespace CKY.MultiAgentFramework.Core.Abstractions;

/// <summary>
/// RAG 检索服务接口 — 检索增强生成的检索管线
/// </summary>
/// <remarks>
/// <para>核心 RAG 检索流程：查询 → 嵌入 → 向量搜索 → 返回相关分块</para>
/// <para>Services 层提供默认实现，依赖 IEmbeddingService 和 IVectorStore</para>
/// </remarks>
public interface IRagRetriever
{
    /// <summary>
    /// 检索与查询最相关的文档分块
    /// </summary>
    /// <param name="query">用户查询文本</param>
    /// <param name="collectionName">知识库集合名称</param>
    /// <param name="topK">返回的最大结果数</param>
    /// <param name="scoreThreshold">最低相似度阈值</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>排序后的检索结果列表</returns>
    Task<List<RetrievalResult>> RetrieveAsync(
        string query,
        string collectionName,
        int topK = 5,
        float scoreThreshold = 0.7f,
        CancellationToken ct = default);
}
