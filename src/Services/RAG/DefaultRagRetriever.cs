using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Core.Models.RAG;
using Microsoft.Extensions.Logging;

namespace CKY.MultiAgentFramework.Services.RAG;

/// <summary>
/// 默认 RAG 检索服务 — 查询嵌入 + 向量搜索
/// </summary>
public class DefaultRagRetriever : IRagRetriever
{
    private readonly IEmbeddingService _embeddingService;
    private readonly IVectorStore _vectorStore;
    private readonly ILogger<DefaultRagRetriever> _logger;

    public DefaultRagRetriever(
        IEmbeddingService embeddingService,
        IVectorStore vectorStore,
        ILogger<DefaultRagRetriever> logger)
    {
        _embeddingService = embeddingService;
        _vectorStore = vectorStore;
        _logger = logger;
    }

    public async Task<List<RetrievalResult>> RetrieveAsync(
        string query,
        string collectionName,
        int topK = 5,
        float scoreThreshold = 0.7f,
        CancellationToken ct = default)
    {
        _logger.LogDebug("RAG 检索: query={Query}, collection={Collection}, topK={TopK}",
            query, collectionName, topK);

        // 1. 将查询转换为向量
        var queryVector = await _embeddingService.GetEmbeddingAsync(query, ct);

        // 2. 向量相似度搜索
        var searchResults = await _vectorStore.SearchAsync(
            collectionName, queryVector, topK, ct: ct);

        // 3. 过滤低分结果，映射为 RetrievalResult
        var results = searchResults
            .Where(r => r.Score >= scoreThreshold)
            .Select(r => new RetrievalResult
            {
                Content = r.Metadata.TryGetValue("content", out var content)
                    ? content?.ToString() ?? string.Empty
                    : string.Empty,
                Score = r.Score,
                DocumentId = r.Metadata.TryGetValue("document_id", out var docId)
                    ? docId?.ToString() ?? string.Empty
                    : string.Empty,
                ChunkIndex = r.Metadata.TryGetValue("chunk_index", out var idx)
                    ? Convert.ToInt32(idx)
                    : 0,
                Metadata = r.Metadata
            })
            .OrderByDescending(r => r.Score)
            .ToList();

        _logger.LogDebug("RAG 检索完成: {Count} 个结果 (阈值 {Threshold})",
            results.Count, scoreThreshold);

        return results;
    }
}
