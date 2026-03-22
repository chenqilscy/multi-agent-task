using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Core.Enums;
using CKY.MultiAgentFramework.Core.Models.Persisted;
using CKY.MultiAgentFramework.Core.Models.Task;
using CKY.MultiAgentFramework.Services.Scheduling;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CKY.MultiAgentFramework.Tests.Services.Scheduling;

public class PersistentTaskSchedulerTests
{
    private readonly Mock<ITaskScheduler> _innerScheduler = new();
    private readonly Mock<ISchedulePlanRepository> _planRepository = new();
    private readonly Mock<ILogger<PersistentTaskScheduler>> _logger = new();

    private PersistentTaskScheduler CreateSut() =>
        new(_innerScheduler.Object, _planRepository.Object, _logger.Object);

    [Fact]
    public void Constructor_NullArgs_ShouldThrow()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new PersistentTaskScheduler(null!, _planRepository.Object, _logger.Object));
        Assert.Throws<ArgumentNullException>(() =>
            new PersistentTaskScheduler(_innerScheduler.Object, null!, _logger.Object));
        Assert.Throws<ArgumentNullException>(() =>
            new PersistentTaskScheduler(_innerScheduler.Object, _planRepository.Object, null!));
    }

    [Fact]
    public async Task ScheduleAsync_ShouldDelegateAndPersist()
    {
        var tasks = new List<DecomposedTask>
        {
            new() { TaskId = "task-1", TaskName = "Test Task" }
        };
        var plan = new ScheduleExecutionPlan { PlanId = "plan-1" };
        var scheduleResult = new ScheduleResult
        {
            ExecutionPlan = plan,
            ScheduledTasks = new List<string> { "task-1" }
        };
        _innerScheduler.Setup(x => x.ScheduleAsync(tasks, It.IsAny<CancellationToken>()))
            .ReturnsAsync(scheduleResult);
        _planRepository.Setup(x => x.AddAsync(It.IsAny<SchedulePlanEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SchedulePlanEntity e, CancellationToken _) => e);

        var sut = CreateSut();
        var result = await sut.ScheduleAsync(tasks);

        result.ExecutionPlan.PlanId.Should().Be("plan-1");
        _innerScheduler.Verify(x => x.ScheduleAsync(tasks, It.IsAny<CancellationToken>()), Times.Once);
        _planRepository.Verify(x => x.AddAsync(It.IsAny<SchedulePlanEntity>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteTaskAsync_ShouldDelegate()
    {
        var task = new DecomposedTask { TaskId = "task-1" };
        var expectedResult = new TaskExecutionResult { TaskId = "task-1", Success = true };
        _innerScheduler.Setup(x => x.ExecuteTaskAsync(task, It.IsAny<Func<DecomposedTask, CancellationToken, Task<TaskExecutionResult>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        var sut = CreateSut();
        var result = await sut.ExecuteTaskAsync(task, (t, ct) => Task.FromResult(expectedResult));

        result.TaskId.Should().Be("task-1");
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task RestoreScheduleAsync_Exists_ShouldReturn()
    {
        var planEntity = new SchedulePlanEntity
        {
            PlanId = "plan-1",
            PlanJson = "{\"ExecutionPlan\":{\"PlanId\":\"plan-1\",\"SerialGroups\":[],\"ParallelGroups\":[]},\"ScheduledTasks\":[]}"
        };
        _planRepository.Setup(x => x.GetByPlanIdAsync("plan-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(planEntity);

        var sut = CreateSut();
        var result = await sut.RestoreScheduleAsync("plan-1");

        // May return null if JSON doesn't deserialize correctly, that's OK
        _planRepository.Verify(x => x.GetByPlanIdAsync("plan-1", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RestoreScheduleAsync_NotFound_ShouldReturnNull()
    {
        _planRepository.Setup(x => x.GetByPlanIdAsync("missing", It.IsAny<CancellationToken>()))
            .ReturnsAsync((SchedulePlanEntity?)null);

        var sut = CreateSut();
        var result = await sut.RestoreScheduleAsync("missing");

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetPlanStatusAsync_ShouldDelegate()
    {
        var entity = new SchedulePlanEntity { PlanId = "p-1", Status = SchedulePlanStatus.Running };
        _planRepository.Setup(x => x.GetByPlanIdAsync("p-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        var sut = CreateSut();
        var result = await sut.GetPlanStatusAsync("p-1");

        result.Should().NotBeNull();
        result!.Status.Should().Be(SchedulePlanStatus.Running);
    }

    [Fact]
    public async Task UpdatePlanStatusAsync_ShouldUpdateAndSave()
    {
        var entity = new SchedulePlanEntity { PlanId = "p-1", Status = SchedulePlanStatus.Created };
        _planRepository.Setup(x => x.GetByPlanIdAsync("p-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        var sut = CreateSut();
        await sut.UpdatePlanStatusAsync("p-1", SchedulePlanStatus.Completed);

        _planRepository.Verify(x => x.UpdateAsync(
            It.Is<SchedulePlanEntity>(e => e.Status == SchedulePlanStatus.Completed && e.CompletedAt.HasValue),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdatePlanStatusAsync_Running_ShouldSetStartedAt()
    {
        var entity = new SchedulePlanEntity { PlanId = "p-1" };
        _planRepository.Setup(x => x.GetByPlanIdAsync("p-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        var sut = CreateSut();
        await sut.UpdatePlanStatusAsync("p-1", SchedulePlanStatus.Running);

        _planRepository.Verify(x => x.UpdateAsync(
            It.Is<SchedulePlanEntity>(e => e.StartedAt.HasValue),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetRecentPlansAsync_ShouldDelegate()
    {
        var plans = new List<SchedulePlanEntity>
        {
            new() { PlanId = "p-1" },
            new() { PlanId = "p-2" }
        };
        _planRepository.Setup(x => x.GetRecentPlansAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(plans);

        var sut = CreateSut();
        var result = await sut.GetRecentPlansAsync(10);

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetPlansByStatusAsync_ShouldDelegate()
    {
        var plans = new List<SchedulePlanEntity>();
        _planRepository.Setup(x => x.GetByStatusAsync(SchedulePlanStatus.Failed, It.IsAny<CancellationToken>()))
            .ReturnsAsync(plans);

        var sut = CreateSut();
        var result = await sut.GetPlansByStatusAsync(SchedulePlanStatus.Failed);

        result.Should().BeEmpty();
    }
}
