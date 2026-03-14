namespace CKY.MultiAgentFramework.Core.Exceptions
{
    /// <summary>
    /// 任务调度异常（任务调度失败）
    /// </summary>
    /// <remarks>
    /// 使用场景：
    /// - 任务调度失败（资源不足、依赖冲突）
    /// - 任务执行失败（Agent 异常、超时）
    /// - 任务依赖失败（前置任务失败、循环依赖）
    /// - 任务状态转换失败（非法状态转换）
    /// - 任务优先级冲突
    ///
    /// 主要属性：
    /// - TaskId: 失败的任务 ID
    /// - IsRetryable = false: 任务调度失败通常不重试
    ///
    /// 错误分类：
    /// - 资源不足: 系统资源（内存、CPU、Agent 数量）不足
    /// - 依赖冲突: 前置任务失败或循环依赖
    /// - 状态冲突: 非法的状态转换（如从失败直接到完成）
    /// - 超时失败: 任务执行超过最大时长
    ///
    /// 错误处理建议：
    /// - 不应自动重试（可能重复失败）
    /// - 检查任务依赖关系和状态
    /// - 考虑手动重试或修复后重新调度
    /// - 记录详细错误日志用于诊断
    ///
    /// 示例：
    /// <code>
    /// try
    /// {
    ///     await _taskScheduler.ScheduleAsync(task);
    /// }
    /// catch (TaskSchedulingException ex) when (ex.TaskId == taskId)
    /// {
    ///     _logger.LogError(ex, "任务 {TaskId} 调度失败", ex.TaskId);
    ///     // 检查任务依赖和状态
    ///     await _taskManager.MarkFailedAsync(taskId, ex.Message);
    /// }
    /// </code>
    /// </remarks>
    public class TaskSchedulingException : MafException
    {
        /// <summary>
        /// 获取失败的任务 ID
        /// </summary>
        public string? TaskId { get; }

        /// <summary>
        /// 初始化 TaskSchedulingException 类的新实例
        /// </summary>
        /// <param name="taskId">任务 ID</param>
        /// <param name="message">错误消息</param>
        public TaskSchedulingException(
            string? taskId,
            string message)
            : base(MafErrorCode.TaskSchedulingError, message, isRetryable: false, component: "TaskScheduler")
        {
            TaskId = taskId;
        }
    }
}
