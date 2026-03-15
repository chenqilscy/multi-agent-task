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
            return await DetectMissingSlotsAsync(userInput, intent, entities, null!, ct);
        }

        public async Task<SlotDetectionResult> DetectMissingSlotsAsync(
            string userInput,
            IntentRecognitionResult intent,
            EntityExtractionResult entities,
            DialogContext context,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(userInput))
                throw new ArgumentException("User input cannot be empty", nameof(userInput));

            if (intent == null)
                throw new ArgumentNullException(nameof(intent));

            if (entities == null)
                throw new ArgumentNullException(nameof(entities));

            _logger.LogDebug("Detecting missing slots for intent: {Intent} with context", intent.PrimaryIntent);

            var slotDef = _slotDefinitionProvider.GetDefinition(intent.PrimaryIntent);

            if (slotDef != null)
            {
                // 预定义意图 → 使用模板检测
                var result = await DetectWithTemplateAsync(slotDef, entities, ct);

                // 如果提供了上下文，尝试自动填充槽位
                if (context != null && result.MissingSlots.Count > 0)
                {
                    result = await AutoFillSlotsFromContextAsync(result, slotDef, context, intent.PrimaryIntent);
                }

                return result;
            }

            // 未知意图 → 使用LLM动态识别
            return await DetectWithLlmAsync(userInput, intent.PrimaryIntent, entities, context, ct);
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

        /// <summary>
        /// 从对话上下文自动填充槽位
        /// Auto-fill slots from dialog context
        /// </summary>
        private async Task<SlotDetectionResult> AutoFillSlotsFromContextAsync(
            SlotDetectionResult originalResult,
            IntentSlotDefinition slotDef,
            DialogContext context,
            string intent)
        {
            var filledResult = new SlotDetectionResult
            {
                Intent = originalResult.Intent,
                DetectedSlots = new Dictionary<string, object>(originalResult.DetectedSlots),
                MissingSlots = new List<SlotDefinition>(),
                Confidence = originalResult.Confidence
            };

            var allSlots = slotDef.GetAllSlots();
            var autoFilledCount = 0;

            foreach (var slot in allSlots)
            {
                var slotKey = slot.SlotName;

                // 如果槽位已经被检测到，跳过
                if (filledResult.DetectedSlots.ContainsKey(slotKey))
                    continue;

                // 策略1: 从历史偏好中填充
                if (context.HistoricalSlots != null)
                {
                    var historyKey = $"{intent}.{slotKey}";
                    if (context.HistoricalSlots.TryGetValue(historyKey, out var historicalValue))
                    {
                        filledResult.DetectedSlots[slotKey] = historicalValue;
                        autoFilledCount++;
                        _logger.LogDebug("Auto-filled slot {Slot} from historical preference: {Value}", slotKey, historicalValue);
                        continue;
                    }
                }

                // 策略2: 从上一轮对话中填充（仅当意图相同时）
                if (context.PreviousSlots != null &&
                    context.PreviousIntent != null &&
                    context.PreviousIntent == intent)
                {
                    if (context.PreviousSlots.TryGetValue(slotKey, out var previousValue))
                    {
                        filledResult.DetectedSlots[slotKey] = previousValue;
                        autoFilledCount++;
                        _logger.LogDebug("Auto-filled slot {Slot} from previous turn: {Value}", slotKey, previousValue);
                        continue;
                    }
                }

                // 策略3: 使用默认值
                if (slot.HasDefaultValue && slot.DefaultValue != null)
                {
                    filledResult.DetectedSlots[slotKey] = slot.DefaultValue;
                    autoFilledCount++;
                    _logger.LogDebug("Auto-filled slot {Slot} with default value: {Value}", slotKey, slot.DefaultValue);
                    continue;
                }

                // 无法自动填充，添加到缺失列表
                var missingSlot = originalResult.MissingSlots.FirstOrDefault(s => s.SlotName == slotKey);
                if (missingSlot != null)
                {
                    filledResult.MissingSlots.Add(missingSlot);
                }
            }

            // 重新计算置信度（包含自动填充的槽位）
            if (allSlots.Count > 0)
            {
                filledResult.Confidence = (double)filledResult.DetectedSlots.Count / allSlots.Count;
            }

            if (autoFilledCount > 0)
            {
                _logger.LogInformation("Auto-filled {Count} slots from context for intent: {Intent}", autoFilledCount, intent);
            }

            return await Task.FromResult(filledResult);
        }

        /// <summary>
        /// 从对话上下文自动填充LLM检测的槽位
        /// Auto-fill LLM-detected slots from dialog context
        /// </summary>
        private async Task<SlotDetectionResult> AutoFillSlotsFromLlmContextAsync(
            SlotDetectionResult originalResult,
            DialogContext context,
            string intent)
        {
            var filledResult = new SlotDetectionResult
            {
                Intent = originalResult.Intent,
                DetectedSlots = new Dictionary<string, object>(originalResult.DetectedSlots),
                MissingSlots = new List<SlotDefinition>(),
                Confidence = originalResult.Confidence
            };

            var autoFilledCount = 0;

            foreach (var slot in originalResult.MissingSlots)
            {
                var slotKey = slot.SlotName;

                // 策略1: 从历史偏好中填充
                if (context.HistoricalSlots != null)
                {
                    var historyKey = $"{intent}.{slotKey}";
                    if (context.HistoricalSlots.TryGetValue(historyKey, out var historicalValue))
                    {
                        filledResult.DetectedSlots[slotKey] = historicalValue;
                        autoFilledCount++;
                        _logger.LogDebug("Auto-filled LLM-detected slot {Slot} from historical preference: {Value}", slotKey, historicalValue);
                        continue;
                    }
                }

                // 策略2: 从上一轮对话中填充（仅当意图相同时）
                if (context.PreviousSlots != null &&
                    context.PreviousIntent != null &&
                    context.PreviousIntent == intent)
                {
                    if (context.PreviousSlots.TryGetValue(slotKey, out var previousValue))
                    {
                        filledResult.DetectedSlots[slotKey] = previousValue;
                        autoFilledCount++;
                        _logger.LogDebug("Auto-filled LLM-detected slot {Slot} from previous turn: {Value}", slotKey, previousValue);
                        continue;
                    }
                }

                // 策略3: 使用默认值
                if (slot.HasDefaultValue && slot.DefaultValue != null)
                {
                    filledResult.DetectedSlots[slotKey] = slot.DefaultValue;
                    autoFilledCount++;
                    _logger.LogDebug("Auto-filled LLM-detected slot {Slot} with default value: {Value}", slotKey, slot.DefaultValue);
                    continue;
                }

                // 无法自动填充，保留在缺失列表中
                filledResult.MissingSlots.Add(slot);
            }

            // 重新计算置信度
            if (filledResult.DetectedSlots.Count > 0)
            {
                filledResult.Confidence = Math.Min(1.0, filledResult.Confidence + (autoFilledCount * 0.1));
            }

            if (autoFilledCount > 0)
            {
                _logger.LogInformation("Auto-filled {Count} LLM-detected slots from context for intent: {Intent}", autoFilledCount, intent);
            }

            return await Task.FromResult(filledResult);
        }

        private async Task<SlotDetectionResult> DetectWithLlmAsync(
            string userInput,
            string intent,
            EntityExtractionResult entities,
            DialogContext? context,
            CancellationToken ct)
        {
            _logger.LogInformation("Using LLM to detect slots for unknown intent: {Intent}", intent);

            // Sanitize user input to prevent prompt injection
            var sanitizedInput = System.Text.RegularExpressions.Regex.Replace(userInput, @"[\r\n\t]", " ");

            var prompt = $@"
分析用户请求，识别完成该意图所需的槽位信息：

用户输入：{sanitizedInput}
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

                var result = ParseLlmSlotDetection(response, intent, entities);

                // 如果提供了上下文且有缺失槽位，尝试自动填充
                if (context != null && result.MissingSlots.Count > 0)
                {
                    result = await AutoFillSlotsFromLlmContextAsync(result, context, intent);
                }

                return result;
            }
            catch (System.Text.Json.JsonException ex)
            {
                _logger.LogError(ex, "JSON parsing failed during LLM slot detection for intent: {Intent}", intent);
                return new SlotDetectionResult
                {
                    Intent = intent,
                    Confidence = 0.0
                };
            }
            catch (System.InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation during LLM slot detection for intent: {Intent}", intent);
                return new SlotDetectionResult
                {
                    Intent = intent,
                    Confidence = 0.0
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in LLM slot detection for intent: {Intent}", intent);
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
            catch (System.Text.RegularExpressions.RegexMatchTimeoutException ex)
            {
                _logger.LogWarning(ex, "Regex timeout while parsing LLM response");
                return new SlotDetectionResult
                {
                    Intent = intent,
                    DetectedSlots = entities.Entities,
                    Confidence = 0.3 // Low confidence for parsing failures
                };
            }
            catch (FormatException ex)
            {
                _logger.LogWarning(ex, "Invalid format while parsing LLM response");
                return new SlotDetectionResult
                {
                    Intent = intent,
                    DetectedSlots = entities.Entities,
                    Confidence = 0.3
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Unexpected error parsing LLM response, returning default result");
                return new SlotDetectionResult
                {
                    Intent = intent,
                    DetectedSlots = entities.Entities,
                    Confidence = 0.3
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
            _logger.LogDebug("Generating clarification for intent: {Intent}, missing slots: {Count}", intent, missingSlots.Count);

            if (missingSlots.Count == 0)
            {
                return Task.FromResult(string.Empty);
            }

            // 策略1: 检查是否有必填槽位
            // Strategy 1: Check if there are required slots
            var requiredSlots = missingSlots.Where(s => s.Required).ToList();

            if (requiredSlots.Count > 0)
            {
                // 策略1a: 单个必填槽位缺失 - 直接询问
                // Strategy 1a: Single required slot missing - direct question
                if (requiredSlots.Count == 1 && missingSlots.Count == 1)
                {
                    var singleQuestion = GenerateSingleSlotQuestion(requiredSlots[0]);
                    return Task.FromResult(singleQuestion);
                }

                // 策略1b: 多个必填槽位缺失 - 列表询问或组合询问
                // Strategy 1b: Multiple required slots missing - list or combined question
                var multipleQuestion = GenerateMultipleSlotsQuestion(requiredSlots);
                return Task.FromResult(multipleQuestion);
            }

            // 策略2: 只有可选槽位缺失 - 询问是否需要
            // Strategy 2: Only optional slots missing - ask if needed
            var optionalQuestion = GenerateOptionalSlotsQuestion(missingSlots);
            return Task.FromResult(optionalQuestion);
        }

        private string GenerateSingleSlotQuestion(SlotDefinition slot)
        {
            // 优先级1: 有有效值列表 - 提供选项
            // Priority 1: Has valid values list - show options
            if (slot.ValidValues != null && slot.ValidValues.Length > 0)
            {
                var options = string.Join("、", slot.ValidValues);
                return $"请选择{slot.Description}：{options}";
            }

            // 优先级2: 有同义词 - 提供示例
            // Priority 2: Has synonyms - show examples
            if (slot.Synonyms.Count > 0)
            {
                var examples = string.Join("、", slot.Synonyms.Take(2));
                return $"请问您想要{slot.Description}？例如：{examples}";
            }

            // 默认: 简单询问
            // Default: simple question
            return $"请问{slot.Description}是什么？";
        }

        private string GenerateMultipleSlotsQuestion(List<SlotDefinition> slots)
        {
            // 检查是否可以通过一个自然语言问题覆盖多个槽位
            // Check if we can cover multiple slots with one natural language question
            if (TryGenerateCombinedQuestion(slots, out var combinedQuestion))
            {
                return combinedQuestion!;
            }

            // 默认: 列出所有槽位
            // Default: list all slots
            var slotDescriptions = string.Join("、", slots.Select(s => s.Description));
            return $"请提供以下信息：{slotDescriptions}";
        }

        private bool TryGenerateCombinedQuestion(List<SlotDefinition> slots, out string? question)
        {
            question = null;

            // 常见组合模式识别
            // Common combination pattern recognition
            var slotNames = slots.Select(s => s.SlotName).ToHashSet();

            // 模式1: Location + Date → "请问您想查询哪个城市哪天的天气？"
            // Pattern 1: Location + Date → "Which city and which day's weather?"
            if (slotNames.Contains("Location") && slotNames.Contains("Date"))
            {
                question = "请问您想查询哪个城市哪天的天气？";
                return true;
            }

            // 模式2: Device + Location + Action → "请问您想在哪个房间控制什么设备？"
            // Pattern 2: Device + Location + Action → "Which room and which device?"
            if (slotNames.Contains("Device") && slotNames.Contains("Location") && slotNames.Contains("Action"))
            {
                question = "请问您想在哪个房间控制什么设备？";
                return true;
            }

            // 模式3: Device + Location → "请问您想控制哪个房间的什么设备？"
            // Pattern 3: Device + Location → "Which room's which device?"
            if (slotNames.Contains("Device") && slotNames.Contains("Location"))
            {
                question = "请问您想控制哪个房间的什么设备？";
                return true;
            }

            return false;
        }

        private string GenerateOptionalSlotsQuestion(List<SlotDefinition> slots)
        {
            var slotDescriptions = string.Join("、", slots.Select(s => s.Description));
            return $"是否需要指定{slotDescriptions}？（可选）";
        }
    }
}
