using CKY.MultiAgentFramework.Core.Abstractions;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CKY.MultiAgentFramework.Infrastructure.Embedding;

/// <summary>
/// OpenAI Embedding 服务实现
/// </summary>
/// <remarks>
/// <para>支持 text-embedding-3-small (1536维) 和 text-embedding-3-large (3072维) 模型</para>
/// <para>API 文档: https://platform.openai.com/docs/api-reference/embeddings</para>
/// <para>HttpClient 必须通过依赖注入提供，配置 BaseAddress 和 Authorization 头</para>
/// <para>OpenAI embedding API 原生支持批量输入（input 可为字符串数组），单次最多 2048 个</para>
/// </remarks>
public class OpenAiEmbeddingService : IEmbeddingService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OpenAiEmbeddingService> _logger;
    private readonly string _model;
    private readonly int _vectorDimension;

    /// <summary>
    /// 默认模型
    /// </summary>
    public const string DefaultModel = "text-embedding-3-small";

    /// <summary>
    /// 默认向量维度（text-embedding-3-small）
    /// </summary>
    public const int DefaultDimension = 1536;

    /// <summary>
    /// 批量请求最大文本数（OpenAI 限制 2048）
    /// </summary>
    private const int MaxBatchSize = 2048;

    public OpenAiEmbeddingService(
        HttpClient httpClient,
        ILogger<OpenAiEmbeddingService> logger,
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

        _logger.LogDebug("[OpenAI Embedding] 请求单文本嵌入, 文本长度: {Length}", text.Length);

        var request = new EmbeddingRequest
        {
            Model = _model,
            Input = text
        };

        var response = await SendRequestAsync(request, ct);

        if (response.Data is not { Count: > 0 })
        {
            _logger.LogWarning("[OpenAI Embedding] 返回空嵌入结果");
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

        _logger.LogDebug("[OpenAI Embedding] 请求批量嵌入, 文本数: {Count}", textList.Count);

        var allEmbeddings = new List<float[]>();

        for (int i = 0; i < textList.Count; i += MaxBatchSize)
        {
            ct.ThrowIfCancellationRequested();

            var batch = textList.Skip(i).Take(MaxBatchSize).ToList();
            var batchEmbeddings = await GetBatchEmbeddingsAsync(batch, ct);
            allEmbeddings.AddRange(batchEmbeddings);

            _logger.LogDebug("[OpenAI Embedding] 批次 {Batch}/{Total} 完成",
                (i / MaxBatchSize) + 1, (textList.Count + MaxBatchSize - 1) / MaxBatchSize);
        }

        return allEmbeddings;
    }

    private async Task<List<float[]>> GetBatchEmbeddingsAsync(
        List<string> texts, CancellationToken ct)
    {
        // OpenAI embedding API 原生支持批量输入
        var request = new BatchEmbeddingRequest
        {
            Model = _model,
            Input = texts
        };

        var json = JsonSerializer.Serialize(request, JsonSerializerOptions);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        var httpResponse = await _httpClient.PostAsync("embeddings", content, ct);

        if (!httpResponse.IsSuccessStatusCode)
        {
            var errorBody = await httpResponse.Content.ReadAsStringAsync(ct);
            _logger.LogError("[OpenAI Embedding] API 错误: {StatusCode}, Body: {Body}",
                httpResponse.StatusCode, errorBody);
            httpResponse.EnsureSuccessStatusCode();
        }

        var response = await httpResponse.Content.ReadFromJsonAsync<EmbeddingResponse>(
            JsonSerializerOptions, ct);

        if (response is null)
            throw new InvalidOperationException("OpenAI Embedding API 返回空响应");

        // 按 index 排序确保与输入顺序一致
        var sorted = (response.Data ?? [])
            .OrderBy(d => d.Index)
            .Select(d => d.Embedding)
            .ToList();

        // 对缺失数据填充零向量
        while (sorted.Count < texts.Count)
        {
            _logger.LogWarning("[OpenAI Embedding] 批量嵌入缺少结果, 使用零向量替代");
            sorted.Add(new float[_vectorDimension]);
        }

        return sorted;
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
            _logger.LogError("[OpenAI Embedding] API 错误: {StatusCode}, Body: {Body}",
                response.StatusCode, errorBody);
            response.EnsureSuccessStatusCode();
        }

        var result = await response.Content.ReadFromJsonAsync<EmbeddingResponse>(
            JsonSerializerOptions, ct);

        if (result is null)
            throw new InvalidOperationException("OpenAI Embedding API 返回空响应");

        _logger.LogDebug("[OpenAI Embedding] 使用 tokens: {Usage}",
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

    private sealed class BatchEmbeddingRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("input")]
        public List<string> Input { get; set; } = [];
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

        [JsonPropertyName("total_tokens")]
        public int TotalTokens { get; set; }
    }

    #endregion
}
