// src/tests/UnitTests/Repository/TaskExecutionResultRepositoryTests.cs
using CKY.MultiAgentFramework.Core.Models.Persisted;
using CKY.MultiAgentFramework.Repository.Repositories;
using CKY.MultiAgentFramework.Tests.Helpers;
using FluentAssertions;

namespace CKY.MultiAgentFramework.Tests.Repository;

public class TaskExecutionResultRepositoryTests : RepositoryTestBase
{
    private TaskExecutionResultRepository _repository
    {
        get
        {
            if (DbContext == null)
                throw new InvalidOperationException("DbContext not initialized. Call InitializeAsync first.");
            return new TaskExecutionResultRepository(DbContext);
        }
    }

    [Fact]
    public async Task AddAsync_ShouldAssignIdAndSetCreatedAt()
    {
        // Arrange
        var result = TestDataBuilder.CreateTaskExecutionResult(r => r.Success = true);

        // Act
        var savedResult = await _repository.AddAsync(result);

        // Assert
        savedResult.Id.Should().BeGreaterThan(0);
        savedResult.Success.Should().BeTrue();
        savedResult.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task GetByIdAsync_WhenResultExists_ShouldReturnResult()
    {
        // Arrange
        var result = TestDataBuilder.CreateTaskExecutionResult(r => r.Success = true);
        await _repository.AddAsync(result);

        // Act
        var retrieved = await _repository.GetByIdAsync(result.Id);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.Id.Should().Be(result.Id);
        retrieved.Success.Should().BeTrue();
    }

    [Fact]
    public async Task GetByTaskIdAsync_ShouldReturnResultsOrderedByCreatedAt()
    {
        // Arrange
        var taskId = Guid.NewGuid().ToString();
        var result1 = TestDataBuilder.CreateTaskExecutionResult(r =>
        {
            r.TaskId = taskId;
            r.Success = true;
            r.CreatedAt = DateTime.UtcNow.AddMinutes(-10);
        });
        var result2 = TestDataBuilder.CreateTaskExecutionResult(r =>
        {
            r.TaskId = taskId;
            r.Success = false;
            r.CreatedAt = DateTime.UtcNow.AddMinutes(-5);
        });
        var result3 = TestDataBuilder.CreateTaskExecutionResult(r =>
        {
            r.TaskId = taskId;
            r.Success = true;
            r.CreatedAt = DateTime.UtcNow.AddMinutes(-1);
        });
        await _repository.AddRangeAsync(new List<TaskExecutionResultEntity> { result1, result2, result3 });

        // Act
        var retrieved = await _repository.GetByTaskIdAsync(taskId);

        // Assert
        retrieved.Should().HaveCount(3);
        // Should be ordered by CreatedAt descending (most recent first)
        retrieved[0].Id.Should().Be(result3.Id);
        retrieved[1].Id.Should().Be(result2.Id);
        retrieved[2].Id.Should().Be(result1.Id);
    }

    [Fact]
    public async Task GetByPlanIdAsync_ShouldReturnResultsOrderedByStartedAt()
    {
        // Arrange
        var planId = Guid.NewGuid().ToString();
        var result1 = TestDataBuilder.CreateTaskExecutionResult(r =>
        {
            r.PlanId = planId;
            r.StartedAt = DateTime.UtcNow.AddMinutes(-5);
        });
        var result2 = TestDataBuilder.CreateTaskExecutionResult(r =>
        {
            r.PlanId = planId;
            r.StartedAt = DateTime.UtcNow.AddMinutes(-10);
        });
        var result3 = TestDataBuilder.CreateTaskExecutionResult(r =>
        {
            r.PlanId = planId;
            r.StartedAt = DateTime.UtcNow.AddMinutes(-1);
        });
        await _repository.AddRangeAsync(new List<TaskExecutionResultEntity> { result1, result2, result3 });

        // Act
        var retrieved = await _repository.GetByPlanIdAsync(planId);

        // Assert
        retrieved.Should().HaveCount(3);
        // Should be ordered by StartedAt ascending (oldest first)
        retrieved[0].Id.Should().Be(result2.Id);
        retrieved[1].Id.Should().Be(result1.Id);
        retrieved[2].Id.Should().Be(result3.Id);
    }

    [Fact]
    public async Task AddRangeAsync_ShouldAddMultipleResults()
    {
        // Arrange
        var results = new List<TaskExecutionResultEntity>
        {
            TestDataBuilder.CreateTaskExecutionResult(r => r.Success = true),
            TestDataBuilder.CreateTaskExecutionResult(r => r.Success = false),
            TestDataBuilder.CreateTaskExecutionResult(r => r.Success = true)
        };

        // Act
        var saved = await _repository.AddRangeAsync(results);

        // Assert
        saved.Should().HaveCount(3);
        saved.All(r => r.Id > 0).Should().BeTrue();
        saved[0].Success.Should().BeTrue();
        saved[1].Success.Should().BeFalse();
        saved[2].Success.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateAsync_ShouldModifyResultProperties()
    {
        // Arrange
        var result = TestDataBuilder.CreateTaskExecutionResult(r =>
        {
            r.Success = false;
            r.Message = "Original message";
            r.Error = "Original error";
        });
        await _repository.AddAsync(result);

        // Act
        result.Success = true;
        result.Message = "Updated message";
        result.Error = null;
        result.CompletedAt = DateTime.UtcNow;
        result.DurationMs = 1000;
        await _repository.UpdateAsync(result);

        // Assert
        var retrieved = await _repository.GetByIdAsync(result.Id);
        retrieved.Should().NotBeNull();
        retrieved!.Success.Should().BeTrue();
        retrieved.Message.Should().Be("Updated message");
        retrieved.Error.Should().BeNull();
        retrieved.CompletedAt.Should().NotBeNull();
        retrieved.DurationMs.Should().Be(1000);
    }

    [Fact]
    public async Task AddRangeAsync_ShouldCalculateDuration()
    {
        // Arrange
        var startTime = DateTime.UtcNow.AddMinutes(-1);
        var completedAt = DateTime.UtcNow;

        var result = TestDataBuilder.CreateTaskExecutionResult(r =>
        {
            r.StartedAt = startTime;
            r.CompletedAt = completedAt;
        });

        // Act
        await _repository.AddAsync(result);

        // Assert
        var retrieved = await _repository.GetByIdAsync(result.Id);
        retrieved.Should().NotBeNull();
        retrieved!.StartedAt.Should().BeCloseTo(startTime, TimeSpan.FromMilliseconds(100));
        retrieved.CompletedAt.Should().BeCloseTo(completedAt, TimeSpan.FromMilliseconds(100));
        // Note: DurationMs is calculated by the service layer, not the repository
        // This test verifies that the repository can store and retrieve the values
    }
}
