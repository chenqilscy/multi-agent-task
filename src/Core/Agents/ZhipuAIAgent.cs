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
    /// 智谱 AI LLM Agent 实现
    /// </summary>
    /// <remarks>
    /// HttpClient 必须通过依赖注入提供，不应在构造函数中创建新实例。
    /// 在 Program.cs 中配置：
    /// <code>
    /// services.AddHttpClient<ZhipuAIAgent>(client =>
    /// {
    ///     client.BaseAddress = new Uri("https://open.bigmodel.cn/api/paas/v4/chat/completions");
    ///     client.DefaultRequestHeaders.Add("Authorization", "Bearer {API_KEY}");
    ///     client.DefaultRequestHeaders.Add("Content-Type", "application/json");
    /// });
    /// services.AddSingleton<LlmResiliencePipeline>();
    /// </code>
    /// </remarks>
    public class ZhipuAIAgent : LlmAgent
    {
        private readonly HttpClient _httpClient;
        private readonly LlmResiliencePipeline _resiliencePipeline;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="config">LLM 提供商配置</param>
        /// <param name="logger">日志记录器</param>
        /// <param name="httpClient">HttpClient（必须通过依赖注入提供）</param>
        /// <param name="resiliencePipeline">弹性管道（可选，默认使用标准配置）</param>
        /// <exception cref="ArgumentNullException">当必需参数为 null 时抛出</exception>
        public ZhipuAIAgent(
            LlmProviderConfig config,
            ILogger<ZhipuAIAgent> logger,
            HttpClient httpClient,
            LlmResiliencePipeline? resiliencePipeline = null)
            : base(config, logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _resiliencePipeline = resiliencePipeline ?? new LlmResiliencePipeline(logger);
        }

        /// <summary>
        /// 执行智谱 AI LLM 调用（带弹性保护）
        /// </summary>
        public override async Task<string> ExecuteAsync(
            string modelId,
            string prompt,
            string? systemPrompt = null,
            CancellationToken ct = default)
        {
            return await _resiliencePipeline.ExecuteAsync(
                AgentId,
                async (innerCt) => await ExecuteInternalAsync(modelId, prompt, systemPrompt, innerCt),
                timeout: TimeSpan.FromSeconds(30),
                ct);
        }

        /// <summary>
        /// 内部执行方法（实际的 API 调用逻辑）
        /// </summary>
        private async Task<string> ExecuteInternalAsync(
            string modelId,
            string prompt,
            string? systemPrompt,
            CancellationToken ct)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                Logger.LogInformation("[ZhipuAI] Calling model: {ModelId}", modelId);

                // 构建请求体
                var requestBody = BuildRequestBody(modelId, prompt, systemPrompt);
                var jsonContent = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                // 发送请求
                var response = await _httpClient.PostAsync("chat/completions", content, ct);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync(ct);
                var result = JsonSerializer.Deserialize<JsonElement>(responseContent);

                // 提取返回的文本内容
                var text = ExtractContent(result);

                stopwatch.Stop();
                Logger.LogInformation("[ZhipuAI] Success: {ModelId}, Duration: {Duration}s",
                    modelId, stopwatch.Elapsed.TotalSeconds);

                return text;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                Logger.LogError(ex, "[ZhipuAI] Error: {ModelId}, Duration: {Duration}s",
                    modelId, stopwatch.Elapsed.TotalSeconds);

                // 转换为自定义异常
                throw ConvertToLlmException(ex, modelId);
            }
        }

        /// <summary>
        /// 构建请求体
        /// </summary>
        /// <remarks>
        /// 注意：场景（Scenario）在 Agent 创建时已确定，这里直接使用配置中的参数。
        /// 如果需要不同场景的特殊处理，应该在创建专门的 Agent 时处理。
        /// </remarks>
        private object BuildRequestBody(
            string modelId,
            string prompt,
            string? systemPrompt)
        {
            var messages = new List<object>();

            // 添加系统提示词
            if (!string.IsNullOrWhiteSpace(systemPrompt))
            {
                messages.Add(new
                {
                    role = "system",
                    content = systemPrompt
                });
            }

            // 添加用户消息
            messages.Add(new
            {
                role = "user",
                content = prompt
            });

            return new
            {
                model = modelId,
                messages = messages,
                temperature = Config.Temperature,
                top_p = 0.9,
                max_tokens = Config.MaxTokens
            };
        }

        /// <summary>
        /// 从 API 响应中提取文本内容
        /// </summary>
        private string ExtractContent(JsonElement result)
        {
            try
            {
                if (result.TryGetProperty("choices", out var choices) &&
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
                Logger.LogWarning("[ZhipuAI] Unexpected response format: {Response}", result.ToString());
                return result.ToString();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "[ZhipuAI] Failed to extract content from response");
                throw;
            }
        }

        /// <summary>
        /// 获取智谱AI支持的模型列表
        /// </summary>
        public static string[] GetSupportedModels()
        {
            return new[]
            {
                "glm-4-plus",      // GLM-4 Plus（最强）
                "glm-4",           // GLM-4（标准版）
                "glm-4-air",       // GLM-4 Air（轻量版）
                "glm-4-flash",     // GLM-4 Flash（极速版）
                "glm-3-turbo"      // GLM-3 Turbo（旧版）
            };
        }

        /// <summary>
        /// 获取推荐的模型（基于成本和性能平衡）
        /// </summary>
        public static string GetRecommendedModel()
        {
            return "glm-4"; // 平衡性能和成本
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
