using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Core.Agents;
using CKY.MultiAgentFramework.Core.Models.LLM;
using CKY.MultiAgentFramework.Core.Models.Task;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace CKY.MultiAgentFramework.Core.Agents.Specialized
{
    /// <summary>
    /// 文本向量化 Agent
    /// 负责将文本转换为向量表示，用于语义搜索、相似度计算等
    /// </summary>
    /// <remarks>
    /// 使用场景：
    /// - 语义搜索：将查询文本向量化，在向量数据库中搜索相似文档
    /// - 文档聚类：对文档向量进行聚类分析
    /// - 相似度计算：计算两段文本的语义相似度
    /// - 推荐系统：基于内容的相似度推荐
    /// </remarks>
    public class EmbeddingAgent : MafAgentBase
    {
        public override string AgentId => "embedding-agent-001";
        public override string Name => "EmbeddingAgent";
        public override string Description => "文本向量化Agent，将文本转换为向量表示，用于语义搜索和相似度计算";
        public override IReadOnlyList<string> Capabilities => new[]
        {
            "text-embedding",
            "semantic-search",
            "similarity-calculation",
            "vector-generation"
        };

        public EmbeddingAgent(
            ILlmAgentRegistry llmRegistry,
            ILogger<EmbeddingAgent> logger)
            : base(llmRegistry, logger)
        {
        }

        /// <summary>
        /// 执行业务逻辑：文本向量化
        /// </summary>
        public override async Task<MafTaskResponse> ExecuteBusinessLogicAsync(
            MafTaskRequest request,
            CancellationToken ct = default)
        {
            try
            {
                var text = request.UserInput;
                if (string.IsNullOrWhiteSpace(text))
                {
                    return new MafTaskResponse
                    {
                        TaskId = request.TaskId,
                        Success = false,
                        Error = "输入文本不能为空"
                    };
                }

                Logger.LogInformation("[Embedding] 开始向量化文本，长度: {Length}", text.Length);

                // 调用 LLM 的 Embedding API
                var vectorJson = await CallLlmAsync(text, LlmScenario.Embed, null, ct);

                // 解析向量结果
                var vector = JsonSerializer.Deserialize<float[]>(vectorJson);
                if (vector == null || vector.Length == 0)
                {
                    return new MafTaskResponse
                    {
                        TaskId = request.TaskId,
                        Success = false,
                        Error = "向量化失败，返回了空向量"
                    };
                }

                Logger.LogInformation("[Embedding] 向量化完成，向量维度: {Dimension}", vector.Length);

                return new MafTaskResponse
                {
                    TaskId = request.TaskId,
                    Success = true,
                    Result = $"文本已成功向量化，维度: {vector.Length}",
                    Data = new Dictionary<string, object>
                    {
                        ["vector"] = vector,
                        ["dimension"] = vector.Length,
                        ["text_length"] = text.Length
                    }
                };
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "[Embedding] 向量化失败");
                return new MafTaskResponse
                {
                    TaskId = request.TaskId,
                    Success = false,
                    Error = $"向量化失败: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// 批量向量化多个文本
        /// </summary>
        public async Task<MafTaskResponse> EmbedBatchAsync(
            string[] texts,
            CancellationToken ct = default)
        {
            try
            {
                if (texts == null || texts.Length == 0)
                {
                    return new MafTaskResponse
                    {
                        Success = false,
                        Error = "文本数组不能为空"
                    };
                }

                Logger.LogInformation("[Embedding] 开始批量向量化，数量: {Count}", texts.Length);

                // 批量调用 LLM
                var results = await CallLlmBatchAsync(texts, LlmScenario.Embed, null, ct);

                var vectors = new List<float[]>();
                for (int i = 0; i < results.Length; i++)
                {
                    var vector = JsonSerializer.Deserialize<float[]>(results[i]);
                    if (vector != null && vector.Length > 0)
                    {
                        vectors.Add(vector);
                    }
                }

                Logger.LogInformation("[Embedding] 批量向量化完成，成功: {Success}/{Total}", vectors.Count, texts.Length);

                return new MafTaskResponse
                {
                    Success = true,
                    Result = $"批量向量化完成，成功处理 {vectors.Count}/{texts.Length} 个文本",
                    Data = new Dictionary<string, object>
                    {
                        ["vectors"] = vectors,
                        ["count"] = vectors.Count,
                        ["total"] = texts.Length
                    }
                };
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "[Embedding] 批量向量化失败");
                return new MafTaskResponse
                {
                    Success = false,
                    Error = $"批量向量化失败: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// 计算两个向量的余弦相似度
        /// </summary>
        public float CalculateCosineSimilarity(float[] vector1, float[] vector2)
        {
            if (vector1.Length != vector2.Length)
                throw new ArgumentException("向量维度不匹配");

            float dotProduct = 0;
            float magnitude1 = 0;
            float magnitude2 = 0;

            for (int i = 0; i < vector1.Length; i++)
            {
                dotProduct += vector1[i] * vector2[i];
                magnitude1 += vector1[i] * vector1[i];
                magnitude2 += vector2[i] * vector2[i];
            }

            magnitude1 = (float)Math.Sqrt(magnitude1);
            magnitude2 = (float)Math.Sqrt(magnitude2);

            if (magnitude1 == 0 || magnitude2 == 0)
                return 0;

            return dotProduct / (magnitude1 * magnitude2);
        }
    }
}
