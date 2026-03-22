namespace CKY.MultiAgentFramework.Core.Models.RAG;

/// <summary>
/// 文档分块
/// </summary>
public class DocumentChunk
{
    /// <summary>分块ID</summary>
    public string ChunkId { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>所属文档ID</summary>
    public string DocumentId { get; set; } = string.Empty;

    /// <summary>分块索引（在文档中的顺序）</summary>
    public int ChunkIndex { get; set; }

    /// <summary>分块文本内容</summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>向量ID（对应向量存储中的ID）</summary>
    public string? VectorId { get; set; }

    /// <summary>附加元数据</summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// 检索结果（分块 + 相似度评分）
/// </summary>
public class RetrievalResult
{
    /// <summary>分块内容</summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>相似度评分（0-1）</summary>
    public float Score { get; set; }

    /// <summary>所属文档ID</summary>
    public string DocumentId { get; set; } = string.Empty;

    /// <summary>分块索引</summary>
    public int ChunkIndex { get; set; }

    /// <summary>附加元数据</summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// 分块配置
/// </summary>
public class ChunkingConfig
{
    /// <summary>最大分块大小（字符数）</summary>
    public int MaxChunkSize { get; set; } = 800;

    /// <summary>分块重叠比例（0-1）</summary>
    public double OverlapRatio { get; set; } = 0.2;

    /// <summary>是否尊重文档结构（标题、段落边界）</summary>
    public bool RespectStructure { get; set; } = true;
}

/// <summary>
/// RAG查询请求
/// </summary>
public class RagQueryRequest
{
    /// <summary>用户查询</summary>
    public string Query { get; set; } = string.Empty;

    /// <summary>知识库集合名称</summary>
    public string CollectionName { get; set; } = string.Empty;

    /// <summary>返回最相关的分块数量</summary>
    public int TopK { get; set; } = 5;

    /// <summary>最低相似度阈值（0-1）</summary>
    public float ScoreThreshold { get; set; } = 0.7f;

    /// <summary>对话历史（用于上下文感知检索）</summary>
    public List<string>? ConversationHistory { get; set; }
}

/// <summary>
/// RAG查询响应
/// </summary>
public class RagQueryResponse
{
    /// <summary>AI生成的回答</summary>
    public string Answer { get; set; } = string.Empty;

    /// <summary>检索到的相关分块</summary>
    public List<RetrievalResult> RetrievedChunks { get; set; } = new();

    /// <summary>是否使用了知识库上下文</summary>
    public bool UsedKnowledgeContext { get; set; }
}
