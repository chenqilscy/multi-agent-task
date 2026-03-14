using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Core.Agents;
using Microsoft.Agents.AI;
using Microsoft.Extensions.Logging;

namespace CKY.MultiAgentFramework.Services.NLP
{
    /// <summary>
    /// LLM 服务适配器
    /// 将 LlmAgent 适配到 ILlmService 接口
    /// </summary>
    public class LlmServiceAdapter : ILlmService
    {
        private readonly LlmAgent _llmAgent;
        private readonly ILogger<LlmServiceAdapter> _logger;

        public LlmServiceAdapter(
            LlmAgent llmAgent,
            ILogger<LlmServiceAdapter> logger)
        {
            _llmAgent = llmAgent ?? throw new ArgumentNullException(nameof(llmAgent));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<string> CompleteAsync(
            string prompt,
            CancellationToken ct = default)
        {
            _logger.LogDebug("LLM request: {Prompt}", prompt);

            try
            {
                var result = await _llmAgent.ExecuteAsync(
                    _llmAgent.Config.ModelId,
                    prompt,
                    null,
                    ct);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "LLM request failed");
                throw;
            }
        }

        public async Task<string> CompleteAsync(
            string systemPrompt,
            string userPrompt,
            CancellationToken ct = default)
        {
            _logger.LogDebug("LLM request with system prompt");

            try
            {
                var result = await _llmAgent.ExecuteAsync(
                    _llmAgent.Config.ModelId,
                    userPrompt,
                    systemPrompt,
                    ct);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "LLM request failed");
                throw;
            }
        }

        public AIAgent? GetUnderlyingAgent()
        {
            return _llmAgent;
        }
    }
}
