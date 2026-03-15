using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Core.Models.Dialog;

namespace CKY.MultiAgentFramework.Core.Abstractions
{
    /// <summary>
    /// 记忆分类器接口
    /// 智能区分短期记忆和长期记忆，实现自动遗忘策略
    /// Memory classifier interface - intelligently distinguishes short-term/long-term memory with automatic forgetting policy
    /// </summary>
    public interface IMemoryClassifier
    {
        /// <summary>
        /// 分类并存储记忆
        /// Classify and store memories
        /// </summary>
        Task<MemoryClassificationResult> ClassifyAndStoreAsync(
            string intent,
            Dictionary<string, object> slots,
            DialogContext context,
            CancellationToken ct = default);

        /// <summary>
        /// 评估记忆是否应该遗忘
        /// Evaluate if memory should be forgotten
        /// </summary>
        ForgettingDecision EvaluateForgetting(
            SemanticMemory memory,
            DateTime lastAccessed,
            int accessCount);
    }
}
