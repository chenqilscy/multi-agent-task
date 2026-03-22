using CKY.MultiAgentFramework.Core.Enums;
using CKY.MultiAgentFramework.Core.Models.Task;
using CKY.MultiAgentFramework.Services.Orchestration;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CKY.MultiAgentFramework.IntegrationTests.Orchestration;

/// <summary>
/// MafTaskOrchestrator 集成测试
/// 验证执行计划创建、任务分组和执行流程
/// </summary>
public class TaskOrchestratorIntegrationTests
{
    private readonly MafTaskOrchestrator _orchestrator;

    public TaskOrchestratorIntegrationTests()
    {
        _orchestrator = new MafTaskOrchestrator(
            NullLogger<MafTaskOrchestrator>.Instance);
    }

    [Fact]
    public async Task CreatePlanAsync_IndependentTasks_ShouldGroupAsParallel()
    {
        var tasks = new List<DecomposedTask>
        {
            new() { TaskName = "独立任务A" },
            new() { TaskName = "独立任务B" },
            new() { TaskName = "独立任务C" }
        };

        var plan = await _orchestrator.CreatePlanAsync(tasks);

        plan.Should().NotBeNull();
        plan.PlanId.Should().NotBeNullOrEmpty();
        // 无依赖的多任务应归入并行组
        var totalTasks = plan.ParallelGroups.SelectMany(g => g.Tasks).Count()
                         + plan.SerialGroups.SelectMany(g => g.Tasks).Count();
        totalTasks.Should().Be(3, "所有任务都应被分组到计划中");
    }

    [Fact]
    public async Task CreatePlanAsync_SingleTask_ShouldBeSerial()
    {
        var tasks = new List<DecomposedTask>
        {
            new() { TaskName = "单独任务" }
        };

        var plan = await _orchestrator.CreatePlanAsync(tasks);

        plan.SerialGroups.Should().HaveCount(1, "单个任务应放入串行组");
        plan.SerialGroups.First().Mode.Should().Be(GroupExecutionMode.Serial);
        plan.SerialGroups.First().Tasks.Should().HaveCount(1);
    }

    [Fact]
    public async Task ExecutePlanAsync_ShouldExecuteAllTasks()
    {
        var tasks = new List<DecomposedTask>
        {
            new() { TaskName = "执行任务1" },
            new() { TaskName = "执行任务2" }
        };

        var plan = await _orchestrator.CreatePlanAsync(tasks);
        var results = await _orchestrator.ExecutePlanAsync(plan);

        results.Should().HaveCount(2);
        results.Should().AllSatisfy(r => r.Success.Should().BeTrue());
    }

    [Fact]
    public async Task ExecutePlanAsync_ShouldSetGroupStatus()
    {
        var tasks = new List<DecomposedTask>
        {
            new() { TaskName = "状态追踪任务" }
        };

        var plan = await _orchestrator.CreatePlanAsync(tasks);
        await _orchestrator.ExecutePlanAsync(plan);

        var allGroups = plan.SerialGroups.Concat(plan.ParallelGroups);
        allGroups.Should().AllSatisfy(g =>
        {
            g.Status.Should().Be(GroupStatus.Completed);
            g.StartTime.Should().NotBeNull();
            g.EndTime.Should().NotBeNull();
        });
    }

    [Fact]
    public async Task ExecutePlanAsync_ShouldSetTaskTiming()
    {
        var tasks = new List<DecomposedTask>
        {
            new() { TaskName = "计时任务", Status = MafTaskStatus.Pending }
        };

        var plan = await _orchestrator.CreatePlanAsync(tasks);
        var results = await _orchestrator.ExecutePlanAsync(plan);

        results.First().StartedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        results.First().CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task CancelAsync_NonExistentPlan_ShouldNotThrow()
    {
        var act = () => _orchestrator.CancelAsync("nonexistent-plan-id");
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ExecutePlanAsync_EmptyPlan_ShouldReturnEmpty()
    {
        var plan = new ExecutionPlan();

        var results = await _orchestrator.ExecutePlanAsync(plan);

        results.Should().BeEmpty();
    }

    [Fact]
    public async Task CreatePlanAsync_EmptyTaskList_ShouldReturnEmptyPlan()
    {
        var plan = await _orchestrator.CreatePlanAsync(new List<DecomposedTask>());

        plan.SerialGroups.Should().BeEmpty();
        plan.ParallelGroups.Should().BeEmpty();
    }
}
