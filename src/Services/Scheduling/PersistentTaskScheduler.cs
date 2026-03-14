using System.Text.Json;
using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Core.Constants;
using CKY.MultiAgentFramework.Core.Enums;
using CKY.MultiAgentFramework.Core.Models.Persisted;
using CKY.MultiAgentFramework.Core.Models.Task;
using CKY.MultiAgentFramework.Services.Serialization;
using Microsoft.Extensions.Logging;

namespace CKY.MultiAgentFramework.Services.Scheduling
{
    /// <summary>
    /// 持久化任务调度器
    /// 扩展 MafTaskScheduler，添加调度计划的持久化能力
    /// </summary>
    public class PersistentTaskScheduler : ITaskScheduler
    {
        private readonly ITaskScheduler _innerScheduler;
        private readonly ISchedulePlanRepository _planRepository;
        private readonly ILogger<PersistentTaskScheduler> _logger;

        public PersistentTaskScheduler(
            ITaskScheduler innerScheduler,
            ISchedulePlanRepository planRepository,
            ILogger<PersistentTaskScheduler> logger)
        {
            _innerScheduler = innerScheduler ?? throw new ArgumentNullException(nameof(innerScheduler));
            _planRepository = planRepository ?? throw new ArgumentNullException(nameof(planRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public async Task<ScheduleResult> ScheduleAsync(
            List<DecomposedTask> tasks,
            CancellationToken ct = default)
        {
            _logger.LogInformation("Scheduling {Count} tasks with persistence", tasks.Count);

            // 1. 调用内部调度器生成调度计划
            var scheduleResult = await _innerScheduler.ScheduleAsync(tasks, ct);

            // 2. 持久化调度计划
            var planEntity = await SaveSchedulePlanAsync(scheduleResult, ct);

            _logger.LogInformation("Schedule plan saved with ID {PlanId}", planEntity.PlanId);

            return scheduleResult;
        }

        /// <inheritdoc />
        public async Task<TaskExecutionResult> ExecuteTaskAsync(
            DecomposedTask task,
            Func<DecomposedTask, CancellationToken, Task<TaskExecutionResult>> executor,
            CancellationToken ct = default)
        {
            _logger.LogInformation("Executing task {TaskId} with persistence tracking", task.TaskId);

            // 调用内部调度器执行任务
            var result = await _innerScheduler.ExecuteTaskAsync(task, executor, ct);

            // 这里可以选择是否持久化执行结果
            // 通常执行结果由 TaskOrchestrator 统一管理

            return result;
        }

        /// <summary>
        /// 从持久化存储恢复调度计划
        /// </summary>
        public async Task<ScheduleResult?> RestoreScheduleAsync(
            string planId,
            CancellationToken ct = default)
        {
            _logger.LogInformation("Restoring schedule plan {PlanId}", planId);

            var planEntity = await _planRepository.GetByPlanIdAsync(planId, ct);
            if (planEntity == null)
            {
                _logger.LogWarning("Schedule plan {PlanId} not found", planId);
                return null;
            }

            try
            {
                var scheduleResult = JsonSerializerHelper.Deserialize<ScheduleResult>(
                    planEntity.PlanJson,
                    context: $"SchedulePlan:{planId}",
                    logger: _logger);

                if (scheduleResult != null)
                {
                    _logger.LogInformation("Successfully restored schedule plan {PlanId}", planId);
                    return scheduleResult;
                }
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Failed to deserialize schedule plan {PlanId}", planId);
            }

            return null;
        }

        /// <summary>
        /// 获取调度计划状态
        /// </summary>
        public async Task<SchedulePlanEntity?> GetPlanStatusAsync(
            string planId,
            CancellationToken ct = default)
        {
            return await _planRepository.GetByPlanIdAsync(planId, ct);
        }

        /// <summary>
        /// 更新调度计划状态
        /// </summary>
        public async Task UpdatePlanStatusAsync(
            string planId,
            SchedulePlanStatus status,
            string? errorMessage = null,
            CancellationToken ct = default)
        {
            var plan = await _planRepository.GetByPlanIdAsync(planId, ct);
            if (plan != null)
            {
                plan.Status = status;
                plan.ErrorMessage = errorMessage;

                if (status == SchedulePlanStatus.Running)
                {
                    plan.StartedAt = DateTime.UtcNow;
                }
                else if (status == SchedulePlanStatus.Completed ||
                         status == SchedulePlanStatus.Failed ||
                         status == SchedulePlanStatus.Cancelled)
                {
                    plan.CompletedAt = DateTime.UtcNow;
                }

                await _planRepository.UpdateAsync(plan, ct);
                _logger.LogInformation("Updated plan {PlanId} status to {Status}", planId, status);
            }
        }

        /// <summary>
        /// 保存调度计划到持久化存储
        /// </summary>
        private async Task<SchedulePlanEntity> SaveSchedulePlanAsync(
            ScheduleResult scheduleResult,
            CancellationToken ct)
        {
            var planJson = JsonSerializer.Serialize(scheduleResult, new JsonSerializerOptions
            {
                WriteIndented = false
            });

            var planEntity = new SchedulePlanEntity
            {
                PlanId = scheduleResult.ExecutionPlan.PlanId,
                PlanJson = planJson,
                Status = SchedulePlanStatus.Created,
                TotalTasks = scheduleResult.ScheduledTasks.Count,
                HighPriorityCount = scheduleResult.ExecutionPlan.HighPriorityTasks.Count,
                MediumPriorityCount = scheduleResult.ExecutionPlan.MediumPriorityTasks.Count,
                LowPriorityCount = scheduleResult.ExecutionPlan.LowPriorityTasks.Count,
                CreatedAt = DateTime.UtcNow
            };

            return await _planRepository.AddAsync(planEntity, ct);
        }

        /// <summary>
        /// 获取最近的调度计划
        /// </summary>
        public async Task<List<SchedulePlanEntity>> GetRecentPlansAsync(
            int count = PersistenceConstants.Defaults.DefaultFetchCount,
            CancellationToken ct = default)
        {
            return await _planRepository.GetRecentPlansAsync(count, ct);
        }

        /// <summary>
        /// 根据状态获取调度计划
        /// </summary>
        public async Task<List<SchedulePlanEntity>> GetPlansByStatusAsync(
            SchedulePlanStatus status,
            CancellationToken ct = default)
        {
            return await _planRepository.GetByStatusAsync(status, ct);
        }
    }
}
