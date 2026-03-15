using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Core.Models.LLM;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace CKY.MultiAgentFramework.Core.Agents.Providers
{
    /// <summary>
    /// MiniMax LLM Agent 实现（MiniMax 大语言模型）
    /// </summary>
    /// <remarks>
    /// 模型支持：
    /// - abab6.5s-chat: 旗舰模型
    /// - abab6.5-chat: 通用模型
    /// - abab5.5-chat: 性价比模型
    ///
    /// 配置示例：
    /// <code>
    /// var config = new LlmProviderConfig
    /// {
    ///     ProviderName = "MiniMax",
    ///     ApiKey = "your-api-key",
    ///     GroupId = "your-group-id", // MiniMax 需要 GroupId
    ///     ModelId = "abab6.5s-chat",
    ///     BaseUrl = "https://api.minimax.chat/v1"
    /// };
    /// </code>
    ///
    /// 参考文档：https://platform.minimaxi.com/document/DevelopGuide/105
    /// </remarks>
    public class MiniMaxLlmAgent : MafAiAgent
    {
        private readonly HttpClient _httpClient;
        private readonly string _groupId;

        /// <summary>
        /// 初始化 MiniMaxLlmAgent 类的新实例
        /// </summary>
        public MiniMaxLlmAgent(
            LlmProviderConfig config,
            ILogger logger,
            HttpClient? httpClient = null)
            : base(config, logger)
        {
            _httpClient = httpClient ?? new HttpClient
            {
                BaseAddress = new Uri(config.ApiBaseUrl ?? "https://api.minimax.chat/v1")
            };

            // MiniMax 需要 GroupId
            if (!config.AdditionalParameters.TryGetValue("GroupId", out var groupIdObj))
            {
                throw new ArgumentException("MiniMax requires 'GroupId' in AdditionalParameters", nameof(config));
            }
            _groupId = groupIdObj.ToString() ?? string.Empty;
        }

        /// <inheritdoc />
        public override async Task<string> ExecuteAsync(
            string modelId,
            string prompt,
            string? systemPrompt = null,
            CancellationToken ct = default)
        {
            Logger.LogDebug("[MiniMaxLlmAgent] ExecuteAsync called with model: {Model}", modelId);

            try
            {
                var requestBody = new
                {
                    model = modelId,
                    messages = BuildMessages(prompt, systemPrompt),
                    temperature = 0.7,
                    top_p = 0.9,
                    tokens_to_generate = 2000
                };

                var jsonContent = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var request = new HttpRequestMessage(HttpMethod.Post, $"text/chatcompletion_v2?GroupId={_groupId}");
                request.Content = content;
                request.Headers.Add("Authorization", $"Bearer {GetApiKey()}");

                var response = await _httpClient.SendAsync(request, ct);
                response.EnsureSuccessStatusCode();

                var responseBody = await response.Content.ReadAsStringAsync(ct);
                var result = JsonSerializer.Deserialize<MiniMaxResponse>(responseBody);

                if (result?.Choices?.Length > 0 && result.Choices[0].Text != null)
                {
                    return result.Choices[0].Text;
                }

                throw new InvalidOperationException("Invalid response from MiniMax API");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "[MiniMaxLlmAgent] ExecuteAsync failed");
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
            Logger.LogDebug("[MiniMaxLlmAgent] ExecuteStreamingAsync called with model: {Model}", modelId);

            var requestBody = new
            {
                model = modelId,
                messages = BuildMessages(prompt, systemPrompt),
                temperature = 0.7,
                top_p = 0.9,
                tokens_to_generate = 2000,
                stream = true
            };

            var jsonContent = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(HttpMethod.Post, $"text/chatcompletion_v2?GroupId={_groupId}");
            request.Content = content;
            request.Headers.Add("Authorization", $"Bearer {GetApiKey()}");

            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
            response.EnsureSuccessStatusCode();

            using var stream = await response.Content.ReadAsStreamAsync(ct);
            using var reader = new StreamReader(stream);

            while (!reader.EndOfStream && !ct.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync(ct);
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                MiniMaxStreamResponse? chunk = null;
                try
                {
                    chunk = JsonSerializer.Deserialize<MiniMaxStreamResponse>(line);
                }
                catch (JsonException)
                {
                    continue;
                }

                if (chunk?.Choices?.Length > 0 && chunk.Choices[0].Delta?.Text != null)
                {
                    yield return chunk.Choices[0].Delta.Text;
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

            messages.Add(new { role = "user", content = prompt, sender_type = "USER" });

            return messages.ToArray();
        }

        #region Response Models

        private class MiniMaxResponse
        {
            public MiniMaxChoice[]? Choices { get; set; }
            public string? Created { get; set; }
            public string? Id { get; set; }
        }

        private class MiniMaxChoice
        {
            public string? Text { get; set; }
            public string? Finish_reason { get; set; }
        }

        private class MiniMaxStreamResponse
        {
            public MiniMaxStreamChoice[]? Choices { get; set; }
        }

        private class MiniMaxStreamChoice
        {
            public MiniMaxStreamDelta? Delta { get; set; }
            public string? Finish_reason { get; set; }
        }

        private class MiniMaxStreamDelta
        {
            public string? Text { get; set; }
        }

        #endregion
    }
}
