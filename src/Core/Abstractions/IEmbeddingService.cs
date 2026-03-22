using CKY.MultiAgentFramework.Core.Models.RAG;

namespace CKY.MultiAgentFramework.Core.Abstractions;

/// <summary>
/// 嵌入服务接口 — 将文本转换为向量
/// </summary>
/// <remarks>
/// <para>Core 层抽象，具体实现在 Infrastructure 层（如 ZhipuAI、Dashscope）</para>
/// </remarks>
public interface IEmbeddingService
{
    /// <summary>
    /// 将单段文本转换为向量
    /// </summary>
    Task<float[]> GetEmbeddingAsync(string text, CancellationToken ct = default);

    /// <summary>
    /// 将多段文本批量转换为向量
    /// </summary>
    Task<List<float[]>> GetEmbeddingsAsync(IEnumerable<string> texts, CancellationToken ct = default);

    /// <summary>
    /// 向量维度
    /// </summary>
    int VectorDimension { get; }
}
