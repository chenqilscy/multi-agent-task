using CKY.MultiAgentFramework.Core.Models.RAG;

namespace CKY.MultiAgentFramework.Core.Abstractions;

/// <summary>
/// 文档分块器接口 — 将文本切分为适合嵌入的分块
/// </summary>
/// <remarks>
/// <para>Core 层抽象，Services 层提供内置实现（固定大小、结构感知等）</para>
/// </remarks>
public interface IDocumentChunker
{
    /// <summary>
    /// 将文本切分为多个分块
    /// </summary>
    /// <param name="text">原始文本</param>
    /// <param name="documentId">所属文档ID</param>
    /// <param name="config">分块配置</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>分块列表</returns>
    Task<List<DocumentChunk>> ChunkAsync(
        string text,
        string documentId,
        ChunkingConfig? config = null,
        CancellationToken ct = default);
}
