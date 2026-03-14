// src/tests/UnitTests/Repository/ExecutionPlanRepositoryTests.cs
using CKY.MultiAgentFramework.Core.Enums;
using CKY.MultiAgentFramework.Core.Models.Persisted;
using CKY.MultiAgentFramework.Repository.Repositories;
using CKY.MultiAgentFramework.Tests.Helpers;
using FluentAssertions;

namespace CKY.MultiAgentFramework.Tests.Repository;

public class ExecutionPlanRepositoryTests : RepositoryTestBase
{
    private ExecutionPlanRepository _repository
    {
        get
        {
            if (DbContext == null)
                throw new InvalidOperationException("DbContext not initialized. Call InitializeAsync first.");
            return new ExecutionPlanRepository(DbContext);
        }
    }

    [Fact]
    public async Task AddAsync_ShouldAssignIdAndSetCreatedAt()
    {
        // Arrange
        var plan = TestDataBuilder.CreateExecutionPlan(p => p.PlanId = "test-plan-001");

        // Act
        var result = await _repository.AddAsync(plan);

        // Assert
        result.Id.Should().BeGreaterThan(0);
        result.PlanId.Should().Be("test-plan-001");
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task GetByIdAsync_WhenPlanExists_ShouldReturnPlan()
    {
        // Arrange
        var plan = TestDataBuilder.CreateExecutionPlan(p => p.PlanId = "test-plan-002");
        await _repository.AddAsync(plan);

        // Act
        var result = await _repository.GetByIdAsync(plan.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(plan.Id);
        result.PlanId.Should().Be("test-plan-002");
        result.Status.Should().Be(ExecutionPlanStatus.Created);
    }

    [Fact]
    public async Task GetByPlanIdAsync_ShouldReturnPlanByPlanId()
    {
        // Arrange
        var planId = "test-plan-003";
        var plan = TestDataBuilder.CreateExecutionPlan(p =>
        {
            p.PlanId = planId;
            p.TotalTasks = 15;
        });
        await _repository.AddAsync(plan);

        // Act
        var result = await _repository.GetByPlanIdAsync(planId);

        // Assert
        result.Should().NotBeNull();
        result!.PlanId.Should().Be(planId);
        result.TotalTasks.Should().Be(15);
    }

    [Fact]
    public async Task GetByStatusAsync_ShouldReturnPlansWithGivenStatus()
    {
        // Arrange
        var createdPlan = TestDataBuilder.CreateExecutionPlan(p =>
        {
            p.PlanId = "plan-created";
            p.Status = ExecutionPlanStatus.Created;
        });
        var runningPlan = TestDataBuilder.CreateExecutionPlan(p =>
        {
            p.PlanId = "plan-running";
            p.Status = ExecutionPlanStatus.Running;
        });
        var completedPlan = TestDataBuilder.CreateExecutionPlan(p =>
        {
            p.PlanId = "plan-completed";
            p.Status = ExecutionPlanStatus.Completed;
        });

        await _repository.AddAsync(createdPlan);
        await _repository.AddAsync(runningPlan);
        await _repository.AddAsync(completedPlan);

        // Act
        var result = await _repository.GetByStatusAsync(ExecutionPlanStatus.Running);

        // Assert
        result.Should().HaveCount(1);
        result[0].PlanId.Should().Be("plan-running");
        result[0].Status.Should().Be(ExecutionPlanStatus.Running);
    }

    [Fact]
    public async Task GetByMultipleStatusAsync_ShouldReturnPlansMatchingAnyStatus()
    {
        // Arrange
        var createdPlan = TestDataBuilder.CreateExecutionPlan(p =>
        {
            p.PlanId = "plan-created";
            p.Status = ExecutionPlanStatus.Created;
        });
        var runningPlan = TestDataBuilder.CreateExecutionPlan(p =>
        {
            p.PlanId = "plan-running";
            p.Status = ExecutionPlanStatus.Running;
        });
        var completedPlan = TestDataBuilder.CreateExecutionPlan(p =>
        {
            p.PlanId = "plan-completed";
            p.Status = ExecutionPlanStatus.Completed;
        });
        var failedPlan = TestDataBuilder.CreateExecutionPlan(p =>
        {
            p.PlanId = "plan-failed";
            p.Status = ExecutionPlanStatus.Failed;
        });

        await _repository.AddAsync(createdPlan);
        await _repository.AddAsync(runningPlan);
        await _repository.AddAsync(completedPlan);
        await _repository.AddAsync(failedPlan);

        // Act
        var statuses = new List<ExecutionPlanStatus>
        {
            ExecutionPlanStatus.Running,
            ExecutionPlanStatus.Failed
        };
        var result = await _repository.GetByMultipleStatusAsync(statuses, 10);

        // Assert
        result.Should().HaveCount(2);
        result.All(p => p.Status == ExecutionPlanStatus.Running || p.Status == ExecutionPlanStatus.Failed)
            .Should().BeTrue();
    }

    [Fact]
    public async Task GetByMultipleStatusAsync_ShouldLimitToCount()
    {
        // Arrange
        for (int i = 0; i < 10; i++)
        {
            var plan = TestDataBuilder.CreateExecutionPlan(p =>
            {
                p.PlanId = $"plan-{i:D3}";
                p.Status = i % 2 == 0 ? ExecutionPlanStatus.Created : ExecutionPlanStatus.Running;
            });
            await _repository.AddAsync(plan);
        }

        // Act
        var statuses = new List<ExecutionPlanStatus>
        {
            ExecutionPlanStatus.Created,
            ExecutionPlanStatus.Running
        };
        var result = await _repository.GetByMultipleStatusAsync(statuses, 5);

        // Assert
        result.Should().HaveCount(5);
    }

    [Fact]
    public async Task UpdateAsync_ShouldModifyPlanProperties()
    {
        // Arrange
        var plan = TestDataBuilder.CreateExecutionPlan(p =>
        {
            p.PlanId = "plan-to-update";
            p.Status = ExecutionPlanStatus.Created;
            p.TotalTasks = 5;
            p.CompletedTasks = 0;
            p.SerialGroupCount = 2;
            p.ParallelGroupCount = 1;
        });
        await _repository.AddAsync(plan);

        // Act
        plan.Status = ExecutionPlanStatus.Running;
        plan.StartedAt = DateTime.UtcNow;
        plan.TotalTasks = 8;
        plan.CompletedTasks = 2;
        plan.SerialGroupCount = 3;
        plan.ParallelGroupCount = 2;
        await _repository.UpdateAsync(plan);

        // Assert
        var result = await _repository.GetByIdAsync(plan.Id);
        result.Should().NotBeNull();
        result!.Status.Should().Be(ExecutionPlanStatus.Running);
        result.StartedAt.Should().NotBeNull();
        result.TotalTasks.Should().Be(8);
        result.CompletedTasks.Should().Be(2);
        result.SerialGroupCount.Should().Be(3);
        result.ParallelGroupCount.Should().Be(2);
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemovePlan()
    {
        // Arrange
        var plan = TestDataBuilder.CreateExecutionPlan(p => p.PlanId = "plan-to-delete");
        await _repository.AddAsync(plan);

        // Act
        await _repository.DeleteAsync(plan.Id);

        // Assert
        var result = await _repository.GetByIdAsync(plan.Id);
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByMultipleStatusAsync_ShouldOrderByCreatedAtDesc()
    {
        // Arrange
        var plan1 = TestDataBuilder.CreateExecutionPlan(p =>
        {
            p.PlanId = "plan-old";
            p.Status = ExecutionPlanStatus.Created;
            p.CreatedAt = DateTime.UtcNow.AddMinutes(-10);
        });
        var plan2 = TestDataBuilder.CreateExecutionPlan(p =>
        {
            p.PlanId = "plan-new";
            p.Status = ExecutionPlanStatus.Created;
            p.CreatedAt = DateTime.UtcNow.AddMinutes(-5);
        });
        var plan3 = TestDataBuilder.CreateExecutionPlan(p =>
        {
            p.PlanId = "plan-newest";
            p.Status = ExecutionPlanStatus.Created;
            p.CreatedAt = DateTime.UtcNow.AddMinutes(-1);
        });

        await _repository.AddAsync(plan1);
        await _repository.AddAsync(plan2);
        await _repository.AddAsync(plan3);

        // Act
        var statuses = new List<ExecutionPlanStatus> { ExecutionPlanStatus.Created };
        var result = await _repository.GetByMultipleStatusAsync(statuses, 10);

        // Assert
        result.Should().HaveCountGreaterThanOrEqualTo(3);
        // First three should be ordered by CreatedAt descending
        result[0].PlanId.Should().Be("plan-newest");
        result[1].PlanId.Should().Be("plan-new");
        result[2].PlanId.Should().Be("plan-old");
    }
}
