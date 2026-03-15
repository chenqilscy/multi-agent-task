using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Core.Models.Dialog;
using CKY.MultiAgentFramework.Core.Models.LLM;
using Microsoft.Extensions.Logging;

namespace CKY.MultiAgentFramework.Services.Dialog
{
    /// <summary>
    /// 澄清管理器，负责分析澄清需求、生成澄清问题并处理用户响应
    /// Clarification manager, responsible for analyzing clarification needs, generating clarification questions, and processing user responses
    /// </summary>
    public class ClarificationManager : IClarificationManager
    {
        private readonly ISlotManager _slotManager;
        private readonly IMafAiAgentRegistry _llmRegistry;
        private readonly ILogger<ClarificationManager> _logger;

        public ClarificationManager(
            ISlotManager slotManager,
            IMafAiAgentRegistry llmRegistry,
            ILogger<ClarificationManager> logger)
        {
            _slotManager = slotManager ?? throw new ArgumentNullException(nameof(slotManager));
            _llmRegistry = llmRegistry ?? throw new ArgumentNullException(nameof(llmRegistry));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<ClarificationAnalysis> AnalyzeClarificationNeededAsync(
            SlotDetectionResult slotDetection,
            DialogContext context,
            CancellationToken ct = default)
        {
            if (slotDetection == null)
                throw new ArgumentNullException(nameof(slotDetection));

            _logger.LogDebug("Analyzing clarification need for intent: {Intent}", slotDetection.Intent);

            var analysis = new ClarificationAnalysis
            {
                MissingSlots = slotDetection.MissingSlots,
                Confidence = slotDetection.Confidence,
                Context = context
            };

            // Determine if clarification is needed
            analysis.NeedsClarification = slotDetection.MissingSlots.Count > 0;

            if (!analysis.NeedsClarification)
            {
                analysis.Strategy = ClarificationStrategy.Template;
                analysis.EstimatedTurns = 0;
                return analysis;
            }

            // Select strategy based on missing slots and context
            analysis.Strategy = SelectStrategy(slotDetection, context);

            // Calculate estimated turns
            analysis.EstimatedTurns = CalculateEstimatedTurns(slotDetection.MissingSlots, analysis.Strategy);

            // Generate suggested values from historical preferences
            analysis.SuggestedValues = GenerateSuggestedValues(slotDetection.MissingSlots, context, slotDetection.Intent);

            // Determine if confirmation is needed
            analysis.RequiresConfirmation = analysis.SuggestedValues.Count > 0 && analysis.Strategy == ClarificationStrategy.SmartInference;

            return analysis;
        }

        private ClarificationStrategy SelectStrategy(SlotDetectionResult slotDetection, DialogContext? context)
        {
            // Strategy 1: Template for simple cases
            if (slotDetection.MissingSlots.Count <= 2)
            {
                return ClarificationStrategy.Template;
            }

            // Strategy 2: Smart inference for cases with historical data
            if (context?.HistoricalSlots != null && context.HistoricalSlots.Count > 0)
            {
                var hasRelevantHistory = slotDetection.MissingSlots.Any(slot =>
                    context.HistoricalSlots.ContainsKey($"{slotDetection.Intent}.{slot.SlotName}"));

                if (hasRelevantHistory)
                {
                    return ClarificationStrategy.SmartInference;
                }
            }

            // Strategy 3: LLM for complex cases
            if (slotDetection.MissingSlots.Count > 3)
            {
                return ClarificationStrategy.LLM;
            }

            // Default: Template
            return ClarificationStrategy.Template;
        }

        private int CalculateEstimatedTurns(List<SlotDefinition> missingSlots, ClarificationStrategy strategy)
        {
            return strategy switch
            {
                ClarificationStrategy.Template => missingSlots.Count,
                ClarificationStrategy.SmartInference => Math.Max(1, missingSlots.Count / 2),
                ClarificationStrategy.LLM => 1,
                ClarificationStrategy.Hybrid => Math.Max(1, missingSlots.Count - 1),
                _ => missingSlots.Count
            };
        }

        private Dictionary<string, object> GenerateSuggestedValues(List<SlotDefinition> missingSlots, DialogContext? context, string intent)
        {
            var suggested = new Dictionary<string, object>();

            if (context?.HistoricalSlots == null)
                return suggested;

            foreach (var slot in missingSlots)
            {
                var key = $"{intent}.{slot.SlotName}";
                if (context.HistoricalSlots.TryGetValue(key, out var value))
                {
                    suggested[slot.SlotName] = value;
                }
            }

            return suggested;
        }

        public async Task<string> GenerateClarificationQuestionAsync(
            ClarificationContext clarificationContext,
            CancellationToken ct = default)
        {
            if (clarificationContext == null)
                throw new ArgumentNullException(nameof(clarificationContext));

            _logger.LogDebug("Generating clarification question for strategy: {Strategy}", clarificationContext.Strategy);

            return clarificationContext.Strategy switch
            {
                ClarificationStrategy.Template => await GenerateTemplateQuestionAsync(clarificationContext, ct),
                ClarificationStrategy.SmartInference => await GenerateSmartInferenceQuestionAsync(clarificationContext, ct),
                ClarificationStrategy.LLM => await GenerateLlmQuestionAsync(clarificationContext, ct),
                ClarificationStrategy.Hybrid => await GenerateHybridQuestionAsync(clarificationContext, ct),
                _ => "请提供更多信息"
            };
        }

        private async Task<string> GenerateTemplateQuestionAsync(ClarificationContext context, CancellationToken ct)
        {
            // Reuse SlotManager's clarification generation
            return await _slotManager.GenerateClarificationAsync(
                context.MissingSlots,
                context.Intent,
                ct);
        }

        private async Task<string> GenerateSmartInferenceQuestionAsync(ClarificationContext context, CancellationToken ct)
        {
            var firstMissing = context.MissingSlots.FirstOrDefault();
            if (firstMissing == null)
                return "所有信息已完整";

            var suggestedValue = context.FilledSlots.GetValueOrDefault(firstMissing.SlotName);
            if (suggestedValue != null)
            {
                return $"根据历史记录，您想要{firstMissing.Description}设置为{suggestedValue}吗？";
            }

            return await GenerateTemplateQuestionAsync(context, ct);
        }

        private async Task<string> GenerateLlmQuestionAsync(ClarificationContext context, CancellationToken ct)
        {
            var prompt = $@"
生成一个自然的澄清问题，询问用户以下缺失信息：

意图：{context.Intent}
缺失槽位：{string.Join("、", context.MissingSlots.Select(s => $"{s.SlotName}({s.Description})"))}

请生成一个简洁、自然的中文问题。";

            try
            {
                var llmAgent = await _llmRegistry.GetBestAgentAsync(LlmScenario.Intent, ct);
                var response = await llmAgent.ExecuteAsync(
                    llmAgent.GetCurrentModelId(),
                    prompt,
                    null,
                    ct);

                return response.Trim();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "LLM question generation failed, falling back to template");
                return await GenerateTemplateQuestionAsync(context, ct);
            }
        }

        private async Task<string> GenerateHybridQuestionAsync(ClarificationContext context, CancellationToken ct)
        {
            // Combine template and smart inference
            var suggestedCount = context.FilledSlots.Count;
            var missingCount = context.MissingSlots.Count;

            if (suggestedCount > 0 && missingCount > 0)
            {
                var suggested = string.Join("、", context.FilledSlots.Keys);
                var missing = string.Join("、", context.MissingSlots.Select(s => s.Description));
                return $"我已为您设置{suggested}，请问{missing}是什么？";
            }

            return await GenerateTemplateQuestionAsync(context, ct);
        }

        public async Task<ClarificationResponse> ProcessUserResponseAsync(
            string userInput,
            ClarificationContext clarificationContext,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(userInput))
                throw new ArgumentException("User input cannot be empty", nameof(userInput));

            if (clarificationContext == null)
                throw new ArgumentNullException(nameof(clarificationContext));

            _logger.LogDebug("Processing user response for clarification");

            var response = new ClarificationResponse
            {
                UpdatedSlots = new Dictionary<string, object>(clarificationContext.FilledSlots)
            };

            // Extract entities from user input
            // This is simplified - in production, use entity extractor
            foreach (var slot in clarificationContext.MissingSlots.ToList())
            {
                if (userInput.Contains(slot.Description) ||
                    (slot.Synonyms.Count > 0 && slot.Synonyms.Any(s => userInput.Contains(s))))
                {
                    // Extract value (simplified)
                    var value = ExtractValueFromInput(userInput, slot);
                    if (value != null)
                    {
                        response.UpdatedSlots[slot.SlotName] = value;
                        clarificationContext.MissingSlots.Remove(slot);
                        clarificationContext.FilledSlots[slot.SlotName] = value;
                    }
                }
            }

            response.StillMissing = clarificationContext.MissingSlots;
            response.Completed = clarificationContext.MissingSlots.Count == 0;
            response.NeedsFurtherClarification = !response.Completed;

            if (response.Completed)
            {
                clarificationContext.IsCompleted = true;
                response.Message = "谢谢，信息已完整";
            }
            else
            {
                clarificationContext.TurnCount++;
                response.Message = $"收到，继续确认：{string.Join("、", clarificationContext.MissingSlots.Select(s => s.Description))}";
            }

            return response;
        }

        private object? ExtractValueFromInput(string input, SlotDefinition slot)
        {
            // Simplified value extraction
            // In production, use NER or pattern matching
            if (slot.Type == SlotType.Enumeration && slot.ValidValues != null)
            {
                foreach (var value in slot.ValidValues)
                {
                    if (input.Contains(value))
                        return value;
                }
            }

            if (slot.Type == SlotType.Integer && int.TryParse(input, out var intValue))
            {
                return intValue;
            }

            // Default: return input as string
            return input;
        }
    }
}
