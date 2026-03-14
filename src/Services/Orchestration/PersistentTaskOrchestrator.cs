using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Core.Constants;
using CKY.MultiAgentFramework.Core.Enums;
using CKY.MultiAgentFramework.Core.Models.Persisted;
using CKY.MultiAgentFramework.Core.Models.Task;
using CKY.MultiAgentFramework.Services.Serialization;
using Microsoft.Extensions.Logging;

namespace CKY.MultiAgentFramework.Services.Orchestration
{
    /// <summary>
    /// 持久化任务编排器
    /// 扩展 MafTaskOrchestrator，添加执行计划的持久化能力
    /// </summary>
    public class PersistentTaskOrchestrator : ITaskOrchestrator
    {
        private readonly ITaskOrchestrator _innerOrchestrator;
        private readonly IExecutionPlanRepository _planRepository;
        private readonly ITaskExecutionResultRepository _resultRepository;
        private readonly ILogger<PersistentTaskOrchestrator> _logger;

        public PersistentTaskOrchestrator(
            ITaskOrchestrator innerOrchestrator,
            IExecutionPlanRepository planRepository,
            ITaskExecutionResultRepository resultRepository,
            ILogger<PersistentTaskOrchestrator> logger)
        {
            _innerOrchestrator = innerOrchestrator ?? throw new ArgumentNullException(nameof(innerOrchestrator));
            _planRepository = planRepository ?? throw new ArgumentNullException(nameof(planRepository));
            _resultRepository = resultRepository ?? throw new ArgumentNullException(nameof(resultRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public async Task<ExecutionPlan> CreatePlanAsync(
            List<DecomposedTask> tasks,
            CancellationToken ct = default)
        {
            _logger.LogInformation("Creating execution plan for {Count} tasks with persistence", tasks.Count);

            // 调用内部编排器创建执行计划
            var plan = await _innerOrchestrator.CreatePlanAsync(tasks, ct);

            // 持久化执行计划
            await SaveExecutionPlanAsync(plan, ct);

            _logger.LogInformation("Execution plan saved with ID {PlanId}", plan.PlanId);

            return plan;
        }

        /// <inheritdoc />
        public async Task<List<TaskExecutionResult>> ExecutePlanAsync(
            ExecutionPlan plan,
            CancellationToken ct = default)
        {
            _logger.LogInformation("Executing plan {PlanId} with persistence tracking", plan.PlanId);

            // 更新计划状态为运行中
            await UpdatePlanStatusAsync(plan.PlanId, ExecutionPlanStatus.Running, ct: ct);

            try
            {
                // 调用内部编排器执行计划
                var results = await _innerOrchestrator.ExecutePlanAsync(plan, ct);

                // 持久化所有执行结果
                await SaveExecutionResultsAsync(plan.PlanId, results, ct);

                // 更新计划状态
                var allSuccess = results.All(r => r.Success);
                var finalStatus = allSuccess ? ExecutionPlanStatus.Completed : ExecutionPlanStatus.PartiallyCompleted;

                await UpdatePlanStatusAsync(
                    plan.PlanId,
                    finalStatus,
                    completedTasks: results.Count(r => r.Success),
                    failedTasks: results.Count(r => !r.Success),
                    ct: ct);

                _logger.LogInformation("Plan {PlanId} execution completed. Status: {Status}", plan.PlanId, finalStatus);

                return results;
            }
            catch (OperationCanceledException)
            {
                // 任务被取消，不记录为错误
                await UpdatePlanStatusAsync(
                    plan.PlanId,
                    ExecutionPlanStatus.Cancelled,
                    errorMessage: "Operation was cancelled",
                    ct: ct);

                _logger.LogWarning("Plan {PlanId} execution was cancelled", plan.PlanId);
                throw;
            }
            catch (InvalidOperationException ex)
            {
                // JSON 反序列化或其他无效操作错误
                await UpdatePlanStatusAsync(
                    plan.PlanId,
                    ExecutionPlanStatus.Failed,
                    errorMessage: $"Invalid operation: {ex.Message}",
                    ct: ct);

                _logger.LogError(ex, "Plan {PlanId} execution failed due to invalid operation", plan.PlanId);
                throw;
            }
            catch (Exception ex)
            {
                // 其他未预期的错误
                await UpdatePlanStatusAsync(
                    plan.PlanId,
                    ExecutionPlanStatus.Failed,
                    errorMessage: $"Unexpected error: {ex.Message}",
                    ct: ct);

                _logger.LogError(ex, "Plan {PlanId} execution failed with unexpected error", plan.PlanId);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task CancelAsync(string planId, CancellationToken ct = default)
        {
            _logger.LogInformation("Cancelling plan {PlanId}", planId);

            // 调用内部编排器取消执行
            await _innerOrchestrator.CancelAsync(planId, ct);

            // 更新计划状态为已取消
            await UpdatePlanStatusAsync(planId, ExecutionPlanStatus.Cancelled, ct: ct);
        }

        /// <summary>
        /// 从持久化存储恢复执行计划
        /// </summary>
        public async Task<ExecutionPlan?> RestorePlanAsync(
            string planId,
            CancellationToken ct = default)
        {
            _logger.LogInformation("Restoring execution plan {PlanId}", planId);

            var planEntity = await _planRepository.GetByPlanIdAsync(planId, ct);
            if (planEntity == null)
            {
                _logger.LogWarning("Execution plan {PlanId} not found", planId);
                return null;
            }

            try
            {
                var plan = JsonSerializerHelper.Deserialize<ExecutionPlan>(
                    planEntity.PlanJson,
                    context: $"ExecutionPlan:{planId}",
                    logger: _logger);

                if (plan != null)
                {
                    _logger.LogInformation("Successfully restored execution plan {PlanId}", planId);
                    return plan;
                }
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Failed to deserialize execution plan {PlanId}", planId);
            }

            return null;
        }

        /// <summary>
        /// 获取执行计划状态
        /// </summary>
        public async Task<ExecutionPlanEntity?> GetPlanStatusAsync(
            string planId,
            CancellationToken ct = default)
        {
            return await _planRepository.GetByPlanIdAsync(planId, ct);
        }

        /// <summary>
        /// 获取执行结果
        /// </summary>
        public async Task<List<TaskExecutionResultEntity>> GetExecutionResultsAsync(
            string planId,
            CancellationToken ct = default)
        {
            return await _resultRepository.GetByPlanIdAsync(planId, ct);
        }

        /// <summary>
        /// 保存执行计划到持久化存储
        /// </summary>
        private async Task SaveExecutionPlanAsync(
            ExecutionPlan plan,
            CancellationToken ct)
        {
            var planJson = JsonSerializerHelper.Serialize(plan, logger: _logger);

            var planEntity = new ExecutionPlanEntity
            {
                PlanId = plan.PlanId,
                PlanJson = planJson,
                Status = ExecutionPlanStatus.Created,
                SerialGroupCount = plan.SerialGroups.Count,
                ParallelGroupCount = plan.ParallelGroups.Count,
                TotalTasks = plan.SerialGroups.Sum(g => g.Tasks.Count) +
                            plan.ParallelGroups.Sum(g => g.Tasks.Count),
                AllowPartialExecution = plan.AllowPartialExecution,
                CreatedAt = plan.CreatedAt
            };

            await _planRepository.AddAsync(planEntity, ct);
        }

        /// <summary>
        /// 保存执行结果到持久化存储
        /// </summary>
        private async Task SaveExecutionResultsAsync(
            string planId,
            List<TaskExecutionResult> results,
            CancellationToken ct)
        {
            var resultEntities = results.Select(r => new TaskExecutionResultEntity
            {
                TaskId = r.TaskId,
                PlanId = planId,
                Success = r.Success,
                Message = r.Message,
                DataJson = r.Data != null ? JsonSerializerHelper.Serialize(r.Data, _logger) : null,
                Error = r.Error,
                StartedAt = r.StartedAt,
                CompletedAt = r.CompletedAt,
                DurationMs = r.Duration.HasValue ? (long?)r.Duration.Value.TotalMilliseconds : null,
                RetryCount = r.RetryCount,
                CreatedAt = DateTime.UtcNow
            }).ToList();

            await _resultRepository.AddRangeAsync(resultEntities, ct);
            _logger.LogInformation("Saved {Count} execution results for plan {PlanId}", resultEntities.Count, planId);
        }

        /// <summary>
        /// 更新执行计划状态
        /// </summary>
        private async Task UpdatePlanStatusAsync(
            string planId,
            ExecutionPlanStatus status,
            string? errorMessage = null,
            int? completedTasks = null,
            int? failedTasks = null,
            CancellationToken ct = default)
        {
            var plan = await _planRepository.GetByPlanIdAsync(planId, ct);
            if (plan != null)
            {
                plan.Status = status;
                plan.ErrorMessage = errorMessage;

                if (status == ExecutionPlanStatus.Running)
                {
                    plan.StartedAt = DateTime.UtcNow;
                }
                else if (status == ExecutionPlanStatus.Completed ||
                         status == ExecutionPlanStatus.PartiallyCompleted ||
                         status == ExecutionPlanStatus.Failed ||
                         status == ExecutionPlanStatus.Cancelled)
                {
                    plan.CompletedAt = DateTime.UtcNow;
                    if (completedTasks.HasValue) plan.CompletedTasks = completedTasks.Value;
                    if (failedTasks.HasValue) plan.FailedTasks = failedTasks.Value;
                }

                await _planRepository.UpdateAsync(plan, ct);
                _logger.LogDebug("Updated plan {PlanId} status to {Status}", planId, status);
            }
        }

        /// <summary>
        /// 获取最近的执行计划
        /// </summary>
        public async Task<List<ExecutionPlanEntity>> GetRecentPlansAsync(
            ExecutionPlanStatus? status = null,
            int count = PersistenceConstants.Defaults.DefaultFetchCount,
            CancellationToken ct = default)
        {
            if (status.HasValue)
            {
                return await _planRepository.GetByStatusAsync(status.Value, ct);
            }

            // 如果没有指定状态，使用优化的查询获取多种状态的计划
            var statuses = new List<ExecutionPlanStatus>
            {
                ExecutionPlanStatus.Completed,
                ExecutionPlanStatus.Running,
                ExecutionPlanStatus.Failed
            };

            return await _planRepository.GetByMultipleStatusAsync(statuses, count, ct);
        }
    }
}
