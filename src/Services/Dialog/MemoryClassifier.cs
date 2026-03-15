using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Core.Models.Dialog;
using Microsoft.Extensions.Logging;

namespace CKY.MultiAgentFramework.Services.Dialog
{
    /// <summary>
    /// 记忆分类器实现
    /// Memory classifier implementation - intelligently distinguishes short-term/long-term memory
    /// </summary>
    public class MemoryClassifier : IMemoryClassifier
    {
        private readonly IMafMemoryManager _memoryManager;
        private readonly ILogger<MemoryClassifier> _logger;

        public MemoryClassifier(
            IMafMemoryManager memoryManager,
            ILogger<MemoryClassifier> logger)
        {
            _memoryManager = memoryManager ?? throw new ArgumentNullException(nameof(memoryManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 分类并存储记忆
        /// Classify and store memories
        /// </summary>
        public async Task<MemoryClassificationResult> ClassifyAndStoreAsync(
            string intent,
            Dictionary<string, object> slots,
            DialogContext context,
            CancellationToken ct = default)
        {
            _logger.LogDebug("Classifying and storing memories for intent {Intent}", intent);

            var result = new MemoryClassificationResult();

            foreach (var slot in slots)
            {
                var key = $"{intent}.{slot.Key}";

                // 规则1.1: 频次规则（≥3次）→ 长期记忆
                if (context.HistoricalSlots.TryGetValue(key, out var count) && count is int intCount && intCount >= 3)
                {
                    result.LongTermMemories.Add(new LongTermMemory
                    {
                        Key = key,
                        Value = slot.Value?.ToString() ?? "",
                        ImportanceScore = 0.8,
                        Tags = new List<string> { "用户偏好", "频繁使用" },
                        Reason = "出现3次以上"
                    });

                    await _memoryManager.SaveSemanticMemoryAsync(
                        context.UserId,
                        key,
                        slot.Value?.ToString() ?? "",
                        new List<string> { "用户偏好" },
                        ct);

                    _logger.LogInformation("Stored long-term memory: {Key} (count: {Count})", key, intCount);
                    continue;
                }

                // 规则2.1-2.4: 短期记忆规则
                result.ShortTermMemories.Add(new ShortTermMemory
                {
                    Key = key,
                    Value = slot.Value,
                    Expiry = TimeSpan.FromHours(24),
                    Reason = "临时信息"
                });

                _logger.LogDebug("Stored short-term memory: {Key}", key);
            }

            _logger.LogInformation("Memory classification complete: {LongTerm} long-term, {ShortTerm} short-term",
                result.LongTermMemories.Count, result.ShortTermMemories.Count);

            return result;
        }

        /// <summary>
        /// 评估记忆是否应该遗忘
        /// Evaluate if memory should be forgotten
        /// </summary>
        public ForgettingDecision EvaluateForgetting(
            SemanticMemory memory,
            DateTime lastAccessed,
            int accessCount)
        {
            var daysSinceLastAccess = (DateTime.UtcNow - lastAccessed).Days;

            // 规则1: 30天未访问 → 降级或删除
            if (daysSinceLastAccess > 30)
            {
                return accessCount > 10 ? ForgettingDecision.Downgrade : ForgettingDecision.Delete;
            }

            // 规则2: 90天以上 → 标记清理
            if (daysSinceLastAccess > 90)
            {
                return ForgettingDecision.MarkForCleanup;
            }

            return ForgettingDecision.Keep;
        }
    }
}
