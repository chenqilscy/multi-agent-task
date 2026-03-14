using CKY.MultiAgentFramework.Core.Enums;
using CKY.MultiAgentFramework.Core.Models.Task;

namespace CKY.MultiAgentFramework.Core.Abstractions
{
    /// <summary>
    /// 优先级计算请求
    /// </summary>
    public class PriorityCalculationRequest
    {
        /// <summary>任务ID</summary>
        public string? TaskId { get; set; }

        /// <summary>基础优先级</summary>
        public TaskPriority BasePriority { get; set; } = TaskPriority.Normal;

        /// <summary>用户交互类型</summary>
        public UserInteractionType UserInteraction { get; set; } = UserInteractionType.Automatic;

        /// <summary>时间因素</summary>
        public TimeFactor TimeFactor { get; set; } = TimeFactor.Normal;

        /// <summary>资源使用率</summary>
        public ResourceUsage ResourceUsage { get; set; } = ResourceUsage.Low;

        /// <summary>依赖的任务</summary>
        public DecomposedTask? DependencyTask { get; set; }

        /// <summary>是否超期</summary>
        public bool IsOverdue { get; set; }
    }

    /// <summary>
    /// 优先级计算器接口
    /// </summary>
    public interface IPriorityCalculator
    {
        /// <summary>
        /// 计算任务优先级分数 (0-100)
        /// </summary>
        int CalculatePriority(PriorityCalculationRequest request);
    }
}
