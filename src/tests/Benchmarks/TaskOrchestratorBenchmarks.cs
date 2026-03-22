using BenchmarkDotNet.Attributes;
using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Core.Enums;
using CKY.MultiAgentFramework.Core.Models.Task;
using CKY.MultiAgentFramework.Services.Orchestration;
using Microsoft.Extensions.Logging.Abstractions;

namespace CKY.MAF.Benchmarks;

/// <summary>
/// 任务编排器性能基准测试
/// </summary>
[MemoryDiagnoser]
[RankColumn]
public class TaskOrchestratorBenchmarks
{
    private MafTaskOrchestrator _orchestrator = null!;

    [Params(5, 20, 100)]
    public int TaskCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _orchestrator = new MafTaskOrchestrator(NullLogger<MafTaskOrchestrator>.Instance);
    }

    [Benchmark(Description = "创建执行计划（独立任务）")]
    public async Task<ExecutionPlan> CreatePlanIndependent()
    {
        var tasks = Enumerable.Range(0, TaskCount).Select(i => new DecomposedTask
        {
            TaskId = $"task-{i}",
            TaskName = $"独立任务{i}"
        }).ToList();

        return await _orchestrator.CreatePlanAsync(tasks);
    }

    [Benchmark(Description = "创建执行计划（链式依赖）")]
    public async Task<ExecutionPlan> CreatePlanChainDependency()
    {
        var tasks = new List<DecomposedTask>();
        for (int i = 0; i < TaskCount; i++)
        {
            var task = new DecomposedTask
            {
                TaskId = $"task-{i}",
                TaskName = $"链式任务{i}"
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

        return await _orchestrator.CreatePlanAsync(tasks);
    }

    [Benchmark(Description = "创建执行计划（树形依赖）")]
    public async Task<ExecutionPlan> CreatePlanTreeDependency()
    {
        var tasks = new List<DecomposedTask>();
        for (int i = 0; i < TaskCount; i++)
        {
            var task = new DecomposedTask
            {
                TaskId = $"task-{i}",
                TaskName = $"树形任务{i}"
            };
            // 每个任务依赖其"父节点"（二叉树结构）
            if (i > 0)
            {
                task.Dependencies.Add(new TaskDependency
                {
                    DependsOnTaskId = $"task-{(i - 1) / 2}",
                    Type = DependencyType.MustSucceed
                });
            }
            tasks.Add(task);
        }

        return await _orchestrator.CreatePlanAsync(tasks);
    }

    [Benchmark(Description = "执行计划（全并行）")]
    public async Task<List<TaskExecutionResult>> ExecutePlanParallel()
    {
        var tasks = Enumerable.Range(0, TaskCount).Select(i => new DecomposedTask
        {
            TaskId = $"task-{i}",
            TaskName = $"并行任务{i}"
        }).ToList();

        var plan = await _orchestrator.CreatePlanAsync(tasks);
        return await _orchestrator.ExecutePlanAsync(plan);
    }
}
