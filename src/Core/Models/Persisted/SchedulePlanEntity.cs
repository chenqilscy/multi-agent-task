using CKY.MultiAgentFramework.Core.Enums;

namespace CKY.MultiAgentFramework.Core.Models.Persisted
{
    /// <summary>
    /// 调度计划实体（用于持久化）
    /// </summary>
    public class SchedulePlanEntity
    {
        /// <summary>
        /// 主键
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 计划ID
        /// </summary>
        public string PlanId { get; set; } = string.Empty;

        /// <summary>
        /// 计划JSON（序列化的执行计划）
        /// </summary>
        public string PlanJson { get; set; } = string.Empty;

        /// <summary>
        /// 状态
        /// </summary>
        public SchedulePlanStatus Status { get; set; } = SchedulePlanStatus.Created;

        /// <summary>
        /// 任务总数
        /// </summary>
        public int TotalTasks { get; set; }

        /// <summary>
        /// 高优先级任务数
        /// </summary>
        public int HighPriorityCount { get; set; }

        /// <summary>
        /// 中优先级任务数
        /// </summary>
        public int MediumPriorityCount { get; set; }

        /// <summary>
        /// 低优先级任务数
        /// </summary>
        public int LowPriorityCount { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 开始执行时间
        /// </summary>
        public DateTime? StartedAt { get; set; }

        /// <summary>
        /// 完成时间
        /// </summary>
        public DateTime? CompletedAt { get; set; }

        /// <summary>
        /// 错误信息（如果失败）
        /// </summary>
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// 执行计划实体（用于持久化）
    /// </summary>
    public class ExecutionPlanEntity
    {
        /// <summary>
        /// 主键
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 计划ID
        /// </summary>
        public string PlanId { get; set; } = string.Empty;

        /// <summary>
        /// 计划JSON（序列化的执行计划）
        /// </summary>
        public string PlanJson { get; set; } = string.Empty;

        /// <summary>
        /// 状态
        /// </summary>
        public ExecutionPlanStatus Status { get; set; } = ExecutionPlanStatus.Created;

        /// <summary>
        /// 串行任务组数量
        /// </summary>
        public int SerialGroupCount { get; set; }

        /// <summary>
        /// 并行任务组数量
        /// </summary>
        public int ParallelGroupCount { get; set; }

        /// <summary>
        /// 总任务数
        /// </summary>
        public int TotalTasks { get; set; }

        /// <summary>
        /// 是否允许部分执行
        /// </summary>
        public bool AllowPartialExecution { get; set; } = true;

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 开始执行时间
        /// </summary>
        public DateTime? StartedAt { get; set; }

        /// <summary>
        /// 完成时间
        /// </summary>
        public DateTime? CompletedAt { get; set; }

        /// <summary>
        /// 已完成任务数
        /// </summary>
        public int CompletedTasks { get; set; }

        /// <summary>
        /// 失败任务数
        /// </summary>
        public int FailedTasks { get; set; }

        /// <summary>
        /// 错误信息（如果失败）
        /// </summary>
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// 任务执行结果实体（用于持久化）
    /// </summary>
    public class TaskExecutionResultEntity
    {
        /// <summary>
        /// 主键
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 任务ID（对应 DecomposedTask.TaskId）
        /// </summary>
        public string TaskId { get; set; } = string.Empty;

        /// <summary>
        /// 执行计划ID
        /// </summary>
        public string PlanId { get; set; } = string.Empty;

        /// <summary>
        /// 是否成功
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// 输出消息
        /// </summary>
        public string? Message { get; set; }

        /// <summary>
        /// 结果数据（JSON序列化）
        /// </summary>
        public string? DataJson { get; set; }

        /// <summary>
        /// 错误信息
        /// </summary>
        public string? Error { get; set; }

        /// <summary>
        /// 开始时间
        /// </summary>
        public DateTime StartedAt { get; set; }

        /// <summary>
        /// 完成时间
        /// </summary>
        public DateTime? CompletedAt { get; set; }

        /// <summary>
        /// 执行时长（毫秒）
        /// </summary>
        public long? DurationMs { get; set; }

        /// <summary>
        /// 重试次数
        /// </summary>
        public int RetryCount { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
