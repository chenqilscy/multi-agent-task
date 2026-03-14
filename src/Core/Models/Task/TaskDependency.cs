using CKY.MultiAgentFramework.Core.Enums;

namespace CKY.MultiAgentFramework.Core.Models.Task
{
    /// <summary>
    /// 任务依赖关系（定义任务之间的执行顺序约束）
    /// </summary>
    /// <remarks>
    /// 设计目的：
    /// - 确保任务按正确的顺序执行
    /// - 支持复杂的依赖场景（数据依赖、执行依赖）
    /// - 自动检测循环依赖和死锁
    /// - 支持条件依赖（基于前置任务的结果）
    ///
    /// 依赖类型：
    /// - MustComplete: 前置任务必须完成（无论成功或失败）
    /// - MustSucceed: 前置任务必须成功完成
    /// - MustStart: 前置任务必须开始执行（允许并行）
    /// - DataDependency: 前置任务必须完成且有输出数据
    ///
    /// 使用场景：
    /// <code>
    /// // 任务 B 依赖任务 A 成功完成
    /// var dependency = new TaskDependency
    /// {
    ///     DependsOnTaskId = "task-a",
    ///     Type = DependencyType.MustSucceed
    /// };
    ///
    /// // 任务 C 依赖任务 B 的数据输出
    /// var dataDependency = new TaskDependency
    /// {
    ///     DependsOnTaskId = "task-b",
    ///     Type = DependencyType.DataDependency
    /// };
    /// </code>
    ///
    /// 循环依赖检测：
    /// - 任务调度器会自动检测循环依赖
    /// - 如果检测到循环依赖，抛出 TaskSchedulingException
    /// - 建议在设计时避免循环依赖
    /// </remarks>
    public class TaskDependency
    {
        /// <summary>
        /// 获取或设置依赖的任务 ID
        /// </summary>
        public string DependsOnTaskId { get; set; } = string.Empty;

        /// <summary>
        /// 获取或设置依赖类型
        /// </summary>
        public DependencyType Type { get; set; }

        /// <summary>
        /// 获取或设置依赖是否已满足
        /// </summary>
        public bool IsSatisfied { get; set; }

        /// <summary>
        /// 获取或设置可选的条件表达式（用于复杂依赖场景）
        /// </summary>
        /// <remarks>
        /// 示例："Result.Data.Confidence > 0.8"
        /// 仅当前置任务的输出满足条件时，依赖才算满足
        /// </remarks>
        public string? Condition { get; set; }

        /// <summary>
        /// 检查依赖是否满足
        /// </summary>
        /// <param name="targetTask">要检查的目标任务</param>
        /// <returns>如果依赖满足则返回 true，否则返回 false</returns>
        public bool CheckSatisfied(DecomposedTask targetTask)
        {
            if (targetTask == null) return false;

            return Type switch
            {
                DependencyType.MustComplete => targetTask.Status == MafTaskStatus.Completed,
                DependencyType.MustSucceed => targetTask.Status == MafTaskStatus.Completed
                                      && targetTask.Result?.Success == true,
                DependencyType.MustStart => targetTask.Status == MafTaskStatus.Running
                                      || targetTask.Status == MafTaskStatus.Completed,
                DependencyType.DataDependency => targetTask.Status == MafTaskStatus.Completed
                                           && targetTask.Result?.Data != null,
                _ => false
            };
        }
    }
}
