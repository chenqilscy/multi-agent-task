using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Core.Agents;
using CKY.MultiAgentFramework.Core.Models.LLM;
using Microsoft.Extensions.Logging;

namespace CKY.MultiAgentFramework.Services.NLP
{
    /// <summary>
    /// 基于LLM的意图识别器
    /// 使用 LlmAgent Registry 获取合适的 LLM 实例进行语义理解
    /// </summary>
    public class LlmIntentRecognizer : IIntentRecognizer
    {
        private readonly ILlmAgentRegistry _llmRegistry;
        private readonly IIntentKeywordProvider _keywordProvider;
        private readonly ILogger<LlmIntentRecognizer> _logger;

        public LlmIntentRecognizer(
            ILlmAgentRegistry llmRegistry,
            IIntentKeywordProvider keywordProvider,
            ILogger<LlmIntentRecognizer> logger)
        {
            _llmRegistry = llmRegistry ?? throw new ArgumentNullException(nameof(llmRegistry));
            _keywordProvider = keywordProvider ?? throw new ArgumentNullException(nameof(keywordProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public async Task<IntentRecognitionResult> RecognizeAsync(
            string userInput,
            CancellationToken ct = default)
        {
            _logger.LogDebug("Recognizing intent with LLM: {Input}", userInput);

            try
            {
                var systemPrompt = BuildSystemPrompt();
                var userPrompt = BuildUserPrompt(userInput);

                // 获取支持 Intent 场景的 LLM Agent
                var llmAgent = await _llmRegistry.GetBestAgentAsync(LlmScenario.Intent, ct);
                var response = await llmAgent.ExecuteAsync(
                    llmAgent.GetCurrentModelId(),
                    userPrompt,
                    systemPrompt,
                    ct);

                var result = ParseLlmResponse(response, userInput);

                _logger.LogInformation("LLM recognized intent: {Intent} (confidence: {Confidence})",
                    result.PrimaryIntent, result.Confidence);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "LLM intent recognition failed");
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<List<IntentRecognitionResult>> RecognizeBatchAsync(
            List<string> userInputs,
            CancellationToken ct = default)
        {
            var tasks = userInputs.Select(input => RecognizeAsync(input, ct));
            var results = await Task.WhenAll(tasks);
            return results.ToList();
        }

        private string BuildSystemPrompt()
        {
            return "你是一个专业的意图识别助手。你需要分析用户输入，识别其主要意图，并以JSON格式返回结果。";
        }

        private string BuildUserPrompt(string userInput)
        {
            var supportedIntents = _keywordProvider.GetSupportedIntents();

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("请识别以下用户输入的意图。");
            sb.AppendLine();
            sb.AppendLine("支持的意图类型：");

            foreach (var intent in supportedIntents)
            {
                var keywords = _keywordProvider.GetKeywords(intent);
                if (keywords != null && keywords.Length > 0)
                {
                    var sampleKeywords = string.Join("、", keywords.Take(3));
                    sb.AppendLine($"- {intent}: 相关关键词包括 {sampleKeywords}");
                }
            }

            sb.AppendLine();
            sb.AppendLine($"用户输入：{userInput}");
            sb.AppendLine();
            sb.AppendLine("请以JSON格式返回识别结果：");
            sb.AppendLine(@"{""primary_intent"": ""意图名称"", ""confidence"": 0.95}");

            return sb.ToString();
        }

        private IntentRecognitionResult ParseLlmResponse(string llmResponse, string userInput)
        {
            var result = new IntentRecognitionResult { OriginalInput = userInput };

            try
            {
                var jsonStart = llmResponse.IndexOf('{');
                var jsonEnd = llmResponse.LastIndexOf('}');

                if (jsonStart >= 0 && jsonEnd > jsonStart)
                {
                    var jsonStr = llmResponse.Substring(jsonStart, jsonEnd - jsonStart + 1);
                    var parsed = System.Text.Json.JsonDocument.Parse(jsonStr);

                    result.PrimaryIntent = parsed.RootElement.GetProperty("primary_intent").GetString() ?? "Unknown";
                    result.Confidence = parsed.RootElement.GetProperty("confidence").GetDouble();
                }
                else
                {
                    result.PrimaryIntent = "Unknown";
                    result.Confidence = 0.0;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing LLM response");
                result.PrimaryIntent = "Unknown";
                result.Confidence = 0.0;
            }

            return result;
        }
    }
}
