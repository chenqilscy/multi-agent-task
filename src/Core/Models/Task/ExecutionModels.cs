using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Core.Enums;

namespace CKY.MultiAgentFramework.Core.Models.Task
{
    /// <summary>
    /// 执行计划
    /// </summary>
    public class ExecutionPlan
    {
        /// <summary>执行计划ID</summary>
        public string PlanId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>创建时间</summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>串行任务组列表</summary>
        public List<TaskGroup> SerialGroups { get; set; } = new();

        /// <summary>并行任务组列表</summary>
        public List<TaskGroup> ParallelGroups { get; set; } = new();

        /// <summary>是否允许部分执行</summary>
        public bool AllowPartialExecution { get; set; } = true;
    }

    /// <summary>
    /// 任务组
    /// </summary>
    public class TaskGroup
    {
        /// <summary>任务组ID</summary>
        public string GroupId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>执行模式（串行或并行）</summary>
        public GroupExecutionMode Mode { get; set; }

        /// <summary>任务列表</summary>
        public List<DecomposedTask> Tasks { get; set; } = new();

        /// <summary>任务组状态</summary>
        public GroupStatus Status { get; set; } = GroupStatus.Pending;

        /// <summary>开始时间</summary>
        public DateTime? StartTime { get; set; }

        /// <summary>完成时间</summary>
        public DateTime? EndTime { get; set; }
    }

    /// <summary>
    /// 任务分解结果
    /// </summary>
    public class TaskDecomposition
    {
        /// <summary>分解ID</summary>
        public string DecompositionId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>原始用户输入</summary>
        public string OriginalUserInput { get; set; } = string.Empty;

        /// <summary>意图识别结果</summary>
        public IntentRecognitionResult? Intent { get; set; }

        /// <summary>子任务列表</summary>
        public List<DecomposedTask> SubTasks { get; set; } = new();

        /// <summary>分解元数据</summary>
        public DecompositionMetadata Metadata { get; set; } = new();
    }

    /// <summary>
    /// 分解元数据
    /// </summary>
    public class DecompositionMetadata
    {
        /// <summary>分解时间</summary>
        public DateTime DecomposedAt { get; set; } = DateTime.UtcNow;

        /// <summary>分解用时（毫秒）</summary>
        public long ElapsedMs { get; set; }

        /// <summary>使用的策略</summary>
        public string? Strategy { get; set; }
    }
}
