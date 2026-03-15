using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Core.Models.Dialog;
using CKY.MultiAgentFramework.Core.Models.LLM;
using Microsoft.Extensions.Logging;

namespace CKY.MultiAgentFramework.Services.Dialog
{
    /// <summary>
    /// 槽位管理器，负责检测、填充和管理对话槽位
    /// Slot manager, responsible for detecting, filling, and managing dialog slots
    /// </summary>
    public class SlotManager : ISlotManager
    {
        private readonly ISlotDefinitionProvider _slotDefinitionProvider;
        private readonly IMafAiAgentRegistry _llmRegistry;
        private readonly ILogger<SlotManager> _logger;

        public SlotManager(
            ISlotDefinitionProvider slotDefinitionProvider,
            IMafAiAgentRegistry llmRegistry,
            ILogger<SlotManager> logger)
        {
            _slotDefinitionProvider = slotDefinitionProvider ?? throw new ArgumentNullException(nameof(slotDefinitionProvider));
            _llmRegistry = llmRegistry ?? throw new ArgumentNullException(nameof(llmRegistry));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<SlotDetectionResult> DetectMissingSlotsAsync(
            string userInput,
            IntentRecognitionResult intent,
            EntityExtractionResult entities,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(userInput))
                throw new ArgumentException("User input cannot be empty", nameof(userInput));

            if (intent == null)
                throw new ArgumentNullException(nameof(intent));

            if (entities == null)
                throw new ArgumentNullException(nameof(entities));

            _logger.LogDebug("Detecting missing slots for intent: {Intent}", intent.PrimaryIntent);

            var slotDef = _slotDefinitionProvider.GetDefinition(intent.PrimaryIntent);

            if (slotDef != null)
            {
                // 预定义意图 → 使用模板检测
                return await DetectWithTemplateAsync(slotDef, entities, ct);
            }

            // 未知意图 → 使用LLM动态识别
            return await DetectWithLlmAsync(userInput, intent.PrimaryIntent, entities, ct);
        }

        private async Task<SlotDetectionResult> DetectWithTemplateAsync(
            IntentSlotDefinition slotDef,
            EntityExtractionResult entities,
            CancellationToken ct)
        {
            var missingSlots = new List<SlotDefinition>();
            var detectedSlots = new Dictionary<string, object>();

            // 检查必需槽位
            // Check required slots
            foreach (var requiredSlot in slotDef.RequiredSlots)
            {
                if (entities.Entities.ContainsKey(requiredSlot.SlotName))
                {
                    detectedSlots[requiredSlot.SlotName] = entities.Entities[requiredSlot.SlotName];
                }
                else
                {
                    missingSlots.Add(requiredSlot);
                }
            }

            var confidence = slotDef.RequiredSlots.Count == 0
                ? 1.0
                : (double)detectedSlots.Count / slotDef.RequiredSlots.Count;

            return new SlotDetectionResult
            {
                Intent = slotDef.Intent,
                MissingSlots = missingSlots,
                DetectedSlots = detectedSlots,
                Confidence = confidence
            };
        }

        private async Task<SlotDetectionResult> DetectWithLlmAsync(
            string userInput,
            string intent,
            EntityExtractionResult entities,
            CancellationToken ct)
        {
            _logger.LogInformation("Using LLM to detect slots for unknown intent: {Intent}", intent);

            var prompt = $@"
分析用户请求，识别完成该意图所需的槽位信息：

用户输入：{userInput}
识别意图：{intent}

请分析：
1. 完成该意图需要哪些信息槽位（slots）？
2. 用户已提供了哪些槽位？
3. 缺失哪些槽位？

返回JSON格式：
{{
  ""required_slots"": [
    {{ ""name"": ""Location"", ""description"": ""城市"", ""provided"": false }},
    {{ ""name"": ""Date"", ""description"": ""日期"", ""provided"": true, ""value"": ""今天"" }}
  ],
  ""confidence"": 0.5
}}
";

            try
            {
                var llmAgent = await _llmRegistry.GetBestAgentAsync(LlmScenario.Intent, ct);
                var modelId = llmAgent.GetCurrentModelId();
                var response = await llmAgent.ExecuteAsync(modelId, prompt, null, ct);

                return ParseLlmSlotDetection(response, intent, entities);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "LLM slot detection failed for intent: {Intent}", intent);
                return new SlotDetectionResult
                {
                    Intent = intent,
                    Confidence = 0.0
                };
            }
        }

        private SlotDetectionResult ParseLlmSlotDetection(
            string llmResponse,
            string intent,
            EntityExtractionResult entities)
        {
            try
            {
                // 简单的JSON解析 - 提取 confidence 和 required_slots
                // TODO: 后续可以使用 System.Text.Json 进行更完善的解析

                // 提取 confidence
                var confidenceMatch = System.Text.RegularExpressions.Regex.Match(llmResponse, @"""confidence""\s*:\s*([\d.]+)");
                var confidence = confidenceMatch.Success ? double.Parse(confidenceMatch.Groups[1].Value) : 0.5;

                // 使用 entities.Entities 作为已检测到的槽位
                var detectedSlots = new Dictionary<string, object>(entities.Entities);

                // 提取缺失的槽位（简单解析）
                var missingSlots = new List<SlotDefinition>();
                var slotMatches = System.Text.RegularExpressions.Regex.Matches(llmResponse, @"""name""\s*:\s*""([^""]+)""\s*,\s*""description""\s*:\s*""([^""]+)""\s*,\s*""provided""\s*:\s*false");

                foreach (System.Text.RegularExpressions.Match match in slotMatches)
                {
                    missingSlots.Add(new SlotDefinition
                    {
                        SlotName = match.Groups[1].Value,
                        Description = match.Groups[2].Value
                    });
                }

                return new SlotDetectionResult
                {
                    Intent = intent,
                    DetectedSlots = detectedSlots,
                    MissingSlots = missingSlots,
                    Confidence = confidence
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse LLM response, returning default result");
                return new SlotDetectionResult
                {
                    Intent = intent,
                    DetectedSlots = entities.Entities,
                    Confidence = 0.3 // Low confidence for parsing failures
                };
            }
        }

        public Task<Dictionary<string, object>> FillSlotsAsync(
            string intent,
            Dictionary<string, object> providedSlots,
            DialogContext context,
            CancellationToken ct = default)
        {
            _logger.LogDebug("Filling slots for intent: {Intent}", intent);

            var filledSlots = new Dictionary<string, object>(providedSlots);
            var slotDef = _slotDefinitionProvider.GetDefinition(intent);

            if (slotDef == null)
            {
                _logger.LogWarning("No slot definition found for intent: {Intent}", intent);
                return Task.FromResult(filledSlots);
            }

            // Get all slots (required + optional)
            var allSlots = slotDef.GetAllSlots();

            foreach (var slot in allSlots)
            {
                var slotKey = slot.SlotName;

                // Skip if already provided
                if (filledSlots.ContainsKey(slotKey))
                    continue;

                // Strategy 1: Try historical preference
                if (context?.HistoricalSlots != null)
                {
                    var historyKey = $"{intent}.{slotKey}";
                    if (context.HistoricalSlots.TryGetValue(historyKey, out var historicalValue))
                    {
                        filledSlots[slotKey] = historicalValue;
                        _logger.LogDebug("Filled slot {Slot} from historical preference: {Value}", slotKey, historicalValue);
                        continue;
                    }
                }

                // Strategy 2: Try previous intent's slot value (coreference resolution)
                if (context?.PreviousSlots != null &&
                    context.PreviousIntent != null &&
                    context.PreviousIntent == intent)
                {
                    if (context.PreviousSlots.TryGetValue(slotKey, out var previousValue))
                    {
                        filledSlots[slotKey] = previousValue;
                        _logger.LogDebug("Filled slot {Slot} from previous turn: {Value}", slotKey, previousValue);
                        continue;
                    }
                }

                // Strategy 3: Use default value
                if (slot.HasDefaultValue && slot.DefaultValue != null)
                {
                    filledSlots[slotKey] = slot.DefaultValue;
                    _logger.LogDebug("Filled slot {Slot} with default value: {Value}", slotKey, slot.DefaultValue);
                }
            }

            return Task.FromResult(filledSlots);
        }

        public Task<string> GenerateClarificationAsync(
            List<SlotDefinition> missingSlots,
            string intent,
            CancellationToken ct = default)
        {
            // TODO: Task 1.6 实现
            // TODO: Implement in Task 1.6
            return Task.FromResult("请提供更多信息");
        }
    }
}
