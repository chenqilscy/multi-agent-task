using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Core.Constants;
using CKY.MultiAgentFramework.Core.Enums;
using CKY.MultiAgentFramework.Core.Models.Task;
using Microsoft.Extensions.Logging;

namespace CKY.MultiAgentFramework.Services.Scheduling
{
    /// <summary>
    /// 任务调度器
    /// 负责任务的优先级调度、资源管理和执行控制
    /// </summary>
    public class MafTaskScheduler : ITaskScheduler
    {
        private readonly IPriorityCalculator _priorityCalculator;
        private readonly SemaphoreSlim _concurrencyLimiter;
        private readonly int _maxConcurrentTasks;
        private readonly ILogger<MafTaskScheduler> _logger;

        public MafTaskScheduler(
            IPriorityCalculator priorityCalculator,
            int maxConcurrentTasks = 10,
            ILogger<MafTaskScheduler>? logger = null)
        {
            _priorityCalculator = priorityCalculator ?? throw new ArgumentNullException(nameof(priorityCalculator));
            _maxConcurrentTasks = maxConcurrentTasks;
            _concurrencyLimiter = new SemaphoreSlim(maxConcurrentTasks, maxConcurrentTasks);
            _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<MafTaskScheduler>.Instance;
        }

        /// <inheritdoc />
        public async Task<ScheduleResult> ScheduleAsync(
            List<DecomposedTask> tasks,
            CancellationToken ct = default)
        {
            _logger.LogInformation("Scheduling {Count} tasks with max concurrency {MaxConcurrency}",
                tasks.Count, _maxConcurrentTasks);

            var result = new ScheduleResult();

            // 1. 计算所有任务的优先级分数
            foreach (var task in tasks)
            {
                var request = new PriorityCalculationRequest
                {
                    TaskId = task.TaskId,
                    BasePriority = task.Priority,
                    UserInteraction = UserInteractionType.Automatic,
                    TimeFactor = TimeFactor.Normal,
                    ResourceUsage = ResourceUsage.Low
                };

                task.PriorityScore = _priorityCalculator.CalculatePriority(request);
                _logger.LogDebug("Task {TaskId} ({TaskName}) assigned priority score: {Score}",
                    task.TaskId, task.TaskName, task.PriorityScore);
            }

            // 2. 按优先级排序（分数高的优先）
            var sortedTasks = tasks.OrderByDescending(t => t.PriorityScore).ToList();

            // 3. 分组：高优先级(>50)、中优先级(30-50)、低优先级(<30)
            var highPriority = sortedTasks.Where(t => t.PriorityScore > PersistenceConstants.PriorityThresholds.HighPriorityThreshold).ToList();
            var mediumPriority = sortedTasks.Where(t =>
                t.PriorityScore >= PersistenceConstants.PriorityThresholds.MediumPriorityThreshold &&
                t.PriorityScore <= PersistenceConstants.PriorityThresholds.HighPriorityThreshold).ToList();
            var lowPriority = sortedTasks.Where(t => t.PriorityScore < PersistenceConstants.PriorityThresholds.MediumPriorityThreshold).ToList();

            // 4. 生成执行计划
            result.ScheduledTasks = sortedTasks.Select(t => t.TaskId).ToList();
            result.ExecutionPlan = new ScheduleExecutionPlan
            {
                PlanId = Guid.NewGuid().ToString(),
                HighPriorityTasks = highPriority,
                MediumPriorityTasks = mediumPriority,
                LowPriorityTasks = lowPriority
            };

            _logger.LogInformation("Scheduled {High} high, {Medium} medium, {Low} low priority tasks",
                highPriority.Count, mediumPriority.Count, lowPriority.Count);

            return await Task.FromResult(result);
        }

        /// <inheritdoc />
        public async Task<TaskExecutionResult> ExecuteTaskAsync(
            DecomposedTask task,
            Func<DecomposedTask, CancellationToken, Task<TaskExecutionResult>> executor,
            CancellationToken ct = default)
        {
            await _concurrencyLimiter.WaitAsync(ct);

            try
            {
                _logger.LogInformation("Executing task {TaskId} ({TaskName}) with priority {Score}",
                    task.TaskId, task.TaskName, task.PriorityScore);

                task.Status = MafTaskStatus.Running;
                task.StartedAt = DateTime.UtcNow;

                var result = await executor(task, ct);

                task.Status = result.Success ? MafTaskStatus.Completed : MafTaskStatus.Failed;
                task.CompletedAt = result.CompletedAt;
                task.Result = result;

                return result;
            }
            finally
            {
                _concurrencyLimiter.Release();
            }
        }
    }

    /// <summary>
    /// 调度结果
    /// </summary>
    public class ScheduleResult
    {
        /// <summary>已调度的任务ID列表</summary>
        public List<string> ScheduledTasks { get; set; } = new();

        /// <summary>执行计划</summary>
        public ScheduleExecutionPlan ExecutionPlan { get; set; } = new();
    }

    /// <summary>
    /// 调度执行计划
    /// </summary>
    public class ScheduleExecutionPlan
    {
        /// <summary>计划ID</summary>
        public string PlanId { get; set; } = string.Empty;

        /// <summary>高优先级任务</summary>
        public List<DecomposedTask> HighPriorityTasks { get; set; } = new();

        /// <summary>中优先级任务</summary>
        public List<DecomposedTask> MediumPriorityTasks { get; set; } = new();

        /// <summary>低优先级任务</summary>
        public List<DecomposedTask> LowPriorityTasks { get; set; } = new();
    }
}
