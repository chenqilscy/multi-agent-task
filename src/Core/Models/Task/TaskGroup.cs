using CKY.MultiAgentFramework.Core.Enums;

namespace CKY.MultiAgentFramework.Core.Models.Task
{
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
}
