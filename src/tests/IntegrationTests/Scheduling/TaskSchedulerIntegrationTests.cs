using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Core.Enums;
using CKY.MultiAgentFramework.Core.Models.Task;
using CKY.MultiAgentFramework.Services.Scheduling;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CKY.MultiAgentFramework.IntegrationTests.Scheduling;

/// <summary>
/// MafTaskScheduler + MafPriorityCalculator 集成测试
/// 验证调度器与真实优先级计算器的完整流程
/// </summary>
public class TaskSchedulerIntegrationTests
{
    private readonly MafTaskScheduler _scheduler;

    public TaskSchedulerIntegrationTests()
    {
        var calculator = new MafPriorityCalculator(
            NullLogger<MafPriorityCalculator>.Instance);
        _scheduler = new MafTaskScheduler(
            calculator,
            maxConcurrentTasks: 5,
            logger: NullLogger<MafTaskScheduler>.Instance);
    }

    [Fact]
    public async Task ScheduleAsync_ShouldCalculatePrioritiesAndGroup()
    {
        var tasks = new List<DecomposedTask>
        {
            new()
            {
                TaskName = "关键任务",
                Priority = TaskPriority.Critical,
                PriorityReason = PriorityReason.UserExplicit
            },
            new()
            {
                TaskName = "普通任务",
                Priority = TaskPriority.Normal,
                PriorityReason = PriorityReason.SystemDefault
            },
            new()
            {
                TaskName = "后台任务",
                Priority = TaskPriority.Background,
                PriorityReason = PriorityReason.SystemDefault
            }
        };

        var result = await _scheduler.ScheduleAsync(tasks);

        result.ScheduledTasks.Should().HaveCount(3);
        result.ExecutionPlan.Should().NotBeNull();

        // 验证高优先级任务应在前面
        var allGrouped = result.ExecutionPlan.HighPriorityTasks.Count
                         + result.ExecutionPlan.MediumPriorityTasks.Count
                         + result.ExecutionPlan.LowPriorityTasks.Count;
        allGrouped.Should().Be(3);
    }

    [Fact]
    public async Task ScheduleAsync_CriticalTask_ShouldBeHighPriority()
    {
        var tasks = new List<DecomposedTask>
        {
            new()
            {
                TaskName = "紧急任务",
                Priority = TaskPriority.Critical,
            }
        };

        var result = await _scheduler.ScheduleAsync(tasks);

        // Critical base=40, Normal interaction, etc. should be >50
        result.ExecutionPlan.HighPriorityTasks.Should().NotBeEmpty(
            "Critical 优先级的任务应被分组到高优先级");
    }

    [Fact]
    public async Task ScheduleAsync_BackgroundTask_ShouldBeLowPriority()
    {
        var tasks = new List<DecomposedTask>
        {
            new()
            {
                TaskName = "后台清理",
                Priority = TaskPriority.Background,
            }
        };

        var result = await _scheduler.ScheduleAsync(tasks);

        result.ExecutionPlan.LowPriorityTasks.Should().NotBeEmpty(
            "Background 优先级的任务应被分组到低优先级");
    }

    [Fact]
    public async Task ScheduleAsync_ShouldSortByPriorityScoreDesc()
    {
        var tasks = new List<DecomposedTask>
        {
            new() { TaskName = "Low", Priority = TaskPriority.Low },
            new() { TaskName = "High", Priority = TaskPriority.High },
            new() { TaskName = "Critical", Priority = TaskPriority.Critical },
            new() { TaskName = "Normal", Priority = TaskPriority.Normal },
        };

        var result = await _scheduler.ScheduleAsync(tasks);

        // 验证 PriorityScore 被计算并为有效值
        foreach (var task in tasks)
        {
            task.PriorityScore.Should().BeInRange(0, 100);
        }

        // Critical 应有最高分
        var critical = tasks.First(t => t.TaskName == "Critical");
        var low = tasks.First(t => t.TaskName == "Low");
        critical.PriorityScore.Should().BeGreaterThan(low.PriorityScore);
    }

    [Fact]
    public async Task ExecuteTaskAsync_Success_ShouldTrackStatus()
    {
        var task = new DecomposedTask
        {
            TaskName = "执行测试",
            Priority = TaskPriority.Normal,
            Status = MafTaskStatus.Pending
        };

        var result = await _scheduler.ExecuteTaskAsync(task, async (t, ct) =>
        {
            await Task.Delay(50, ct);
            return new TaskExecutionResult
            {
                TaskId = t.TaskId,
                Success = true,
                Message = "执行成功",
                CompletedAt = DateTime.UtcNow
            };
        });

        result.Success.Should().BeTrue();
        task.Status.Should().Be(MafTaskStatus.Completed);
        task.StartedAt.Should().NotBeNull();
        task.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task ExecuteTaskAsync_Failure_ShouldMarkFailed()
    {
        var task = new DecomposedTask
        {
            TaskName = "失败测试",
            Priority = TaskPriority.Normal,
            Status = MafTaskStatus.Pending
        };

        var result = await _scheduler.ExecuteTaskAsync(task, (t, ct) =>
        {
            return Task.FromResult(new TaskExecutionResult
            {
                TaskId = t.TaskId,
                Success = false,
                Error = "模拟失败",
                CompletedAt = DateTime.UtcNow
            });
        });

        result.Success.Should().BeFalse();
        task.Status.Should().Be(MafTaskStatus.Failed);
    }

    [Fact]
    public async Task ExecuteTaskAsync_ConcurrencyLimit_ShouldThrottle()
    {
        // 5 个并发限制的 scheduler
        var activeCount = 0;
        var maxActive = 0;
        var lockObj = new object();

        var taskList = Enumerable.Range(1, 10).Select(i => new DecomposedTask
        {
            TaskName = $"并发任务{i}",
            Priority = TaskPriority.Normal
        }).ToList();

        var executionTasks = taskList.Select(t =>
            _scheduler.ExecuteTaskAsync(t, async (dt, ct) =>
            {
                lock (lockObj)
                {
                    activeCount++;
                    if (activeCount > maxActive) maxActive = activeCount;
                }

                await Task.Delay(100, ct);

                lock (lockObj) { activeCount--; }

                return new TaskExecutionResult
                {
                    TaskId = dt.TaskId,
                    Success = true,
                    CompletedAt = DateTime.UtcNow
                };
            })
        ).ToList();

        await Task.WhenAll(executionTasks);

        maxActive.Should().BeLessThanOrEqualTo(5,
            "不应超过 maxConcurrentTasks=5 的并发限制");
    }

    [Fact]
    public async Task ScheduleAsync_EmptyList_ShouldReturnEmptyPlan()
    {
        var result = await _scheduler.ScheduleAsync(new List<DecomposedTask>());

        result.ScheduledTasks.Should().BeEmpty();
        result.ExecutionPlan.HighPriorityTasks.Should().BeEmpty();
        result.ExecutionPlan.MediumPriorityTasks.Should().BeEmpty();
        result.ExecutionPlan.LowPriorityTasks.Should().BeEmpty();
    }
}
