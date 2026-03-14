// src/tests/UnitTests/Repository/SchedulePlanRepositoryTests.cs
using CKY.MultiAgentFramework.Core.Enums;
using CKY.MultiAgentFramework.Core.Models.Persisted;
using CKY.MultiAgentFramework.Infrastructure.Repository.Repositories;
using CKY.MultiAgentFramework.Tests.Helpers;
using FluentAssertions;

namespace CKY.MultiAgentFramework.Tests.Repository;

public class SchedulePlanRepositoryTests : RepositoryTestBase
{
    private SchedulePlanRepository _repository
    {
        get
        {
            if (DbContext == null)
                throw new InvalidOperationException("DbContext not initialized. Call InitializeAsync first.");
            return new SchedulePlanRepository(DbContext);
        }
    }

    [Fact]
    public async Task AddAsync_ShouldAssignIdAndSetCreatedAt()
    {
        // Arrange
        var plan = TestDataBuilder.CreateSchedulePlan(p => p.PlanId = "test-plan-001");

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
        var plan = TestDataBuilder.CreateSchedulePlan(p => p.PlanId = "test-plan-002");
        await _repository.AddAsync(plan);

        // Act
        var result = await _repository.GetByIdAsync(plan.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(plan.Id);
        result.PlanId.Should().Be("test-plan-002");
        result.Status.Should().Be(SchedulePlanStatus.Created);
    }

    [Fact]
    public async Task GetByPlanIdAsync_ShouldReturnPlanByPlanId()
    {
        // Arrange
        var planId = "test-plan-003";
        var plan = TestDataBuilder.CreateSchedulePlan(p =>
        {
            p.PlanId = planId;
            p.TotalTasks = 10;
        });
        await _repository.AddAsync(plan);

        // Act
        var result = await _repository.GetByPlanIdAsync(planId);

        // Assert
        result.Should().NotBeNull();
        result!.PlanId.Should().Be(planId);
        result.TotalTasks.Should().Be(10);
    }

    [Fact]
    public async Task GetByStatusAsync_ShouldReturnPlansWithGivenStatus()
    {
        // Arrange
        var createdPlan = TestDataBuilder.CreateSchedulePlan(p =>
        {
            p.PlanId = "plan-created";
            p.Status = SchedulePlanStatus.Created;
        });
        var runningPlan = TestDataBuilder.CreateSchedulePlan(p =>
        {
            p.PlanId = "plan-running";
            p.Status = SchedulePlanStatus.Running;
        });
        var completedPlan = TestDataBuilder.CreateSchedulePlan(p =>
        {
            p.PlanId = "plan-completed";
            p.Status = SchedulePlanStatus.Completed;
        });

        await _repository.AddAsync(createdPlan);
        await _repository.AddAsync(runningPlan);
        await _repository.AddAsync(completedPlan);

        // Act
        var result = await _repository.GetByStatusAsync(SchedulePlanStatus.Running);

        // Assert
        result.Should().HaveCount(1);
        result[0].PlanId.Should().Be("plan-running");
        result[0].Status.Should().Be(SchedulePlanStatus.Running);
    }

    [Fact]
    public async Task GetRecentPlansAsync_ShouldReturnMostRecentPlans()
    {
        // Arrange
        var plan1 = TestDataBuilder.CreateSchedulePlan(p => p.PlanId = "plan-001");
        await _repository.AddAsync(plan1);
        await Task.Delay(10);

        var plan2 = TestDataBuilder.CreateSchedulePlan(p => p.PlanId = "plan-002");
        await _repository.AddAsync(plan2);
        await Task.Delay(10);

        var plan3 = TestDataBuilder.CreateSchedulePlan(p => p.PlanId = "plan-003");
        await _repository.AddAsync(plan3);

        // Act
        var result = await _repository.GetRecentPlansAsync(2);

        // Assert
        result.Should().HaveCount(2);
        result[0].PlanId.Should().Be("plan-003"); // Most recent
        result[1].PlanId.Should().Be("plan-002");
    }

    [Fact]
    public async Task UpdateAsync_ShouldModifyPlanProperties()
    {
        // Arrange
        var plan = TestDataBuilder.CreateSchedulePlan(p =>
        {
            p.PlanId = "plan-to-update";
            p.Status = SchedulePlanStatus.Created;
            p.TotalTasks = 5;
            p.HighPriorityCount = 2;
        });
        await _repository.AddAsync(plan);

        // Act
        plan.Status = SchedulePlanStatus.Running;
        plan.StartedAt = DateTime.UtcNow;
        plan.TotalTasks = 8;
        plan.HighPriorityCount = 3;
        await _repository.UpdateAsync(plan);

        // Assert
        var result = await _repository.GetByIdAsync(plan.Id);
        result.Should().NotBeNull();
        result!.Status.Should().Be(SchedulePlanStatus.Running);
        result.StartedAt.Should().NotBeNull();
        result.TotalTasks.Should().Be(8);
        result.HighPriorityCount.Should().Be(3);
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemovePlan()
    {
        // Arrange
        var plan = TestDataBuilder.CreateSchedulePlan(p => p.PlanId = "plan-to-delete");
        await _repository.AddAsync(plan);

        // Act
        await _repository.DeleteAsync(plan.Id);

        // Assert
        var result = await _repository.GetByIdAsync(plan.Id);
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByStatusAsync_ShouldOrderByCreatedAtDesc()
    {
        // Arrange
        var plan1 = TestDataBuilder.CreateSchedulePlan(p =>
        {
            p.PlanId = "plan-old";
            p.Status = SchedulePlanStatus.Created;
            p.CreatedAt = DateTime.UtcNow.AddMinutes(-10);
        });
        var plan2 = TestDataBuilder.CreateSchedulePlan(p =>
        {
            p.PlanId = "plan-new";
            p.Status = SchedulePlanStatus.Created;
            p.CreatedAt = DateTime.UtcNow.AddMinutes(-5);
        });
        var plan3 = TestDataBuilder.CreateSchedulePlan(p =>
        {
            p.PlanId = "plan-newest";
            p.Status = SchedulePlanStatus.Created;
            p.CreatedAt = DateTime.UtcNow.AddMinutes(-1);
        });

        await _repository.AddAsync(plan1);
        await _repository.AddAsync(plan2);
        await _repository.AddAsync(plan3);

        // Act
        var result = await _repository.GetByStatusAsync(SchedulePlanStatus.Created);

        // Assert
        result.Should().HaveCountGreaterThanOrEqualTo(3);
        // First three should be ordered by CreatedAt descending
        result[0].PlanId.Should().Be("plan-newest");
        result[1].PlanId.Should().Be("plan-new");
        result[2].PlanId.Should().Be("plan-old");
    }
}
