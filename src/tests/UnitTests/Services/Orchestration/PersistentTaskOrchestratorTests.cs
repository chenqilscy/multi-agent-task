using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Core.Enums;
using CKY.MultiAgentFramework.Core.Models.Persisted;
using CKY.MultiAgentFramework.Core.Models.Task;
using CKY.MultiAgentFramework.Services.Orchestration;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CKY.MultiAgentFramework.Tests.Services.Orchestration;

public class PersistentTaskOrchestratorTests
{
    private readonly Mock<ITaskOrchestrator> _innerOrchestrator = new();
    private readonly Mock<IExecutionPlanRepository> _planRepository = new();
    private readonly Mock<ITaskExecutionResultRepository> _resultRepository = new();
    private readonly Mock<ILogger<PersistentTaskOrchestrator>> _logger = new();

    private PersistentTaskOrchestrator CreateSut() =>
        new(_innerOrchestrator.Object, _planRepository.Object, _resultRepository.Object, _logger.Object);

    [Fact]
    public void Constructor_NullArgs_ShouldThrow()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new PersistentTaskOrchestrator(null!, _planRepository.Object, _resultRepository.Object, _logger.Object));
        Assert.Throws<ArgumentNullException>(() =>
            new PersistentTaskOrchestrator(_innerOrchestrator.Object, null!, _resultRepository.Object, _logger.Object));
        Assert.Throws<ArgumentNullException>(() =>
            new PersistentTaskOrchestrator(_innerOrchestrator.Object, _planRepository.Object, null!, _logger.Object));
        Assert.Throws<ArgumentNullException>(() =>
            new PersistentTaskOrchestrator(_innerOrchestrator.Object, _planRepository.Object, _resultRepository.Object, null!));
    }

    [Fact]
    public async Task CreatePlanAsync_ShouldDelegateAndPersist()
    {
        var tasks = new List<DecomposedTask> { new() { TaskId = "t1" } };
        var plan = new ExecutionPlan { PlanId = "ep-1" };
        _innerOrchestrator.Setup(x => x.CreatePlanAsync(tasks, It.IsAny<CancellationToken>()))
            .ReturnsAsync(plan);
        _planRepository.Setup(x => x.AddAsync(It.IsAny<ExecutionPlanEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ExecutionPlanEntity e, CancellationToken _) => e);

        var sut = CreateSut();
        var result = await sut.CreatePlanAsync(tasks);

        result.PlanId.Should().Be("ep-1");
        _planRepository.Verify(x => x.AddAsync(It.IsAny<ExecutionPlanEntity>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecutePlanAsync_AllSuccess_ShouldPersistResults()
    {
        var plan = new ExecutionPlan { PlanId = "ep-1" };
        var results = new List<TaskExecutionResult>
        {
            new() { TaskId = "t1", Success = true },
            new() { TaskId = "t2", Success = true }
        };
        _innerOrchestrator.Setup(x => x.ExecutePlanAsync(plan, It.IsAny<CancellationToken>()))
            .ReturnsAsync(results);
        _planRepository.Setup(x => x.GetByPlanIdAsync("ep-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExecutionPlanEntity { PlanId = "ep-1" });
        _resultRepository.Setup(x => x.AddRangeAsync(It.IsAny<List<TaskExecutionResultEntity>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((List<TaskExecutionResultEntity> r, CancellationToken _) => r);

        var sut = CreateSut();
        var actual = await sut.ExecutePlanAsync(plan);

        actual.Should().HaveCount(2);
        actual.Should().AllSatisfy(r => r.Success.Should().BeTrue());
        _resultRepository.Verify(x => x.AddRangeAsync(It.IsAny<List<TaskExecutionResultEntity>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecutePlanAsync_PartialFailure_ShouldSetPartiallyCompleted()
    {
        var plan = new ExecutionPlan { PlanId = "ep-1" };
        var results = new List<TaskExecutionResult>
        {
            new() { TaskId = "t1", Success = true },
            new() { TaskId = "t2", Success = false }
        };
        _innerOrchestrator.Setup(x => x.ExecutePlanAsync(plan, It.IsAny<CancellationToken>()))
            .ReturnsAsync(results);
        _planRepository.Setup(x => x.GetByPlanIdAsync("ep-1", It.IsAny<CancellationToken>()))
            .Returns(() => Task.FromResult<ExecutionPlanEntity?>(new ExecutionPlanEntity { PlanId = "ep-1" }));
        _resultRepository.Setup(x => x.AddRangeAsync(It.IsAny<List<TaskExecutionResultEntity>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((List<TaskExecutionResultEntity> r, CancellationToken _) => r);

        var sut = CreateSut();
        await sut.ExecutePlanAsync(plan);

        // Verify the status was updated to PartiallyCompleted (2 UpdateAsync calls: Running + PartiallyCompleted)
        _planRepository.Verify(x => x.UpdateAsync(
            It.Is<ExecutionPlanEntity>(e => e.Status == ExecutionPlanStatus.PartiallyCompleted),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecutePlanAsync_InnerThrows_ShouldSetFailed()
    {
        var plan = new ExecutionPlan { PlanId = "ep-1" };
        _innerOrchestrator.Setup(x => x.ExecutePlanAsync(plan, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("test error"));
        _planRepository.Setup(x => x.GetByPlanIdAsync("ep-1", It.IsAny<CancellationToken>()))
            .Returns(() => Task.FromResult<ExecutionPlanEntity?>(new ExecutionPlanEntity { PlanId = "ep-1" }));

        var sut = CreateSut();
        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.ExecutePlanAsync(plan));

        _planRepository.Verify(x => x.UpdateAsync(
            It.Is<ExecutionPlanEntity>(e => e.Status == ExecutionPlanStatus.Failed),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CancelAsync_ShouldDelegateAndUpdateStatus()
    {
        _planRepository.Setup(x => x.GetByPlanIdAsync("ep-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExecutionPlanEntity { PlanId = "ep-1" });

        var sut = CreateSut();
        await sut.CancelAsync("ep-1");

        _innerOrchestrator.Verify(x => x.CancelAsync("ep-1", It.IsAny<CancellationToken>()), Times.Once);
        _planRepository.Verify(x => x.UpdateAsync(
            It.Is<ExecutionPlanEntity>(e => e.Status == ExecutionPlanStatus.Cancelled),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RestorePlanAsync_NotFound_ShouldReturnNull()
    {
        _planRepository.Setup(x => x.GetByPlanIdAsync("missing", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ExecutionPlanEntity?)null);

        var sut = CreateSut();
        var result = await sut.RestorePlanAsync("missing");

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetExecutionResultsAsync_ShouldDelegate()
    {
        var results = new List<TaskExecutionResultEntity>
        {
            new() { TaskId = "t1", PlanId = "ep-1", Success = true }
        };
        _resultRepository.Setup(x => x.GetByPlanIdAsync("ep-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(results);

        var sut = CreateSut();
        var actual = await sut.GetExecutionResultsAsync("ep-1");

        actual.Should().HaveCount(1);
    }
}
