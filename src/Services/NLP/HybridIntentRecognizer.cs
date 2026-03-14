using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Core.Models.Task;
using Microsoft.Extensions.Logging;

namespace CKY.MultiAgentFramework.Services.NLP
{
    /// <summary>
    /// 混合意图识别器
    /// 结合LLM语义理解和规则识别，提供高准确率+高可靠性的意图识别
    /// 策略：优先使用LLM，置信度低时降级到规则识别
    /// </summary>
    public class HybridIntentRecognizer : IIntentRecognizer
    {
        private readonly IIntentRecognizer _llmRecognizer;
        private readonly IIntentRecognizer _ruleRecognizer;
        private readonly ILogger<HybridIntentRecognizer> _logger;

        /// <summary>
        /// LLM识别的最低置信度阈值，低于此值使用规则识别
        /// </summary>
        public double ConfidenceThreshold { get; set; } = 0.7;

        public HybridIntentRecognizer(
            IIntentRecognizer llmRecognizer,
            IIntentRecognizer ruleRecognizer,
            ILogger<HybridIntentRecognizer> logger)
        {
            _llmRecognizer = llmRecognizer ?? throw new ArgumentNullException(nameof(llmRecognizer));
            _ruleRecognizer = ruleRecognizer ?? throw new ArgumentNullException(nameof(ruleRecognizer));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public async Task<IntentRecognitionResult> RecognizeAsync(
            string userInput,
            CancellationToken ct = default)
        {
            _logger.LogDebug("Hybrid recognizer processing: {Input}", userInput);

            try
            {
                // 1. 首先尝试 LLM 识别
                var llmResult = await _llmRecognizer.RecognizeAsync(userInput, ct);

                _logger.LogDebug("LLM result: {Intent} (confidence: {Confidence})",
                    llmResult.PrimaryIntent, llmResult.Confidence);

                // 2. 如果LLM置信度足够高，直接返回
                if (llmResult.Confidence >= ConfidenceThreshold)
                {
                    _logger.LogInformation("Using LLM result (high confidence: {Confidence})", llmResult.Confidence);
                    llmResult.Tags.Add("recognition_method:llm");
                    return llmResult;
                }

                // 3. LLM置信度低，使用规则识别作为补充
                _logger.LogInformation("LLM confidence low ({Confidence}), using rule-based fallback", llmResult.Confidence);
                var ruleResult = await _ruleRecognizer.RecognizeAsync(userInput, ct);

                _logger.LogDebug("Rule-based result: {Intent} (confidence: {Confidence})",
                    ruleResult.PrimaryIntent, ruleResult.Confidence);

                // 4. 如果规则识别置信度更高，使用规则结果
                if (ruleResult.Confidence > llmResult.Confidence)
                {
                    _logger.LogInformation("Using rule-based result (higher confidence)");
                    ruleResult.Tags.Add("recognition_method:rule_fallback");
                    ruleResult.Tags.Add($"llm_confidence:{llmResult.Confidence:F2}");
                    return ruleResult;
                }

                // 5. 否则仍使用LLM结果（但标记低置信度）
                _logger.LogInformation("Using LLM result (despite low confidence)");
                llmResult.Tags.Add("recognition_method:llm_low_confidence");
                llmResult.Tags.Add($"rule_confidence:{ruleResult.Confidence:F2}");

                // 将规则识别的备选意图添加到LLM结果中
                if (ruleResult.PrimaryIntent != "Unknown" && ruleResult.PrimaryIntent != llmResult.PrimaryIntent)
                {
                    llmResult.AlternativeIntents[ruleResult.PrimaryIntent] = ruleResult.Confidence;
                }

                return llmResult;
            }
            catch (Exception ex)
            {
                // LLM识别失败，降级到规则识别
                _logger.LogError(ex, "LLM recognition failed, falling back to rule-based");
                var ruleResult = await _ruleRecognizer.RecognizeAsync(userInput, ct);
                ruleResult.Tags.Add("recognition_method:rule_fallback");
                ruleResult.Tags.Add("llm_error:failed");
                return ruleResult;
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
    }
}
