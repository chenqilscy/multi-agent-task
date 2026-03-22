using CKY.MultiAgentFramework.Core.Enums;
using CKY.MultiAgentFramework.Core.Models.Task;
using CKY.MultiAgentFramework.Services.Orchestration;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace CKY.MAF.Tests.Services.Orchestration;

/// <summary>
/// MafTaskOrchestrator 深入测试 — 依赖分组、Cancel、串行/并行执行
/// </summary>
public class MafTaskOrchestratorExtendedTests
{
    private readonly Mock<ILogger<MafTaskOrchestrator>> _loggerMock = new();

    private MafTaskOrchestrator CreateSut() => new(_loggerMock.Object);

    [Fact]
    public async Task CreatePlan_EmptyTasks_ReturnsEmptyPlan()
    {
        var sut = CreateSut();
        var plan = await sut.CreatePlanAsync(new List<DecomposedTask>());
        plan.Should().NotBeNull();
        plan.SerialGroups.Should().BeEmpty();
        plan.ParallelGroups.Should().BeEmpty();
    }

    [Fact]
    public async Task CreatePlan_SingleTask_CreatesSerialGroup()
    {
        var sut = CreateSut();
        var tasks = new List<DecomposedTask>
        {
            new() { TaskId = "t1", TaskName = "任务1" }
        };

        var plan = await sut.CreatePlanAsync(tasks);
        // 单任务应该在某个组里
        var totalTasks = plan.SerialGroups.SelectMany(g => g.Tasks)
            .Concat(plan.ParallelGroups.SelectMany(g => g.Tasks))
            .ToList();
        totalTasks.Should().HaveCount(1);
    }

    [Fact]
    public async Task CreatePlan_MultipleIndependent_CreatesParallelGroup()
    {
        var sut = CreateSut();
        var tasks = new List<DecomposedTask>
        {
            new() { TaskId = "t1", TaskName = "任务1" },
            new() { TaskId = "t2", TaskName = "任务2" },
            new() { TaskId = "t3", TaskName = "任务3" }
        };

        var plan = await sut.CreatePlanAsync(tasks);
        plan.ParallelGroups.Should().NotBeEmpty();
        plan.ParallelGroups.Sum(g => g.Tasks.Count).Should().Be(3);
    }

    [Fact]
    public async Task CreatePlan_DependentTasks_MultipleGroups()
    {
        var sut = CreateSut();
        var tasks = new List<DecomposedTask>
        {
            new() { TaskId = "t1", TaskName = "前置任务" },
            new()
            {
                TaskId = "t2",
                TaskName = "依赖任务",
                Dependencies = new List<TaskDependency>
                {
                    new() { DependsOnTaskId = "t1", Type = DependencyType.MustSucceed }
                }
            }
        };

        var plan = await sut.CreatePlanAsync(tasks);
        var allGroups = plan.SerialGroups.Count + plan.ParallelGroups.Count;
        allGroups.Should().BeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task ExecutePlan_SingleSerialTask_ReturnsSuccessResult()
    {
        var sut = CreateSut();
        var tasks = new List<DecomposedTask>
        {
            new() { TaskId = "t1", TaskName = "测试任务" }
        };

        var plan = await sut.CreatePlanAsync(tasks);
        var results = await sut.ExecutePlanAsync(plan);

        results.Should().HaveCount(1);
        results[0].Success.Should().BeTrue();
        results[0].TaskId.Should().Be("t1");
        results[0].Duration.Should().BeGreaterThan(TimeSpan.Zero);
    }

    [Fact]
    public async Task ExecutePlan_ParallelTasks_AllExecuted()
    {
        var sut = CreateSut();
        var tasks = new List<DecomposedTask>
        {
            new() { TaskId = "t1", TaskName = "并行1" },
            new() { TaskId = "t2", TaskName = "并行2" }
        };

        var plan = await sut.CreatePlanAsync(tasks);
        var results = await sut.ExecutePlanAsync(plan);

        results.Should().HaveCount(2);
        results.Should().OnlyContain(r => r.Success);
    }

    [Fact]
    public async Task CancelAsync_UnknownPlanId_DoesNotThrow()
    {
        var sut = CreateSut();
        await sut.Invoking(s => s.CancelAsync("nonexistent"))
            .Should().NotThrowAsync();
    }

    [Fact]
    public async Task ExecutePlan_SetsTaskStatusToCompleted()
    {
        var sut = CreateSut();
        var task = new DecomposedTask { TaskId = "t1", TaskName = "状态测试" };
        var plan = await sut.CreatePlanAsync(new List<DecomposedTask> { task });
        await sut.ExecutePlanAsync(plan);

        task.Status.Should().Be(MafTaskStatus.Completed);
        task.StartedAt.Should().NotBeNull();
        task.CompletedAt.Should().NotBeNull();
        task.Result.Should().NotBeNull();
    }

    [Fact]
    public async Task ExecutePlan_WithMetrics_DoesNotThrow()
    {
        // 没有 metrics 注入不应抛异常
        var sut = new MafTaskOrchestrator(_loggerMock.Object, null);
        var tasks = new List<DecomposedTask> { new() { TaskId = "t1" } };
        var plan = await sut.CreatePlanAsync(tasks);
        var results = await sut.ExecutePlanAsync(plan);
        results.Should().HaveCount(1);
    }
}
