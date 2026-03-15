using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Core.Models.LLM;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace CKY.MultiAgentFramework.Core.Agents.Providers
{
    /// <summary>
    /// 文心一言 LLM Agent 实现（百度千帆大语言模型）
    /// </summary>
    /// <remarks>
    /// 模型支持：
    /// - ERNIE-Bot 4.0: 旗舰模型
    /// - ERNIE-Bot 3.5: 通用模型
    /// - ERNIE-Bot-turbo: 快速模型
    /// - ERNIE-Bot-long: 长文本模型
    ///
    /// 配置示例：
    /// <code>
    /// var config = new LlmProviderConfig
    /// {
    ///     ProviderName = "Wenxin",
    ///     ApiKey = "your-api-key",
    ///     SecretKey = "your-secret-key", // 文心需要额外的 SecretKey
    ///     ModelId = "ERNIE-Bot-4.0",
    ///     BaseUrl = "https://aip.baidubce.com/rpc/2.0/ai_custom/v1/wenxinworkshop/chat"
    /// };
    /// </code>
    ///
    /// 参考文档：https://cloud.baidu.com/doc/WENXINWORKSHOP/s/Nlks5zkzu
    /// </remarks>
    public class WenxinLlmAgent : MafAiAgent
    {
        private readonly HttpClient _httpClient;
        private readonly string _secretKey;

        /// <summary>
        /// 初始化 WenxinLlmAgent 类的新实例
        /// </summary>
        public WenxinLlmAgent(
            LlmProviderConfig config,
            ILogger logger,
            HttpClient? httpClient = null)
            : base(config, logger)
        {
            _httpClient = httpClient ?? new HttpClient
            {
                BaseAddress = new Uri(config.ApiBaseUrl ?? "https://aip.baidubce.com/rpc/2.0/ai_custom/v1/wenxinworkshop/chat")
            };

            // 文心一言需要从配置的 AdditionalParameters 中获取 SecretKey
            if (!config.AdditionalParameters.TryGetValue("SecretKey", out var secretKeyObj))
            {
                throw new ArgumentException("Wenxin requires 'SecretKey' in AdditionalParameters", nameof(config));
            }
            _secretKey = secretKeyObj.ToString() ?? string.Empty;
        }

        /// <inheritdoc />
        public override async Task<string> ExecuteAsync(
            string modelId,
            string prompt,
            string? systemPrompt = null,
            CancellationToken ct = default)
        {
            Logger.LogDebug("[WenxinLlmAgent] ExecuteAsync called with model: {Model}", modelId);

            try
            {
                // 生成认证令牌
                var authToken = GenerateAuthToken();

                var requestBody = new
                {
                    messages = BuildMessages(prompt, systemPrompt),
                    temperature = 0.7,
                    top_p = 0.9,
                    penalty_score = 1.0
                };

                var jsonContent = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var request = new HttpRequestMessage(HttpMethod.Post, $"completions?access_token={authToken}");
                request.Content = content;

                var response = await _httpClient.SendAsync(request, ct);
                response.EnsureSuccessStatusCode();

                var responseBody = await response.Content.ReadAsStringAsync(ct);
                var result = JsonSerializer.Deserialize<WenxinResponse>(responseBody);

                if (result?.Result != null)
                {
                    return result.Result;
                }

                throw new InvalidOperationException("Invalid response from Wenxin API");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "[WenxinLlmAgent] ExecuteAsync failed");
                throw;
            }
        }

        /// <inheritdoc />
        public override async IAsyncEnumerable<string> ExecuteStreamingAsync(
            string modelId,
            string prompt,
            string? systemPrompt = null,
            CancellationToken ct = default)
        {
            Logger.LogDebug("[WenxinLlmAgent] ExecuteStreamingAsync called with model: {Model}", modelId);

            // 文心一言的流式实现
            var authToken = GenerateAuthToken();

            var requestBody = new
            {
                messages = BuildMessages(prompt, systemPrompt),
                temperature = 0.7,
                top_p = 0.9,
                stream = true
            };

            var jsonContent = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(HttpMethod.Post, $"completions_pro?access_token={authToken}");
            request.Content = content;

            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
            response.EnsureSuccessStatusCode();

            using var stream = await response.Content.ReadAsStreamAsync(ct);
            using var reader = new StreamReader(stream);

            await foreach (var line in ReadStreamLinesAsync(reader, ct))
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                WenxinStreamResponse? chunk = null;
                try
                {
                    chunk = JsonSerializer.Deserialize<WenxinStreamResponse>(line);
                }
                catch (JsonException)
                {
                    continue;
                }

                if (chunk?.Result != null)
                {
                    yield return chunk.Result;
                }

                if (chunk?.Is_end ?? false)
                    break;
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

        private string GenerateAuthToken()
        {
            // 文心一言使用 API Key + Secret Key 生成 JWT Token
            // 这里简化处理，实际应该实现完整的 JWT 签名
            // 实际项目中应使用百度 SDK 或实现完整的 JWT 签名
            return GetApiKey(); // 简化版本，生产环境需要完整实现
        }

        private static async IAsyncEnumerable<string> ReadStreamLinesAsync(StreamReader reader, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
        {
            while (!reader.EndOfStream && !ct.IsCancellationRequested)
            {
                yield return await reader.ReadLineAsync(ct) ?? string.Empty;
            }
        }

        #region Response Models

        private class WenxinResponse
        {
            public string? Result { get; set; }
            public bool Is_end { get; set; }
            public WenxinUsage? Usage { get; set; }
        }

        private class WenxinStreamResponse
        {
            public string? Result { get; set; }
            public bool Is_end { get; set; }
        }

        private class WenxinUsage
        {
            public int Prompt_tokens { get; set; }
            public int Completion_tokens { get; set; }
            public int Total_tokens { get; set; }
        }

        #endregion
    }
}
