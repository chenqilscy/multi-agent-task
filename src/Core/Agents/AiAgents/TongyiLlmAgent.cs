using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Core.Models.LLM;
using CKY.MultiAgentFramework.Core.Resilience;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace CKY.MultiAgentFramework.Core.Agents.Providers
{
    /// <summary>
    /// 通义千问 LLM Agent 实现（阿里云大语言模型）
    /// </summary>
    /// <remarks>
    /// 模型支持：
    /// - qwen-max: 旗舰模型（最强推理能力）
    /// - qwen-plus: 通用模型（平衡性能和成本）
    /// - qwen-turbo: 快速模型（低成本、低延迟）
    /// - qwen-long: 长文本模型（支持 100 万 tokens）
    ///
    /// API 特点：
    /// - 支持流式和非流式调用
    /// - 支持 function calling（函数调用）
    /// - 支持多轮对话（上下文管理）
    /// - 兼容 OpenAI 接口格式
    ///
    /// 配置示例：
    /// <code>
    /// var config = new LlmProviderConfig
    /// {
    ///     ProviderName = "Tongyi",
    ///     ApiKey = "your-api-key",
    ///     ModelId = "qwen-max",
    ///     BaseUrl = "https://dashscope.aliyuncs.com/compatible-mode/v1"
    /// };
    /// </code>
    ///
    /// 参考文档：https://help.aliyun.com/zh/dashscope/developer-reference/compatibility-of-openai-with-dashscope
    /// </remarks>
    public class TongyiLlmAgent : MafAiAgent
    {
        private readonly HttpClient _httpClient;
        private readonly ILlmResiliencePipeline? _resiliencePipeline;

        /// <summary>
        /// 初始化 TongyiLlmAgent 类的新实例
        /// </summary>
        /// <param name="config">LLM 配置</param>
        /// <param name="logger">日志记录器</param>
        /// <param name="httpClient">HTTP 客户端（可选，用于测试）</param>
        /// <param name="resiliencePipeline">弹性管道（可选）</param>
        public TongyiLlmAgent(
            LlmProviderConfig config,
            ILogger logger,
            HttpClient? httpClient = null,
            ILlmResiliencePipeline? resiliencePipeline = null)
            : base(config, logger)
        {
            _httpClient = httpClient ?? new HttpClient
            {
                BaseAddress = new Uri(config.ApiBaseUrl ?? "https://dashscope.aliyuncs.com/compatible-mode/v1")
            };
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {GetApiKey()}");
            _resiliencePipeline = resiliencePipeline;
        }

        /// <inheritdoc />
        public override async Task<string> ExecuteAsync(
            string modelId,
            string prompt,
            string? systemPrompt = null,
            CancellationToken ct = default)
        {
            if (_resiliencePipeline != null)
            {
                return await _resiliencePipeline.ExecuteAsync(
                    AgentId,
                    innerCt => ExecuteInternalAsync(modelId, prompt, systemPrompt, innerCt),
                    timeout: TimeSpan.FromSeconds(60),
                    ct);
            }

            return await ExecuteInternalAsync(modelId, prompt, systemPrompt, ct);
        }

        private async Task<string> ExecuteInternalAsync(
            string modelId,
            string prompt,
            string? systemPrompt,
            CancellationToken ct)
        {
            Logger.LogDebug("[TongyiLlmAgent] ExecuteAsync called with model: {Model}", modelId);

            try
            {
                // 构建请求体（兼容 OpenAI 格式）
                var requestBody = new
                {
                    model = modelId,
                    messages = BuildMessages(prompt, systemPrompt),
                    temperature = 0.7,
                    top_p = 0.9,
                    max_tokens = 2000
                };

                var jsonContent = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                // 发送请求
                var response = await _httpClient.PostAsync("chat/completions", content, ct);
                response.EnsureSuccessStatusCode();

                var responseBody = await response.Content.ReadAsStringAsync(ct);
                var result = JsonSerializer.Deserialize<TongyiResponse>(responseBody);

                if (result?.Choices?.Length > 0)
                {
                    return result.Choices[0].Message.Content;
                }

                throw new InvalidOperationException("Invalid response from Tongyi API");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "[TongyiLlmAgent] ExecuteAsync failed");
                throw;
            }
        }

        /// <inheritdoc />
        public override async IAsyncEnumerable<string> ExecuteStreamingAsync(
            string modelId,
            string prompt,
            string? systemPrompt = null,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
        {
            Logger.LogDebug("[TongyiLlmAgent] ExecuteStreamingAsync called with model: {Model}", modelId);

            // 构建请求体（流式）
            var requestBody = new
            {
                model = modelId,
                messages = BuildMessages(prompt, systemPrompt),
                temperature = 0.7,
                top_p = 0.9,
                max_tokens = 2000,
                stream = true
            };

            var jsonContent = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            // 发送流式请求
            var request = new HttpRequestMessage(HttpMethod.Post, "chat/completions");
            request.Content = content;

            HttpResponseMessage response;
            try
            {
                response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "[TongyiLlmAgent] ExecuteStreamingAsync failed");
                throw;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            using var reader = new StreamReader(stream);

            string? line;
            while ((line = await reader.ReadLineAsync(ct)) != null && !ct.IsCancellationRequested)
            {
                if (string.IsNullOrWhiteSpace(line) || !line.StartsWith("data: "))
                    continue;

                var jsonLine = line.Substring(6); // Remove "data: " prefix
                if (jsonLine.Trim() == "[DONE]")
                    break;

                TongyiStreamResponse? chunk = null;
                try
                {
                    chunk = JsonSerializer.Deserialize<TongyiStreamResponse>(jsonLine);
                }
                catch (JsonException)
                {
                    // 忽略无法解析的行
                    continue;
                }

                if (chunk?.Choices is { Length: > 0 } choices && choices[0].Delta?.Content is { } deltaContent)
                {
                    yield return deltaContent;
                }
            }
        }

        /// <summary>
        /// 构建消息列表
        /// </summary>
        private static object[] BuildMessages(string prompt, string? systemPrompt)
        {
            var messages = new List<object>();

            if (!string.IsNullOrWhiteSpace(systemPrompt))
            {
                messages.Add(new { role = "system", content = systemPrompt });
            }

            messages.Add(new { role = "user", content = prompt });

            return messages.ToArray();
        }

        #region Response Models

        private class TongyiResponse
        {
            public TongyiChoice[] Choices { get; set; } = Array.Empty<TongyiChoice>();
        }

        private class TongyiChoice
        {
            public TongyiMessage Message { get; set; } = new();
        }

        private class TongyiMessage
        {
            public string Content { get; set; } = string.Empty;
        }

        private class TongyiStreamResponse
        {
            public TongyiStreamChoice[] Choices { get; set; } = Array.Empty<TongyiStreamChoice>();
        }

        private class TongyiStreamChoice
        {
            public TongyiStreamDelta? Delta { get; set; }
        }

        private class TongyiStreamDelta
        {
            public string? Content { get; set; }
        }

        #endregion
    }
}
