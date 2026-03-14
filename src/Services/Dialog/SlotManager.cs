using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Core.Models.Dialog;
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
            if (slotDef == null)
            {
                _logger.LogWarning("No slot definition found for intent: {Intent}", intent.PrimaryIntent);
                return new SlotDetectionResult
                {
                    Intent = intent.PrimaryIntent,
                    Confidence = 0.0
                };
            }

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
                Intent = intent.PrimaryIntent,
                MissingSlots = missingSlots,
                DetectedSlots = detectedSlots,
                Confidence = confidence
            };
        }

        public Task<Dictionary<string, object>> FillSlotsAsync(
            string intent,
            Dictionary<string, object> providedSlots,
            object context,
            CancellationToken ct = default)
        {
            // TODO: Task 1.5 实现
            // TODO: Implement in Task 1.5
            // TODO: Task 1.5 - Pass ct to LLM calls when implementing FillSlotsAsync
            return Task.FromResult(providedSlots);
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
