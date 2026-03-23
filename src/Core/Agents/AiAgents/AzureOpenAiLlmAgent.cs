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
    /// Azure OpenAI LLM Agent 实现
    /// </summary>
    /// <remarks>
    /// Azure OpenAI 与标准 OpenAI API 的区别：
    /// - URL 格式：https://{resource}.openai.azure.com/openai/deployments/{deployment}/chat/completions?api-version={version}
    /// - 认证：使用 api-key 头而非 Bearer token（也支持 Azure AD token）
    /// - 模型名通过部署名映射（不在 body 中传递 model 字段）
    ///
    /// 配置示例：
    /// <code>
    /// var config = new LlmProviderConfig
    /// {
    ///     ProviderName = "azure-openai",
    ///     ApiKey = "your-azure-api-key",
    ///     ModelId = "my-gpt4o-deployment",  // Azure 部署名
    ///     ApiBaseUrl = "https://my-resource.openai.azure.com",
    ///     AdditionalParameters = new()
    ///     {
    ///         ["ApiVersion"] = "2024-06-01"
    ///     },
    ///     SupportedScenarios = new() { LlmScenario.Chat, LlmScenario.Code }
    /// };
    /// </code>
    /// </remarks>
    public class AzureOpenAiLlmAgent : MafAiAgent
    {
        private readonly HttpClient _httpClient;
        private readonly ILlmResiliencePipeline? _resiliencePipeline;
        private readonly string _apiVersion;

        private const string DefaultApiVersion = "2024-06-01";

        public AzureOpenAiLlmAgent(
            LlmProviderConfig config,
            ILogger logger,
            HttpClient? httpClient = null,
            ILlmResiliencePipeline? resiliencePipeline = null)
            : base(config, logger)
        {
            _httpClient = httpClient ?? new HttpClient();

            // 设置认证头（仅当 httpClient 外部未配置时）
            if (httpClient == null)
            {
                _httpClient.BaseAddress = new Uri(EnsureTrailingSlash(config.ApiBaseUrl ?? ""));
                _httpClient.DefaultRequestHeaders.Add("api-key", GetApiKey());
            }

            _resiliencePipeline = resiliencePipeline;

            _apiVersion = config.AdditionalParameters.TryGetValue("ApiVersion", out var ver)
                ? ver.ToString() ?? DefaultApiVersion
                : DefaultApiVersion;
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
            Logger.LogDebug("[AzureOpenAI] ExecuteAsync called with deployment: {Deployment}", modelId);

            var requestBody = new
            {
                messages = BuildMessages(prompt, systemPrompt),
                temperature = Config.Temperature,
                max_tokens = Config.MaxTokens
            };

            var jsonContent = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var url = $"openai/deployments/{Uri.EscapeDataString(modelId)}/chat/completions?api-version={Uri.EscapeDataString(_apiVersion)}";
            var response = await _httpClient.PostAsync(url, content, ct);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync(ct);
            var result = JsonSerializer.Deserialize<AzureChatResponse>(responseBody);

            if (result?.Choices is { Length: > 0 })
            {
                return result.Choices[0].Message.Content;
            }

            throw new InvalidOperationException("Invalid response from Azure OpenAI API");
        }

        /// <inheritdoc />
        public override async IAsyncEnumerable<string> ExecuteStreamingAsync(
            string modelId,
            string prompt,
            string? systemPrompt = null,
            [EnumeratorCancellation] CancellationToken ct = default)
        {
            Logger.LogDebug("[AzureOpenAI] ExecuteStreamingAsync called with deployment: {Deployment}", modelId);

            var requestBody = new
            {
                messages = BuildMessages(prompt, systemPrompt),
                temperature = Config.Temperature,
                max_tokens = Config.MaxTokens,
                stream = true
            };

            var jsonContent = JsonSerializer.Serialize(requestBody);
            var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var url = $"openai/deployments/{Uri.EscapeDataString(modelId)}/chat/completions?api-version={Uri.EscapeDataString(_apiVersion)}";
            var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = httpContent };
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

                AzureStreamChunk? chunk;
                try { chunk = JsonSerializer.Deserialize<AzureStreamChunk>(data); }
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

        private static string EnsureTrailingSlash(string url)
        {
            return url.EndsWith('/') ? url : url + "/";
        }

        #region Response Models

        private sealed class AzureChatResponse
        {
            [JsonPropertyName("choices")]
            public AzureChoice[]? Choices { get; set; }
        }

        private sealed class AzureChoice
        {
            [JsonPropertyName("message")]
            public AzureMessage Message { get; set; } = new();
        }

        private sealed class AzureMessage
        {
            [JsonPropertyName("content")]
            public string Content { get; set; } = string.Empty;
        }

        private sealed class AzureStreamChunk
        {
            [JsonPropertyName("choices")]
            public AzureStreamChoice[]? Choices { get; set; }
        }

        private sealed class AzureStreamChoice
        {
            [JsonPropertyName("delta")]
            public AzureDelta? Delta { get; set; }
        }

        private sealed class AzureDelta
        {
            [JsonPropertyName("content")]
            public string? Content { get; set; }
        }

        #endregion
    }
}
