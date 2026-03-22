using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Core.Models.RAG;
using Microsoft.Extensions.Logging;

namespace CKY.MultiAgentFramework.Services.RAG;

/// <summary>
/// 默认 RAG 管线 — 编排文档摄入和知识检索
/// </summary>
public class DefaultRagPipeline : IRagPipeline
{
    private readonly IDocumentChunker _chunker;
    private readonly IEmbeddingService _embeddingService;
    private readonly IVectorStore _vectorStore;
    private readonly IRagRetriever _retriever;
    private readonly ILogger<DefaultRagPipeline> _logger;

    public DefaultRagPipeline(
        IDocumentChunker chunker,
        IEmbeddingService embeddingService,
        IVectorStore vectorStore,
        IRagRetriever retriever,
        ILogger<DefaultRagPipeline> logger)
    {
        _chunker = chunker;
        _embeddingService = embeddingService;
        _vectorStore = vectorStore;
        _retriever = retriever;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<List<DocumentChunk>> IngestAsync(
        string documentId,
        string text,
        string collectionName,
        ChunkingConfig? config = null,
        CancellationToken ct = default)
    {
        _logger.LogInformation("开始摄入文档: documentId={DocumentId}, collection={Collection}",
            documentId, collectionName);

        // 1. 分块
        var chunks = await _chunker.ChunkAsync(text, documentId, config, ct);
        _logger.LogDebug("文档分块完成: {Count} 个分块", chunks.Count);

        if (chunks.Count == 0)
            return chunks;

        // 2. 批量生成嵌入
        var texts = chunks.Select(c => c.Content).ToList();
        var embeddings = await _embeddingService.GetEmbeddingsAsync(texts, ct);

        // 3. 确保集合存在
        await _vectorStore.CreateCollectionAsync(
            collectionName, _embeddingService.VectorDimension, ct);

        // 4. 构建向量点并存储
        var points = new List<VectorPoint>();
        for (int i = 0; i < chunks.Count; i++)
        {
            var chunk = chunks[i];
            var vectorId = $"{documentId}_{chunk.ChunkIndex}";
            chunk.VectorId = vectorId;

            points.Add(new VectorPoint
            {
                Id = vectorId,
                Vector = embeddings[i],
                Metadata = new Dictionary<string, object>
                {
                    ["content"] = chunk.Content,
                    ["document_id"] = documentId,
                    ["chunk_index"] = chunk.ChunkIndex
                }
            });
        }

        await _vectorStore.InsertAsync(collectionName, points, ct);

        _logger.LogInformation("文档摄入完成: documentId={DocumentId}, {Count} 个分块",
            documentId, chunks.Count);

        return chunks;
    }

    /// <inheritdoc/>
    public async Task<RagQueryResponse> QueryAsync(
        RagQueryRequest request,
        CancellationToken ct = default)
    {
        _logger.LogInformation("RAG 查询: query={Query}, collection={Collection}",
            request.Query, request.CollectionName);

        // 1. 检索相关分块
        var retrievedChunks = await _retriever.RetrieveAsync(
            request.Query,
            request.CollectionName,
            request.TopK,
            request.ScoreThreshold,
            ct);

        // 2. 构建响应
        var response = new RagQueryResponse
        {
            RetrievedChunks = retrievedChunks,
            UsedKnowledgeContext = retrievedChunks.Count > 0
        };

        if (retrievedChunks.Count > 0)
        {
            // 构建知识上下文文本（供 LLM Agent 使用）
            response.Answer = BuildKnowledgeContext(retrievedChunks);
        }

        _logger.LogInformation("RAG 查询完成: {Count} 个相关分块",
            retrievedChunks.Count);

        return response;
    }

    /// <summary>
    /// 将检索结果构建为知识上下文字符串
    /// </summary>
    private static string BuildKnowledgeContext(List<RetrievalResult> chunks)
    {
        var lines = new List<string> { "以下是与查询相关的知识上下文：", "" };

        for (int i = 0; i < chunks.Count; i++)
        {
            lines.Add($"[{i + 1}]（相似度：{chunks[i].Score:F2}）");
            lines.Add(chunks[i].Content);
            lines.Add("");
        }

        return string.Join('\n', lines);
    }
}
