using CKY.MultiAgentFramework.Core.Enums;

namespace CKY.MultiAgentFramework.Core.Models.Task
{
    /// <summary>
    /// 主任务实体
    /// 用于 EF Core 持久化
    /// </summary>
    public class MainTask
    {
        /// <summary>
        /// 主键
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 任务标题
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// 任务描述
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// 任务优先级
        /// </summary>
        public TaskPriority Priority { get; set; } = TaskPriority.Normal;

        /// <summary>
        /// 任务状态
        /// </summary>
        public MafTaskStatus Status { get; set; } = MafTaskStatus.Pending;

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 更新时间
        /// </summary>
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// 导航属性：子任务列表
        /// </summary>
        public ICollection<SubTask> SubTasks { get; set; } = new List<SubTask>();
    }
}
