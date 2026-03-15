using CKY.MultiAgentFramework.Core.Exceptions;
using CKY.MultiAgentFramework.Core.Models.LLM;
using CKY.MultiAgentFramework.Core.Resilience;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text;
using System.Text.Json;

namespace CKY.MultiAgentFramework.Core.Agents
{
    /// <summary>
    /// 通义千问 LLM Agent 实现
    /// </summary>
    /// <remarks>
    /// HttpClient 必须通过依赖注入提供，不应在构造函数中创建新实例。
    /// 在 Program.cs 中配置：
    /// <code>
    /// services.AddHttpClient<QwenAIAgent>(client =>
    /// {
    ///     client.BaseAddress = new Uri("https://dashscope.aliyuncs.com/api/v1/services/aigc/text-generation/generation");
    ///     client.DefaultRequestHeaders.Add("Authorization", "Bearer {API_KEY}");
    ///     client.DefaultRequestHeaders.Add("Content-Type", "application/json");
    /// });
    /// services.AddSingleton<LlmResiliencePipeline>();
    /// </code>
    /// </remarks>
    public class QwenAIAgent : MafAiAgent
    {
        private readonly HttpClient _httpClient;
        private readonly LlmResiliencePipeline _resiliencePipeline;

        /// <summary>
        /// 构造函数
        /// </summary>
        public QwenAIAgent(
            LlmProviderConfig config,
            ILogger<QwenAIAgent> logger,
            HttpClient httpClient,
            LlmResiliencePipeline? resiliencePipeline = null)
            : base(config, logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _resiliencePipeline = resiliencePipeline ?? new LlmResiliencePipeline(logger);
        }

        /// <summary>
        /// 执行通义千问 LLM 调用（带弹性保护）
        /// </summary>
        public override async Task<string> ExecuteAsync(
            string modelId,
            string prompt,
            string? systemPrompt = null,
            CancellationToken ct = default)
        {
            // 从配置获取主场景，默认使用 Chat
            var scenario = Config.SupportedScenarios.Any() ? Config.SupportedScenarios.First() : LlmScenario.Chat;

            return await _resiliencePipeline.ExecuteAsync(
                AgentId,
                async (innerCt) => await ExecuteInternalAsync(modelId, prompt, scenario, systemPrompt, innerCt),
                timeout: TimeSpan.FromSeconds(30),
                ct);
        }

        /// <summary>
        /// 内部执行方法（实际的 API 调用逻辑）
        /// </summary>
        private async Task<string> ExecuteInternalAsync(
            string modelId,
            string prompt,
            LlmScenario scenario,
            string? systemPrompt,
            CancellationToken ct)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                Logger.LogInformation("[QwenAI] Calling model: {ModelId}, Scenario: {Scenario}", modelId, scenario);

                // 构建请求体
                var requestBody = BuildRequestBody(modelId, prompt, scenario, systemPrompt);
                var jsonContent = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                // 发送请求（BaseAddress 已通过 DI 配置）
                var response = await _httpClient.PostAsync("", content, ct);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync(ct);
                var result = JsonSerializer.Deserialize<JsonElement>(responseContent);

                // 提取返回的文本内容
                var text = ExtractContent(result);

                stopwatch.Stop();
                Logger.LogInformation("[QwenAI] Success: {ModelId}, Duration: {Duration}s",
                    modelId, stopwatch.Elapsed.TotalSeconds);

                return text;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                Logger.LogError(ex, "[QwenAI] Error: {ModelId}, Duration: {Duration}s",
                    modelId, stopwatch.Elapsed.TotalSeconds);

                // 转换为自定义异常
                throw ConvertToLlmException(ex, modelId);
            }
        }

        /// <summary>
        /// 执行流式通义千问 LLM 调用（带弹性保护）
        /// </summary>
        public override async IAsyncEnumerable<string> ExecuteStreamingAsync(
            string modelId,
            string prompt,
            string? systemPrompt = null,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
        {
            // Streaming support pending: currently falls back to non-streaming execution
            // For now, fallback to non-streaming
            var result = await ExecuteAsync(modelId, prompt, systemPrompt, ct);
            yield return result;
        }

        /// <summary>
        /// 构建请求体
        /// </summary>
        private object BuildRequestBody(
            string modelId,
            string prompt,
            LlmScenario scenario,
            string? systemPrompt)
        {
            // 根据场景构建不同格式的消息
            var (systemMsg, userPrompt) = scenario switch
            {
                LlmScenario.Chat => (systemPrompt ?? "你是一个有用的AI助手。", prompt),
                LlmScenario.Intent => ("你是意图识别专家", $"请识别以下用户意图：{prompt}\n\n请以JSON格式返回，包含 intent（意图类型）和 confidence（置信度）字段。"),
                LlmScenario.Image => ("你是图像描述专家", prompt),
                LlmScenario.Video => ("你是视频内容分析专家", prompt),
                _ => ("你是一个有用的AI助手。", prompt)
            };

            return new
            {
                model = modelId,
                input = new
                {
                    messages = new[]
                    {
                        new
                        {
                            role = "system",
                            content = systemMsg
                        },
                        new
                        {
                            role = "user",
                            content = userPrompt
                        }
                    }
                },
                parameters = new
                {
                    result_format = "message",
                    temperature = 0.7,
                    top_p = 0.9,
                    max_tokens = 2000
                }
            };
        }

        /// <summary>
        /// 从 API 响应中提取文本内容
        /// </summary>
        private string ExtractContent(JsonElement result)
        {
            try
            {
                // 通义千问的响应格式: { output: { choices: [{ message: { content: "..." } }] } }
                if (result.TryGetProperty("output", out var output) &&
                    output.TryGetProperty("choices", out var choices) &&
                    choices.ValueKind == JsonValueKind.Array &&
                    choices.GetArrayLength() > 0)
                {
                    var firstChoice = choices[0];
                    if (firstChoice.TryGetProperty("message", out var message) &&
                        message.TryGetProperty("content", out var content))
                    {
                        return content.GetString() ?? string.Empty;
                    }
                }

                // 如果格式不符合预期，返回原始 JSON
                Logger.LogWarning("[QwenAI] Unexpected response format: {Response}", result.ToString());
                return result.ToString();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "[QwenAI] Failed to extract content from response");
                throw;
            }
        }

        /// <summary>
        /// 获取通义千问支持的模型列表
        /// </summary>
        public static string[] GetSupportedModels()
        {
            return new[]
            {
                "qwen-max",           // 通义千问-Max（最强）
                "qwen-plus",          // 通义千问-Plus（标准版）
                "qwen-turbo",         // 通义千问-Turbo（高速版）
                "qwen-long",          // 通义千问-Long（长文本）
                "qwen-vl-max",        // 通义千问-VL-Max（视觉理解）
                "qwen-vl-plus"        // 通义千问-VL-Plus（视觉理解标准版）
            };
        }

        /// <summary>
        /// 获取推荐的模型（基于成本和性能平衡）
        /// </summary>
        public static string GetRecommendedModel()
        {
            return "qwen-plus"; // 平衡性能和成本
        }

        /// <summary>
        /// 将通用异常转换为 LlmServiceException
        /// </summary>
        private Exception ConvertToLlmException(Exception ex, string modelId)
        {
            return ex switch
            {
                HttpRequestException httpEx when httpEx.StatusCode == HttpStatusCode.TooManyRequests
                    => new LlmServiceException(
                        $"Rate limit exceeded for {Config.ProviderName} model {modelId}",
                        statusCode: 429,
                        isRateLimited: true),

                HttpRequestException httpEx when httpEx.StatusCode == HttpStatusCode.Unauthorized
                    => new LlmServiceException(
                        $"Authentication failed for {Config.ProviderName} model {modelId}",
                        statusCode: 401,
                        isRateLimited: false),

                HttpRequestException httpEx when httpEx.StatusCode == HttpStatusCode.BadRequest
                    => new LlmServiceException(
                        $"Bad request for {Config.ProviderName} model {modelId}",
                        statusCode: 400,
                        isRateLimited: false),

                HttpRequestException httpEx when httpEx.StatusCode == HttpStatusCode.ServiceUnavailable
                    => new LlmServiceException(
                        $"Service unavailable for {Config.ProviderName} model {modelId}",
                        statusCode: 503,
                        isRateLimited: true),

                HttpRequestException httpEx
                    => new LlmServiceException(
                        $"Network error calling {Config.ProviderName} model {modelId}",
                        statusCode: (int?)(httpEx.StatusCode),
                        isRateLimited: true),

                TaskCanceledException tcEx when tcEx.CancellationToken.IsCancellationRequested
                    => new LlmServiceException(
                        $"Request to {Config.ProviderName} model {modelId} was cancelled",
                        statusCode: null,
                        isRateLimited: false),

                TimeoutException
                    => new LlmServiceException(
                        $"Request to {Config.ProviderName} model {modelId} timed out",
                        statusCode: null,
                        isRateLimited: true),

                _ => new LlmServiceException(
                    $"Unexpected error calling {Config.ProviderName} model {modelId}: {ex.Message}",
                    statusCode: null,
                    isRateLimited: false)
            };
        }
    }
}
