using CKY.MultiAgentFramework.Core.Models.RAG;

namespace CKY.MultiAgentFramework.Core.Abstractions;

/// <summary>
/// RAG 管线接口 — 完整的检索增强生成管线
/// </summary>
/// <remarks>
/// <para>编排完整 RAG 流程：文档摄入 + 知识检索 + 上下文增强查询</para>
/// <para>Services 层提供默认实现</para>
/// </remarks>
public interface IRagPipeline
{
    /// <summary>
    /// 摄入文档：解析 → 分块 → 嵌入 → 存储
    /// </summary>
    /// <param name="documentId">文档唯一标识</param>
    /// <param name="text">文档文本内容</param>
    /// <param name="collectionName">目标知识库集合名称</param>
    /// <param name="config">分块配置</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>生成的分块列表</returns>
    Task<List<DocumentChunk>> IngestAsync(
        string documentId,
        string text,
        string collectionName,
        ChunkingConfig? config = null,
        CancellationToken ct = default);

    /// <summary>
    /// RAG 查询：检索相关知识 + 构建增强上下文
    /// </summary>
    /// <param name="request">查询请求</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>查询响应（含检索结果和知识上下文）</returns>
    Task<RagQueryResponse> QueryAsync(
        RagQueryRequest request,
        CancellationToken ct = default);
}
