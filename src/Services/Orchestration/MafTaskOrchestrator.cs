using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Core.Enums;
using CKY.MultiAgentFramework.Core.Models.Task;
using Microsoft.Extensions.Logging;

namespace CKY.MultiAgentFramework.Services.Orchestration
{
    /// <summary>
    /// 任务编排服务
    /// 根据任务依赖关系生成执行计划并执行
    /// </summary>
    public class MafTaskOrchestrator : ITaskOrchestrator
    {
        private readonly ILogger<MafTaskOrchestrator> _logger;
        private readonly Dictionary<string, CancellationTokenSource> _activePlans = new();

        public MafTaskOrchestrator(ILogger<MafTaskOrchestrator> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public Task<ExecutionPlan> CreatePlanAsync(
            List<DecomposedTask> tasks,
            CancellationToken ct = default)
        {
            _logger.LogInformation("Creating execution plan for {Count} tasks", tasks.Count);

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
            _logger.LogInformation("Executing plan {PlanId}", plan.PlanId);

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
