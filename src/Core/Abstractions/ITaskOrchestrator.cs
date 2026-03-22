using CKY.MultiAgentFramework.Core.Models.Task;

namespace CKY.MultiAgentFramework.Core.Abstractions
{
    /// <summary>
    /// 任务编排器接口
    /// 根据任务依赖关系、优先级和资源约束，生成最优的执行计划
    /// </summary>
    public interface ITaskOrchestrator
    {
        /// <summary>
        /// 创建执行计划
        /// </summary>
        Task<ExecutionPlan> CreatePlanAsync(
            List<DecomposedTask> tasks,
            CancellationToken ct = default);

        /// <summary>
        /// 执行计划
        /// </summary>
        Task<List<TaskExecutionResult>> ExecutePlanAsync(
            ExecutionPlan plan,
            CancellationToken ct = default);

        /// <summary>
        /// 执行计划（使用自定义任务执行器）
        /// </summary>
        /// <param name="plan">执行计划</param>
        /// <param name="taskExecutor">任务执行回调，由调用方提供真实的Agent调用逻辑</param>
        /// <param name="ct">取消令牌</param>
        Task<List<TaskExecutionResult>> ExecutePlanAsync(
            ExecutionPlan plan,
            Func<DecomposedTask, CancellationToken, Task<TaskExecutionResult>> taskExecutor,
            CancellationToken ct = default);

        /// <summary>
        /// 取消执行
        /// </summary>
        Task CancelAsync(string planId, CancellationToken ct = default);
    }
}
