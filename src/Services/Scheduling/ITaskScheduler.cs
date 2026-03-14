using CKY.MultiAgentFramework.Core.Enums;
using CKY.MultiAgentFramework.Core.Models.Agent;
using CKY.MultiAgentFramework.Core.Models.Task;

namespace CKY.MultiAgentFramework.Services.Scheduling
{
    /// <summary>
    /// 任务调度器接口
    /// </summary>
    public interface ITaskScheduler
    {
        /// <summary>
        /// 调度多个任务并生成执行计划
        /// </summary>
        Task<ScheduleResult> ScheduleAsync(
            List<DecomposedTask> tasks,
            CancellationToken ct = default);

        /// <summary>
        /// 执行单个任务
        /// </summary>
        Task<TaskExecutionResult> ExecuteTaskAsync(
            DecomposedTask task,
            Func<DecomposedTask, CancellationToken, Task<TaskExecutionResult>> executor,
            CancellationToken ct = default);
    }
}
