using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Core.Models.LLM;
using CKY.MultiAgentFramework.Core.Resilience;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CKY.MultiAgentFramework.Core.Agents.Providers
{
    /// <summary>
    /// OpenAI LLM Agent 实现
    /// </summary>
    /// <remarks>
    /// 模型支持：
    /// - gpt-4o: 旗舰多模态模型
    /// - gpt-4o-mini: 性价比模型
    /// - gpt-4-turbo: 高性能模型
    /// - gpt-3.5-turbo: 快速模型
    /// - o1 / o1-mini: 推理模型
    ///
    /// API 基础 URL：https://api.openai.com/v1
    ///
    /// 配置示例：
    /// <code>
    /// var config = new LlmProviderConfig
    /// {
    ///     ProviderName = "openai",
    ///     ApiKey = "sk-...",
    ///     ModelId = "gpt-4o",
    ///     ApiBaseUrl = "https://api.openai.com/v1",
    ///     SupportedScenarios = new() { LlmScenario.Chat, LlmScenario.Code }
    /// };
    /// </code>
    /// </remarks>
    public class OpenAiLlmAgent : MafAiAgent
    {
        private readonly HttpClient _httpClient;
        private readonly ILlmResiliencePipeline? _resiliencePipeline;

        public OpenAiLlmAgent(
            LlmProviderConfig config,
            ILogger logger,
            HttpClient? httpClient = null,
            ILlmResiliencePipeline? resiliencePipeline = null)
            : base(config, logger)
        {
            _httpClient = httpClient ?? new HttpClient
            {
                BaseAddress = new Uri(config.ApiBaseUrl ?? "https://api.openai.com/v1/")
            };

            // 设置认证头（仅当 httpClient 外部未配置时）
            if (httpClient == null)
            {
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {GetApiKey()}");
            }

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
            Logger.LogDebug("[OpenAI] ExecuteAsync called with model: {Model}", modelId);

            var requestBody = new
            {
                model = modelId,
                messages = BuildMessages(prompt, systemPrompt),
                temperature = Config.Temperature,
                max_tokens = Config.MaxTokens
            };

            var jsonContent = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("chat/completions", content, ct);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync(ct);
            var result = JsonSerializer.Deserialize<OpenAiChatResponse>(responseBody);

            if (result?.Choices is { Length: > 0 })
            {
                return result.Choices[0].Message.Content;
            }

            throw new InvalidOperationException("Invalid response from OpenAI API");
        }

        /// <inheritdoc />
        public override async IAsyncEnumerable<string> ExecuteStreamingAsync(
            string modelId,
            string prompt,
            string? systemPrompt = null,
            [EnumeratorCancellation] CancellationToken ct = default)
        {
            Logger.LogDebug("[OpenAI] ExecuteStreamingAsync called with model: {Model}", modelId);

            var requestBody = new
            {
                model = modelId,
                messages = BuildMessages(prompt, systemPrompt),
                temperature = Config.Temperature,
                max_tokens = Config.MaxTokens,
                stream = true
            };

            var jsonContent = JsonSerializer.Serialize(requestBody);
            var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(HttpMethod.Post, "chat/completions") { Content = httpContent };
            var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            using var reader = new StreamReader(stream);

            while (await reader.ReadLineAsync(ct) is { } line)
            {
                if (ct.IsCancellationRequested) break;
                if (string.IsNullOrWhiteSpace(line) || !line.StartsWith("data: ")) continue;

                var data = line["data: ".Length..];
                if (data.Trim() == "[DONE]") break;

                OpenAiStreamChunk? chunk;
                try { chunk = JsonSerializer.Deserialize<OpenAiStreamChunk>(data); }
                catch (JsonException) { continue; }

                var delta = chunk?.Choices?.FirstOrDefault()?.Delta?.Content;
                if (!string.IsNullOrEmpty(delta))
                    yield return delta;
            }
        }

        private static object[] BuildMessages(string prompt, string? systemPrompt)
        {
            var messages = new List<object>();
            if (!string.IsNullOrWhiteSpace(systemPrompt))
                messages.Add(new { role = "system", content = systemPrompt });
            messages.Add(new { role = "user", content = prompt });
            return messages.ToArray();
        }

        #region Response Models

        private sealed class OpenAiChatResponse
        {
            [JsonPropertyName("choices")]
            public OpenAiChoice[]? Choices { get; set; }
        }

        private sealed class OpenAiChoice
        {
            [JsonPropertyName("message")]
            public OpenAiMessage Message { get; set; } = new();
        }

        private sealed class OpenAiMessage
        {
            [JsonPropertyName("content")]
            public string Content { get; set; } = string.Empty;
        }

        private sealed class OpenAiStreamChunk
        {
            [JsonPropertyName("choices")]
            public OpenAiStreamChoice[]? Choices { get; set; }
        }

        private sealed class OpenAiStreamChoice
        {
            [JsonPropertyName("delta")]
            public OpenAiDelta? Delta { get; set; }
        }

        private sealed class OpenAiDelta
        {
            [JsonPropertyName("content")]
            public string? Content { get; set; }
        }

        #endregion
    }
}
