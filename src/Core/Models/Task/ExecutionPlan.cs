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
}
