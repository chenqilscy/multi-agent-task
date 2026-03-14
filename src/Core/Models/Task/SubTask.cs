using CKY.MultiAgentFramework.Core.Enums;

namespace CKY.MultiAgentFramework.Core.Models.Task
{
    /// <summary>
    /// 子任务实体
    /// 用于 EF Core 持久化
    /// </summary>
    public class SubTask
    {
        /// <summary>
        /// 主键
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 主任务ID（外键）
        /// </summary>
        public int MainTaskId { get; set; }

        /// <summary>
        /// 子任务标题
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// 子任务描述
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// 子任务状态
        /// </summary>
        public MafTaskStatus Status { get; set; } = MafTaskStatus.Pending;

        /// <summary>
        /// 执行顺序
        /// </summary>
        public int ExecutionOrder { get; set; }

        /// <summary>
        /// 导航属性：主任务
        /// </summary>
        public MainTask MainTask { get; set; } = null!;
    }
}
