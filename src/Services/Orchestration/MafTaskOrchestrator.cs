using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Core.Diagnostics;
using CKY.MultiAgentFramework.Core.Enums;
using CKY.MultiAgentFramework.Core.Models.Task;
using CKY.MultiAgentFramework.Services.Monitoring;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace CKY.MultiAgentFramework.Services.Orchestration
{
    /// <summary>
    /// 任务编排服务
    /// 根据任务依赖关系生成执行计划并执行
    /// </summary>
    public class MafTaskOrchestrator : ITaskOrchestrator
    {
        private readonly ILogger<MafTaskOrchestrator> _logger;
        private readonly IPrometheusMetricsCollector? _metrics;
        private readonly Dictionary<string, CancellationTokenSource> _activePlans = new();

        public MafTaskOrchestrator(
            ILogger<MafTaskOrchestrator> logger,
            IPrometheusMetricsCollector? metrics = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _metrics = metrics;
        }

        /// <inheritdoc />
        public Task<ExecutionPlan> CreatePlanAsync(
            List<DecomposedTask> tasks,
            CancellationToken ct = default)
        {
            using var activity = MafActivitySource.Task.StartActivity("task.create_plan");
            activity?.SetTag("task.count", tasks.Count);

            _logger.LogInformation("Creating execution plan for {Count} tasks", tasks.Count);

            // 记录计划创建指标
            _metrics?.IncrementCounter(MafMetrics.TaskCreatedTotal, tasks.Count);

            var plan = new ExecutionPlan();

            // 拓扑排序并按依赖关系分组
            var groups = GroupByDependencies(tasks);

            foreach (var group in groups)
            {
                if (group.Count == 1)
                {
                    // 单任务组 - 串行
                    plan.SerialGroups.Add(new TaskGroup
                    {
                        Mode = GroupExecutionMode.Serial,
                        Tasks = group
                    });
                }
                else
                {
                    // 多任务组 - 并行
                    plan.ParallelGroups.Add(new TaskGroup
                    {
                        Mode = GroupExecutionMode.Parallel,
                        Tasks = group
                    });
                }
            }

            return Task.FromResult(plan);
        }

        /// <inheritdoc />
        public async Task<List<TaskExecutionResult>> ExecutePlanAsync(
            ExecutionPlan plan,
            CancellationToken ct = default)
        {
            using var activity = MafActivitySource.Task.StartActivity("task.execute_plan");
            activity?.SetTag("plan.id", plan.PlanId);
            activity?.SetTag("plan.parallel_groups", plan.ParallelGroups.Count);
            activity?.SetTag("plan.serial_groups", plan.SerialGroups.Count);

            _logger.LogInformation("Executing plan {PlanId}", plan.PlanId);

            var stopwatch = Stopwatch.StartNew();
            var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            _activePlans[plan.PlanId] = cts;

            var results = new List<TaskExecutionResult>();

            try
            {
                // 先执行并行组
                foreach (var group in plan.ParallelGroups)
                {
                    if (cts.Token.IsCancellationRequested) break;

                    group.Status = GroupStatus.Running;
                    group.StartTime = DateTime.UtcNow;

                    var groupResults = await ExecuteGroupAsync(group, cts.Token);
                    results.AddRange(groupResults);

                    group.Status = groupResults.All(r => r.Success) ? GroupStatus.Completed : GroupStatus.Failed;
                    group.EndTime = DateTime.UtcNow;
                }

                // 再执行串行组
                foreach (var group in plan.SerialGroups)
                {
                    if (cts.Token.IsCancellationRequested) break;

                    group.Status = GroupStatus.Running;
                    group.StartTime = DateTime.UtcNow;

                    var groupResults = await ExecuteGroupAsync(group, cts.Token);
                    results.AddRange(groupResults);

                    group.Status = groupResults.All(r => r.Success) ? GroupStatus.Completed : GroupStatus.Failed;
                    group.EndTime = DateTime.UtcNow;
                }
            }
            finally
            {
                _activePlans.Remove(plan.PlanId);
                cts.Dispose();
            }

            activity?.SetTag("plan.total_results", results.Count);
            activity?.SetTag("plan.success_count", results.Count(r => r.Success));
            activity?.SetTag("plan.failure_count", results.Count(r => !r.Success));

            // 记录计划执行指标
            stopwatch.Stop();
            _metrics?.RecordHistogram(MafMetrics.TaskDuration, stopwatch.Elapsed.TotalSeconds);
            _metrics?.IncrementCounter(MafMetrics.TaskCompletedTotal, results.Count(r => r.Success));
            _metrics?.IncrementCounter(MafMetrics.TaskFailedTotal, results.Count(r => !r.Success));

            return results;
        }

        /// <inheritdoc />
        public Task CancelAsync(string planId, CancellationToken ct = default)
        {
            if (_activePlans.TryGetValue(planId, out var cts))
            {
                _logger.LogInformation("Cancelling plan {PlanId}", planId);
                cts.Cancel();
            }
            return Task.CompletedTask;
        }

        private async Task<List<TaskExecutionResult>> ExecuteGroupAsync(
            TaskGroup group,
            CancellationToken ct)
        {
            if (group.Mode == GroupExecutionMode.Parallel)
            {
                var tasks = group.Tasks.Select(t => ExecuteTaskAsync(t, ct));
                return (await Task.WhenAll(tasks)).ToList();
            }
            else
            {
                var results = new List<TaskExecutionResult>();
                foreach (var task in group.Tasks)
                {
                    var result = await ExecuteTaskAsync(task, ct);
                    results.Add(result);

                    if (!result.Success && !group.Tasks.Last().Equals(task))
                    {
                        _logger.LogWarning("Task {TaskId} failed, stopping serial group", task.TaskId);
                        break;
                    }
                }
                return results;
            }
        }

        private Task<TaskExecutionResult> ExecuteTaskAsync(DecomposedTask task, CancellationToken ct)
        {
            // 此处为骨架实现，实际执行由具体的Agent完成
            // 在完整实现中，应通过IAgentMatcher找到Agent并调用其ExecuteAsync
            _logger.LogDebug("Placeholder execution for task {TaskId}", task.TaskId);

            task.Status = MafTaskStatus.Running;
            task.StartedAt = DateTime.UtcNow;

            var result = new TaskExecutionResult
            {
                TaskId = task.TaskId,
                Success = true,
                Message = $"任务 '{task.TaskName}' 已调度执行",
                StartedAt = task.StartedAt.Value,
                CompletedAt = DateTime.UtcNow
            };
            result.Duration = result.CompletedAt - result.StartedAt;

            task.Status = MafTaskStatus.Completed;
            task.CompletedAt = result.CompletedAt;
            task.Result = result;

            return Task.FromResult(result);
        }

        private List<List<DecomposedTask>> GroupByDependencies(List<DecomposedTask> tasks)
        {
            var groups = new List<List<DecomposedTask>>();
            var processed = new HashSet<string>();

            // 找出没有依赖的任务（或依赖已满足的任务）
            var independentTasks = tasks
                .Where(t => !t.Dependencies.Any())
                .ToList();

            if (independentTasks.Any())
            {
                groups.Add(independentTasks);
                foreach (var t in independentTasks) processed.Add(t.TaskId);
            }

            // 找出依赖已处理的任务
            var remaining = tasks.Where(t => !processed.Contains(t.TaskId)).ToList();
            while (remaining.Any())
            {
                var readyTasks = remaining
                    .Where(t => t.Dependencies.All(d => processed.Contains(d.DependsOnTaskId)))
                    .ToList();

                if (!readyTasks.Any()) break;

                groups.Add(readyTasks);
                foreach (var t in readyTasks) processed.Add(t.TaskId);
                remaining = tasks.Where(t => !processed.Contains(t.TaskId)).ToList();
            }

            return groups;
        }
    }
}
