using BenchmarkDotNet.Attributes;
using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Core.Enums;
using CKY.MultiAgentFramework.Core.Models.Task;
using CKY.MultiAgentFramework.Services.Scheduling;
using Microsoft.Extensions.Logging.Abstractions;

namespace CKY.MAF.Benchmarks;

/// <summary>
/// 任务调度器性能基准测试
/// </summary>
[MemoryDiagnoser]
[RankColumn]
public class TaskSchedulerBenchmarks
{
    private MafTaskScheduler _scheduler = null!;

    [Params(10, 50, 200)]
    public int TaskCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var calculator = new MafPriorityCalculator(NullLogger<MafPriorityCalculator>.Instance);
        _scheduler = new MafTaskScheduler(calculator, 10, NullLogger<MafTaskScheduler>.Instance);
    }

    [Benchmark(Description = "调度N个独立任务")]
    public async Task<ScheduleResult> ScheduleIndependentTasks()
    {
        var tasks = Enumerable.Range(0, TaskCount).Select(i => new DecomposedTask
        {
            TaskId = $"task-{i}",
            TaskName = $"任务{i}",
            Priority = (TaskPriority)(i % 5)
        }).ToList();

        return await _scheduler.ScheduleAsync(tasks);
    }

    [Benchmark(Description = "调度N个含依赖的任务")]
    public async Task<ScheduleResult> ScheduleDependentTasks()
    {
        var tasks = new List<DecomposedTask>();
        for (int i = 0; i < TaskCount; i++)
        {
            var task = new DecomposedTask
            {
                TaskId = $"task-{i}",
                TaskName = $"任务{i}",
                Priority = (TaskPriority)(i % 5)
            };
            if (i > 0)
            {
                task.Dependencies.Add(new TaskDependency
                {
                    DependsOnTaskId = $"task-{i - 1}",
                    Type = DependencyType.MustSucceed
                });
            }
            tasks.Add(task);
        }

        return await _scheduler.ScheduleAsync(tasks);
    }

    [Benchmark(Description = "执行单任务（含并发控制）")]
    public async Task<TaskExecutionResult> ExecuteSingleTask()
    {
        var task = new DecomposedTask
        {
            TaskId = "exec-task",
            TaskName = "执行测试任务",
            Priority = TaskPriority.Normal
        };

        return await _scheduler.ExecuteTaskAsync(task, (t, ct) =>
        {
            return Task.FromResult(new TaskExecutionResult
            {
                TaskId = t.TaskId,
                Success = true,
                Message = "完成",
                StartedAt = DateTime.UtcNow,
                CompletedAt = DateTime.UtcNow
            });
        });
    }
}
