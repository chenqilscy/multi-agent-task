using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Core.Enums;
using CKY.MultiAgentFramework.Core.Models.Task;
using Microsoft.Extensions.Logging;

namespace CKY.MultiAgentFramework.Services.Scheduling
{
    /// <summary>
    /// CKY.MAF 优先级计算器
    /// 实现多维评分系统 (0-100分)
    /// </summary>
    public class MafPriorityCalculator : IPriorityCalculator
    {
        private readonly ILogger<MafPriorityCalculator> _logger;

        public MafPriorityCalculator(ILogger<MafPriorityCalculator> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public int CalculatePriority(PriorityCalculationRequest request)
        {
            int score = 0;

            // 1. 基础优先级 (0-40分)
            score += GetBasePriorityScore(request.BasePriority);

            // 2. 用户交互 (0-30分)
            score += GetUserInteractionScore(request.UserInteraction);

            // 3. 时间因素 (0-15分)
            score += GetTimeFactorScore(request.TimeFactor);

            // 4. 资源利用率惩罚 (0-10分)
            score -= GetResourceUsagePenalty(request.ResourceUsage);

            // 5. 依赖传播 (0-5分)
            if (request.DependencyTask != null)
            {
                var dependencyBonus = (int)(request.DependencyTask.PriorityScore * 0.05);
                score += Math.Min(dependencyBonus, 5);
                _logger.LogDebug("Dependency propagation: added {Bonus} points from task {TaskId}",
                    dependencyBonus, request.DependencyTask.TaskId);
            }

            // 6. 时间衰减奖励 (+15% for overdue tasks)
            if (request.IsOverdue)
            {
                var overdueBonus = (int)(score * 0.15);
                score += overdueBonus;
                _logger.LogDebug("Overdue bonus: added {Bonus} points", overdueBonus);
            }

            // 确保分数在0-100范围内
            score = Math.Clamp(score, 0, 100);

            _logger.LogDebug("Final priority score: {Score} for task {TaskId}",
                score, request.TaskId ?? "unknown");

            return score;
        }

        /// <summary>
        /// 获取基础优先级分数
        /// </summary>
        private static int GetBasePriorityScore(TaskPriority priority)
        {
            return priority switch
            {
                TaskPriority.Critical => 40,
                TaskPriority.High => 30,
                TaskPriority.Normal => 20,
                TaskPriority.Low => 10,
                TaskPriority.Background => 5,
                _ => 20
            };
        }

        /// <summary>
        /// 获取用户交互分数
        /// </summary>
        private static int GetUserInteractionScore(UserInteractionType interaction)
        {
            return interaction switch
            {
                UserInteractionType.Active => 30,
                UserInteractionType.Passive => 15,
                UserInteractionType.Automatic => 5,
                _ => 0
            };
        }

        /// <summary>
        /// 获取时间因素分数
        /// </summary>
        private static int GetTimeFactorScore(TimeFactor timeFactor)
        {
            return timeFactor switch
            {
                TimeFactor.Immediate => 15,
                TimeFactor.Urgent => 12,
                TimeFactor.Normal => 8,
                TimeFactor.Deferred => 3,
                _ => 0
            };
        }

        /// <summary>
        /// 获取资源使用惩罚分数
        /// </summary>
        private static int GetResourceUsagePenalty(ResourceUsage usage)
        {
            return usage switch
            {
                ResourceUsage.High => 10,
                ResourceUsage.Medium => 5,
                ResourceUsage.Low => 1,
                _ => 0
            };
        }
    }
}
