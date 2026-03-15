// src/Core/Abstractions/IDialogStateManager.cs
using CKY.MultiAgentFramework.Core.Models.Dialog;
using CKY.MultiAgentFramework.Core.Models.Task;

namespace CKY.MultiAgentFramework.Core.Abstractions
{
    /// <summary>
    /// 对话状态管理器接口
    /// 管理对话上下文的生命周期、轮次追踪和历史槽位
    /// </summary>
    public interface IDialogStateManager
    {
        /// <summary>
        /// 加载或创建对话上下文
        /// </summary>
        Task<DialogContext> LoadOrCreateAsync(
            string conversationId,
            string userId,
            CancellationToken ct = default);

        /// <summary>
        /// 更新对话状态
        /// </summary>
        Task UpdateAsync(
            DialogContext context,
            string intent,
            Dictionary<string, object> slots,
            List<TaskExecutionResult> executionResults,
            CancellationToken ct = default);

        /// <summary>
        /// 记录待处理的澄清
        /// </summary>
        Task RecordPendingClarificationAsync(
            DialogContext context,
            string intent,
            Dictionary<string, object> detectedSlots,
            List<SlotDefinition> missingSlots,
            CancellationToken ct = default);

        /// <summary>
        /// 记录待处理的任务（SubAgent槽位缺失时）
        /// </summary>
        Task RecordPendingTasksAsync(
            DialogContext context,
            ExecutionPlan plan,
            Dictionary<string, object> filledSlots,
            CancellationToken ct = default);

        /// <summary>
        /// 处理用户响应（针对澄清问题）
        /// </summary>
        Task<MafTaskResponse> HandleClarificationResponseAsync(
            string conversationId,
            string userResponse,
            CancellationToken ct = default);
    }
}
