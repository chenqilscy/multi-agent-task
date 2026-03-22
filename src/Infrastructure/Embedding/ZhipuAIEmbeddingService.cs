using CKY.MultiAgentFramework.Core.Abstractions;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CKY.MultiAgentFramework.Infrastructure.Embedding;

/// <summary>
/// 智谱 AI Embedding 服务实现
/// </summary>
/// <remarks>
/// <para>使用智谱AI的 embedding-3 模型，支持 2048 维向量</para>
/// <para>API 文档: https://open.bigmodel.cn/dev/api/vector/embedding-3</para>
/// <para>HttpClient 必须通过依赖注入提供，配置 BaseAddress 和 Authorization 头</para>
/// </remarks>
public class ZhipuAIEmbeddingService : IEmbeddingService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ZhipuAIEmbeddingService> _logger;
    private readonly string _model;
    private readonly int _vectorDimension;

    /// <summary>
    /// 默认模型
    /// </summary>
    public const string DefaultModel = "embedding-3";

    /// <summary>
    /// 默认向量维度
    /// </summary>
    public const int DefaultDimension = 2048;

    /// <summary>
    /// 批量请求最大文本数
    /// </summary>
    private const int MaxBatchSize = 64;

    public ZhipuAIEmbeddingService(
        HttpClient httpClient,
        ILogger<ZhipuAIEmbeddingService> logger,
        string model = DefaultModel,
        int vectorDimension = DefaultDimension)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _model = model;
        _vectorDimension = vectorDimension;
    }

    /// <inheritdoc />
    public int VectorDimension => _vectorDimension;

    /// <inheritdoc />
    public async Task<float[]> GetEmbeddingAsync(string text, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);

        _logger.LogDebug("[ZhipuAI Embedding] 请求单文本嵌入, 文本长度: {Length}", text.Length);

        var request = new EmbeddingRequest
        {
            Model = _model,
            Input = text
        };

        var response = await SendRequestAsync(request, ct);

        if (response.Data is not { Count: > 0 })
        {
            _logger.LogWarning("[ZhipuAI Embedding] 返回空嵌入结果");
            return Array.Empty<float>();
        }

        return response.Data[0].Embedding;
    }

    /// <inheritdoc />
    public async Task<List<float[]>> GetEmbeddingsAsync(IEnumerable<string> texts, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(texts);

        var textList = texts.Where(t => !string.IsNullOrWhiteSpace(t)).ToList();
        if (textList.Count == 0)
            return [];

        _logger.LogDebug("[ZhipuAI Embedding] 请求批量嵌入, 文本数: {Count}", textList.Count);

        var allEmbeddings = new List<float[]>();

        // 分批处理，每批最多 MaxBatchSize 个
        for (int i = 0; i < textList.Count; i += MaxBatchSize)
        {
            ct.ThrowIfCancellationRequested();

            var batch = textList.Skip(i).Take(MaxBatchSize).ToList();
            var batchEmbeddings = await GetBatchEmbeddingsAsync(batch, ct);
            allEmbeddings.AddRange(batchEmbeddings);

            _logger.LogDebug("[ZhipuAI Embedding] 批次 {Batch}/{Total} 完成",
                (i / MaxBatchSize) + 1, (textList.Count + MaxBatchSize - 1) / MaxBatchSize);
        }

        return allEmbeddings;
    }

    private async Task<List<float[]>> GetBatchEmbeddingsAsync(
        List<string> texts, CancellationToken ct)
    {
        // 智谱 AI embedding API 支持单次请求多个文本（通过逐条发送）
        // 为保持兼容性，逐条发送然后合并结果
        var results = new List<float[]>(texts.Count);

        foreach (var text in texts)
        {
            var request = new EmbeddingRequest
            {
                Model = _model,
                Input = text
            };

            var response = await SendRequestAsync(request, ct);

            if (response.Data is { Count: > 0 })
            {
                results.Add(response.Data[0].Embedding);
            }
            else
            {
                _logger.LogWarning("[ZhipuAI Embedding] 文本嵌入返回空结果, 使用零向量替代");
                results.Add(new float[_vectorDimension]);
            }
        }

        return results;
    }

    private async Task<EmbeddingResponse> SendRequestAsync(
        EmbeddingRequest request, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(request, JsonSerializerOptions);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("embeddings", content, ct);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct);
            _logger.LogError("[ZhipuAI Embedding] API 错误: {StatusCode}, Body: {Body}",
                response.StatusCode, errorBody);
            response.EnsureSuccessStatusCode();
        }

        var result = await response.Content.ReadFromJsonAsync<EmbeddingResponse>(
            JsonSerializerOptions, ct);

        if (result is null)
            throw new InvalidOperationException("智谱AI Embedding API 返回空响应");

        _logger.LogDebug("[ZhipuAI Embedding] 使用 tokens: {Usage}",
            result.Usage?.TotalTokens);

        return result;
    }

    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    #region API 模型

    private sealed class EmbeddingRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("input")]
        public string Input { get; set; } = string.Empty;
    }

    private sealed class EmbeddingResponse
    {
        [JsonPropertyName("object")]
        public string? Object { get; set; }

        [JsonPropertyName("data")]
        public List<EmbeddingData>? Data { get; set; }

        [JsonPropertyName("model")]
        public string? Model { get; set; }

        [JsonPropertyName("usage")]
        public EmbeddingUsage? Usage { get; set; }
    }

    private sealed class EmbeddingData
    {
        [JsonPropertyName("object")]
        public string? Object { get; set; }

        [JsonPropertyName("index")]
        public int Index { get; set; }

        [JsonPropertyName("embedding")]
        public float[] Embedding { get; set; } = [];
    }

    private sealed class EmbeddingUsage
    {
        [JsonPropertyName("prompt_tokens")]
        public int PromptTokens { get; set; }

        [JsonPropertyName("completion_tokens")]
        public int CompletionTokens { get; set; }

        [JsonPropertyName("total_tokens")]
        public int TotalTokens { get; set; }
    }

    #endregion
}
