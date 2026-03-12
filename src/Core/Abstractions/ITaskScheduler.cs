using CKY.MultiAgentFramework.Core.Enums;
using CKY.MultiAgentFramework.Core.Models.Agent;
using CKY.MultiAgentFramework.Core.Models.Task;

namespace CKY.MultiAgentFramework.Core.Abstractions
{
    /// <summary>
    /// 任务调度器接口
    /// </summary>
    public interface ITaskScheduler
    {
        /// <summary>
        /// 调度任务
        /// </summary>
        Task ScheduleAsync(
            DecomposedTask task,
            CancellationToken ct = default);

        /// <summary>
        /// 取消任务
        /// </summary>
        Task CancelAsync(
            string taskId,
            CancellationToken ct = default);

        /// <summary>
        /// 获取待执行任务列表（按优先级排序）
        /// </summary>
        Task<List<DecomposedTask>> GetPendingTasksAsync(CancellationToken ct = default);
    }

    /// <summary>
    /// 优先级计算器接口
    /// </summary>
    public interface IPriorityCalculator
    {
        /// <summary>
        /// 计算任务优先级评分
        /// </summary>
        int CalculateScore(DecomposedTask task);

        /// <summary>
        /// 比较两个任务的优先级
        /// </summary>
        int Compare(DecomposedTask task1, DecomposedTask task2);
    }
}
