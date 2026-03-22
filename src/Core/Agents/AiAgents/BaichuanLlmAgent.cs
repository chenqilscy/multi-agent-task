using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Core.Models.LLM;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace CKY.MultiAgentFramework.Core.Agents.Providers
{
    /// <summary>
    /// 百川 LLM Agent 实现（百川智能大语言模型）
    /// </summary>
    /// <remarks>
    /// 模型支持：
    /// - Baichuan2-Turbo: 通用模型
    /// - Baichuan2-53B: 大参数模型
    /// - Baichuan-Text-Embedding: 文本嵌入模型
    ///
    /// 配置示例：
    /// <code>
    /// var config = new LlmProviderConfig
    /// {
    ///     ProviderName = "Baichuan",
    ///     ApiKey = "your-api-key",
    ///     SecretKey = "your-secret-key",
    ///     ModelId = "Baichuan2-Turbo",
    ///     BaseUrl = "https://api.baichuan-ai.com/v1/chat"
    /// };
    /// </code>
    ///
    /// 参考文档：https://platform.baichuan-ai.com/docs
    /// </remarks>
    public class BaichuanLlmAgent : MafAiAgent
    {
        private readonly HttpClient _httpClient;
        private readonly string _secretKey;

        /// <summary>
        /// 初始化 BaichuanLlmAgent 类的新实例
        /// </summary>
        public BaichuanLlmAgent(
            LlmProviderConfig config,
            ILogger logger,
            HttpClient? httpClient = null)
            : base(config, logger)
        {
            _httpClient = httpClient ?? new HttpClient
            {
                BaseAddress = new Uri(config.ApiBaseUrl ?? "https://api.baichuan-ai.com/v1")
            };

            // 百川需要 SecretKey
            if (!config.AdditionalParameters.TryGetValue("SecretKey", out var secretKeyObj))
            {
                throw new ArgumentException("Baichuan requires 'SecretKey' in AdditionalParameters", nameof(config));
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
            Logger.LogDebug("[BaichuanLlmAgent] ExecuteAsync called with model: {Model}", modelId);

            try
            {
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

                var request = new HttpRequestMessage(HttpMethod.Post, "chat/completions");
                request.Content = content;
                request.Headers.Add("Authorization", $"Bearer {GetApiKey()}");

                var response = await _httpClient.SendAsync(request, ct);
                response.EnsureSuccessStatusCode();

                var responseBody = await response.Content.ReadAsStringAsync(ct);
                var result = JsonSerializer.Deserialize<BaichuanResponse>(responseBody);

                if (result?.Choices?.Length > 0)
                {
                    return result.Choices[0].Message.Content;
                }

                throw new InvalidOperationException("Invalid response from Baichuan API");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "[BaichuanLlmAgent] ExecuteAsync failed");
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
            Logger.LogDebug("[BaichuanLlmAgent] ExecuteStreamingAsync called with model: {Model}", modelId);

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

            var request = new HttpRequestMessage(HttpMethod.Post, "chat/completions");
            request.Content = content;
            request.Headers.Add("Authorization", $"Bearer {GetApiKey()}");

            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
            response.EnsureSuccessStatusCode();

            using var stream = await response.Content.ReadAsStreamAsync(ct);
            using var reader = new StreamReader(stream);

            string? line;
            while ((line = await reader.ReadLineAsync(ct)) != null && !ct.IsCancellationRequested)
            {
                if (string.IsNullOrWhiteSpace(line) || !line.StartsWith("data: "))
                    continue;

                var jsonLine = line.Substring(6);
                if (jsonLine.Trim() == "[DONE]")
                    break;

                BaichuanStreamResponse? chunk = null;
                try
                {
                    chunk = JsonSerializer.Deserialize<BaichuanStreamResponse>(jsonLine);
                }
                catch (JsonException)
                {
                    continue;
                }

                if (chunk?.Choices is { Length: > 0 } choices && choices[0].Delta?.Content is { } deltaContent)
                {
                    yield return deltaContent;
                }
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

        #region Response Models

        private class BaichuanResponse
        {
            public BaichuanChoice[]? Choices { get; set; }
            public BaichuanUsage? Usage { get; set; }
        }

        private class BaichuanChoice
        {
            public BaichuanMessage Message { get; set; } = new();
            public string? Finish_reason { get; set; }
        }

        private class BaichuanMessage
        {
            public string Content { get; set; } = string.Empty;
        }

        private class BaichuanUsage
        {
            public int Prompt_tokens { get; set; }
            public int Completion_tokens { get; set; }
            public int Total_tokens { get; set; }
        }

        private class BaichuanStreamResponse
        {
            public BaichuanStreamChoice[]? Choices { get; set; }
        }

        private class BaichuanStreamChoice
        {
            public BaichuanStreamDelta? Delta { get; set; }
        }

        private class BaichuanStreamDelta
        {
            public string? Content { get; set; }
        }

        #endregion
    }
}
