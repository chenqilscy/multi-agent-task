using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Core.Models.LLM;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace CKY.MultiAgentFramework.Infrastructure.Context.Compression
{
    /// <summary>
    /// 基于 LLM 的压缩服务实现
    /// </summary>
    public class LLMCompressionService : ILLMCompressionService
    {
        private readonly IMafAiAgentRegistry _llmRegistry;
        private readonly ILogger<LLMCompressionService> _logger;

        public LLMCompressionService(
            IMafAiAgentRegistry llmRegistry,
            ILogger<LLMCompressionService> logger)
        {
            _llmRegistry = llmRegistry ?? throw new ArgumentNullException(nameof(llmRegistry));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<string> SummarizeAsync(List<ChatMessage> messages, CancellationToken ct)
        {
            try
            {
                // 构建总结提示词
                var prompt = BuildSummaryPrompt(messages);

                // 调用 LLM
                var agent = await _llmRegistry.GetBestAgentAsync(LlmScenario.Chat, ct);
                var summary = await agent.ExecuteAsync(
                    agent.GetCurrentModelId(),
                    prompt,
                    "你是一个专业的对话总结助手。请简洁地总结对话历史。",
                    ct);

                return summary;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[LLMCompression] LLM 总结失败");
                throw;
            }
        }

        private string BuildSummaryPrompt(List<ChatMessage> messages)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("请总结以下对话历史，提取关键信息：");
            sb.AppendLine();

            foreach (var msg in messages)
            {
                var role = msg.Role == ChatRole.User ? "用户" : "助手";
                sb.AppendLine($"{role}: {msg.Text}");
            }

            sb.AppendLine();
            sb.AppendLine("请用简洁的语言总结上述对话的主要内容和结论。");

            return sb.ToString();
        }
    }
}
