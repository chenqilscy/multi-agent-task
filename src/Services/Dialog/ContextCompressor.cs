using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Core.Agents;
using CKY.MultiAgentFramework.Core.Models.Dialog;
using CKY.MultiAgentFramework.Core.Models.LLM;
using CKY.MultiAgentFramework.Core.Models.Message;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace CKY.MultiAgentFramework.Services.Dialog
{
    /// <summary>
    /// 上下文压缩器实现
    /// Context compressor implementation
    /// </summary>
    public class ContextCompressor : IContextCompressor
    {
        private readonly IMafAiAgentRegistry _llmRegistry;
        private readonly ILogger<ContextCompressor> _logger;

        public ContextCompressor(
            IMafAiAgentRegistry llmRegistry,
            ILogger<ContextCompressor> logger)
        {
            _llmRegistry = llmRegistry ?? throw new ArgumentNullException(nameof(llmRegistry));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 压缩并存储对话历史
        /// Compress and store dialog history
        /// </summary>
        public async Task<ContextCompressionResult> CompressAndStoreAsync(
            DialogContext context,
            CancellationToken ct = default)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            _logger.LogInformation("Starting context compression for session: {SessionId}", context.SessionId);

            // TODO: Get messages from session store (will be implemented when session store is integrated)
            var messages = new List<MessageContext>();

            if (messages.Count == 0)
            {
                _logger.LogWarning("No messages to compress for session: {SessionId}", context.SessionId);
                return new ContextCompressionResult
                {
                    OriginalMessageCount = 0,
                    CompressedMessageCount = 0,
                    CompressionRatio = 1.0
                };
            }

            // Generate summary and extract key information in parallel
            var summaryTask = GenerateSummaryAsync(messages, ct);
            var keyInfosTask = ExtractKeyInformationAsync(messages, ct);

            await Task.WhenAll(summaryTask, keyInfosTask);

            var summary = await summaryTask;
            var keyInfos = await keyInfosTask;

            var result = new ContextCompressionResult
            {
                Summary = summary,
                KeyInfos = keyInfos,
                OriginalMessageCount = messages.Count,
                CompressedMessageCount = 1, // Summary + key infos as single message
                CompressionRatio = messages.Count > 0 ? (double)1 / messages.Count : 1.0
            };

            _logger.LogInformation(
                "Context compression completed for session: {SessionId}. " +
                "Original messages: {OriginalCount}, " +
                "Compression ratio: {CompressionRatio:P2}",
                context.SessionId,
                result.OriginalMessageCount,
                result.CompressionRatio);

            // TODO: Store compressed data to L2/L3 (will be implemented when session store is integrated)

            return result;
        }

        /// <summary>
        /// 生成对话摘要（使用LLM）
        /// Generate dialog summary using LLM
        /// </summary>
        public async Task<string> GenerateSummaryAsync(
            List<MessageContext> messages,
            CancellationToken ct = default)
        {
            if (messages == null || messages.Count == 0)
                return string.Empty;

            _logger.LogDebug("Generating summary for {MessageCount} messages", messages.Count);

            // Build conversation text for LLM
            var conversationText = BuildConversationText(messages);

            // Sanitize input to prevent prompt injection
            var sanitizedConversation = SanitizeInput(conversationText);

            var prompt = $@"
请为以下对话生成简洁的摘要（不超过100字）：

对话内容：
{sanitizedConversation}

要求：
1. 总结对话的主要内容和目的
2. 提取关键信息（如时间、地点、人物、事件）
3. 保持简洁明了
4. 返回纯文本，不包含JSON格式或其他标记

摘要：";

            try
            {
                var llmAgent = await _llmRegistry.GetBestAgentAsync(LlmScenario.Intent, ct);
                var modelId = llmAgent.GetCurrentModelId();
                var summary = await llmAgent.ExecuteAsync(modelId, prompt, null, ct);

                // Sanitize and trim the summary
                var sanitizedSummary = SanitizeInput(summary).Trim();

                _logger.LogDebug("Summary generated successfully. Length: {Length}", sanitizedSummary.Length);

                return sanitizedSummary;
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Failed to get LLM agent for summary generation, using fallback");
                return GenerateFallbackSummary(messages);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating summary, using fallback");
                return GenerateFallbackSummary(messages);
            }
        }

        /// <summary>
        /// 提取关键信息（使用LLM）
        /// Extract key information using LLM
        /// </summary>
        public async Task<List<KeyInformation>> ExtractKeyInformationAsync(
            List<MessageContext> messages,
            CancellationToken ct = default)
        {
            if (messages == null || messages.Count == 0)
                return new List<KeyInformation>();

            _logger.LogDebug("Extracting key information from {MessageCount} messages", messages.Count);

            // Build conversation text for LLM
            var conversationText = BuildConversationText(messages);
            var sanitizedConversation = SanitizeInput(conversationText);

            var prompt = $@"
从以下对话中提取关键信息，按类型分类：

对话内容：
{sanitizedConversation}

请提取并返回JSON格式的关键信息：
{{
  ""key_info"": [
    {{
      ""type"": ""Preference"",
      ""content"": ""用户喜欢温度设定为26度"",
      ""importance"": 0.8,
      ""tags"": [""温度"", ""偏好""]
    }},
    {{
      ""type"": ""Decision"",
      ""content"": ""用户决定明天上午10点开会"",
      ""importance"": 0.9,
      ""tags"": [""时间"", ""会议""]
    }},
    {{
      ""type"": ""Fact"",
      ""content"": ""用户住在海淀区"",
      ""importance"": 0.7,
      ""tags"": [""地址""]
    }}
  ]
}}

类型说明：
- Preference: 用户偏好、习惯、喜好
- Decision: 用户做出的决定、选择
- Fact: 事实信息、数据

只返回JSON，不要包含其他文字。";

            try
            {
                var llmAgent = await _llmRegistry.GetBestAgentAsync(LlmScenario.Intent, ct);
                var modelId = llmAgent.GetCurrentModelId();
                var response = await llmAgent.ExecuteAsync(modelId, prompt, null, ct);

                return ParseKeyInformationFromLlmResponse(response);
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "JSON parsing failed during key information extraction, using fallback");
                return GenerateFallbackKeyInformation(messages);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Failed to get LLM agent for key information extraction, using fallback");
                return GenerateFallbackKeyInformation(messages);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting key information, using fallback");
                return GenerateFallbackKeyInformation(messages);
            }
        }

        #region Private Helper Methods

        /// <summary>
        /// Build conversation text from message list
        /// </summary>
        private string BuildConversationText(List<MessageContext> messages)
        {
            var lines = new List<string>();

            foreach (var msg in messages.OrderByDescending(m => m.Timestamp))
            {
                var role = msg.Role ?? "Unknown";
                var content = GetContentAsString(msg.Content);
                var time = msg.Timestamp.ToString("HH:mm:ss");

                lines.Add($"[{time}] {role}: {content}");
            }

            return string.Join("\n", lines);
        }

        /// <summary>
        /// Get content as string from object
        /// </summary>
        private string GetContentAsString(object? content)
        {
            return content?.ToString() ?? string.Empty;
        }

        /// <summary>
        /// Sanitize input to prevent prompt injection
        /// </summary>
        private string SanitizeInput(string input)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            // Remove potential prompt injection patterns
            var sanitized = input
                .Replace("\r\n", "\n")
                .Replace("\r", "\n")
                .Replace("\t", " ");

            // Limit length to prevent token overflow
            const int maxLength = 2000;
            if (sanitized.Length > maxLength)
            {
                sanitized = sanitized.Substring(0, maxLength) + "...";
            }

            return sanitized;
        }

        /// <summary>
        /// Parse key information from LLM response
        /// </summary>
        private List<KeyInformation> ParseKeyInformationFromLlmResponse(string response)
        {
            var keyInfos = new List<KeyInformation>();

            // Try to parse JSON array
            using var document = JsonDocument.Parse(response);
            var root = document.RootElement;

            if (root.TryGetProperty("key_info", out var keyInfoArray))
            {
                foreach (var item in keyInfoArray.EnumerateArray())
                {
                    var keyInfo = new KeyInformation
                    {
                        Type = item.TryGetProperty("type", out var type) ? type.GetString() ?? string.Empty : string.Empty,
                        Content = item.TryGetProperty("content", out var content) ? content.GetString() ?? string.Empty : string.Empty,
                        Importance = item.TryGetProperty("importance", out var importance) ? importance.GetDouble() : 0.5,
                        Tags = new List<string>()
                    };

                    if (item.TryGetProperty("tags", out var tags))
                    {
                        foreach (var tag in tags.EnumerateArray())
                        {
                            keyInfo.Tags.Add(tag.GetString() ?? string.Empty);
                        }
                    }

                    keyInfos.Add(keyInfo);
                }
            }

            _logger.LogDebug("Parsed {Count} key information items from LLM response", keyInfos.Count);
            return keyInfos;
        }

        /// <summary>
        /// Generate fallback summary when LLM is unavailable
        /// </summary>
        private string GenerateFallbackSummary(List<MessageContext> messages)
        {
            if (messages.Count == 0)
                return "无对话内容";

            var userMessages = messages.Where(m => m.Role == "User").ToList();
            var assistantMessages = messages.Where(m => m.Role == "Assistant").ToList();

            return $"对话包含{userMessages.Count}条用户消息和{assistantMessages.Count}条助手回复，" +
                   $"时间跨度从{messages.Min(m => m.Timestamp):yyyy-MM-dd HH:mm}到{messages.Max(m => m.Timestamp):yyyy-MM-dd HH:mm}。";
        }

        /// <summary>
        /// Generate fallback key information when LLM is unavailable
        /// </summary>
        private List<KeyInformation> GenerateFallbackKeyInformation(List<MessageContext> messages)
        {
            var keyInfos = new List<KeyInformation>();

            // Extract basic facts from messages
            foreach (var msg in messages.Take(5)) // Limit to first 5 messages
            {
                var content = GetContentAsString(msg.Content);
                if (content.Length > 0) // Changed from > 10 to > 0 to handle short messages
                {
                    keyInfos.Add(new KeyInformation
                    {
                        Type = "Fact",
                        Content = content.Length > 50 ? content.Substring(0, 50) + "..." : content,
                        Importance = 0.5,
                        Tags = new List<string> { msg.Role ?? "Unknown" }
                    });
                }
            }

            return keyInfos;
        }

        #endregion
    }
}
