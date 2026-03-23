using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Core.Models.LLM;
using CKY.MultiAgentFramework.Core.Resilience;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace CKY.MultiAgentFramework.Core.Agents.Providers
{
    /// <summary>
    /// 讯飞星火 LLM Agent 实现（科大讯飞大语言模型）
    /// </summary>
    /// <remarks>
    /// 模型支持：
    /// - spark-4.0-ultra: 旗舰模型
    /// - spark-4.0: 通用模型
    /// - spark-pro: 专业版
    /// - spark-lite: 轻量版
    ///
    /// 配置示例：
    /// <code>
    /// var config = new LlmProviderConfig
    /// {
    ///     ProviderName = "Xunfei",
    ///     ApiKey = "your-app-id:your-api-key:your-api-secret",
    ///     ModelId = "spark-4.0",
    ///     BaseUrl = "https://spark-api.xf-yun.com/v4.0/chat"
    /// };
    /// </code>
    ///
    /// 参考文档：https://www.xfyun.cn/doc/spark/Web.html
    /// </remarks>
    public class XunfeiLlmAgent : MafAiAgent
    {
        private readonly HttpClient _httpClient;
        private readonly string _appId;
        private readonly string _apiKey;
        private readonly string _apiSecret;
        private readonly ILlmResiliencePipeline? _resiliencePipeline;

        /// <summary>
        /// 初始化 XunfeiLlmAgent 类的新实例
        /// </summary>
        public XunfeiLlmAgent(
            LlmProviderConfig config,
            ILogger logger,
            HttpClient? httpClient = null,
            ILlmResiliencePipeline? resiliencePipeline = null)
            : base(config, logger)
        {
            _httpClient = httpClient ?? new HttpClient();

            // 讯飞的 API Key 格式为：appId:apiKey:apiSecret
            var keyParts = GetApiKey().Split(':');
            if (keyParts.Length != 3)
            {
                throw new ArgumentException("Xunfei API Key must be in format: appId:apiKey:apiSecret", nameof(config));
            }

            _appId = keyParts[0];
            _apiKey = keyParts[1];
            _apiSecret = keyParts[2];
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
            Logger.LogDebug("[XunfeiLlmAgent] ExecuteAsync called with model: {Model}", modelId);

            try
            {
                // 讯飞星火使用 WebSocket 进行 API 调用
                // 这里简化为 HTTP 调用，生产环境应使用 WebSocket
                var url = GetApiUrl(modelId);

                var requestBody = new
                {
                    header = new
                    {
                        app_id = _appId,
                        status = 2 // 2表示最后一个请求
                    },
                    parameter = new
                    {
                        chat = new
                        {
                            domain = modelId,
                            temperature = 0.7,
                            max_tokens = 2000
                        }
                    },
                    payload = new
                    {
                        message = new
                        {
                            text = BuildMessages(prompt, systemPrompt)
                        }
                    }
                };

                var authUrl = GenerateAuthUrl(url);

                var jsonContent = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(authUrl, content, ct);
                response.EnsureSuccessStatusCode();

                var responseBody = await response.Content.ReadAsStringAsync(ct);
                var result = JsonSerializer.Deserialize<XunfeiResponse>(responseBody);

                if (result?.Header?.Code == 0 && result.Payload?.Choices?.Text != null)
                {
                    return result.Payload.Choices.Text;
                }

                throw new InvalidOperationException($"Xunfei API error: {result?.Header?.Message}");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "[XunfeiLlmAgent] ExecuteAsync failed");
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
            Logger.LogDebug("[XunfeiLlmAgent] ExecuteStreamingAsync called with model: {Model}", modelId);

            // 讯飞星火的流式实现使用 WebSocket
            // 这里简化为非流式实现
            var result = await ExecuteAsync(modelId, prompt, systemPrompt, ct);

            // 模拟流式输出，每次返回一部分
            var chunkSize = 50;
            for (int i = 0; i < result.Length; i += chunkSize)
            {
                var size = Math.Min(chunkSize, result.Length - i);
                yield return result.Substring(i, size);
                await Task.Delay(20, ct); // 模拟流式延迟
            }
        }

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

        private string GetApiUrl(string modelId)
        {
            // 根据模型 ID 选择不同的 API 端点
            return modelId switch
            {
                var m when m.Contains("4.0") => "https://spark-api.xf-yun.com/v4.0/chat",
                var m when m.Contains("pro") => "https://spark-api.xf-yun.com/v3.5/chat",
                _ => "https://spark-api.xf-yun.com/v3.1/chat"
            };
        }

        private string GenerateAuthUrl(string url)
        {
            // 讯飞星火需要生成带有签名的 URL
            // 简化版本，生产环境需要完整实现
            var host = new Uri(url).Host;
            var path = new Uri(url).PathAndQuery;

            // 生成时间戳和签名
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var signature = $"host: {host}\ndate: {timestamp}\nGET {path} HTTP/1.1";

            // 这里应该使用 HMAC-SHA256 签名，简化为返回原 URL
            return url;
        }

        #region Response Models

        private class XunfeiResponse
        {
            public XunfeiHeader? Header { get; set; }
            public XunfeiPayload? Payload { get; set; }
        }

        private class XunfeiHeader
        {
            public int Code { get; set; }
            public string? Message { get; set; }
            public string? Sid { get; set; }
        }

        private class XunfeiPayload
        {
            public XunfeiChoices? Choices { get; set; }
        }

        private class XunfeiChoices
        {
            public string? Text { get; set; }
            public int Status { get; set; }
        }

        #endregion
    }
}
