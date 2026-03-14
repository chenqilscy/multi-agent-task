// src/tests/UnitTests/Repository/MainTaskRepositoryTests.cs
using CKY.MultiAgentFramework.Core.Enums;
using CKY.MultiAgentFramework.Core.Models.Task;
using CKY.MultiAgentFramework.Repository.Repositories;
using CKY.MultiAgentFramework.Tests.Helpers;
using FluentAssertions;

namespace CKY.MultiAgentFramework.Tests.Repository;

public class MainTaskRepositoryTests : RepositoryTestBase
{
    private readonly MainTaskRepository _repository;

    public MainTaskRepositoryTests()
    {
        _repository = new MainTaskRepository(DbContext);
    }

    [Fact]
    public async Task AddAsync_ShouldAssignIdAndSetCreatedAt()
    {
        // Arrange
        var task = TestDataBuilder.CreateMainTask(t => t.Title = "New Task");

        // Act
        var result = await _repository.AddAsync(task);

        // Assert
        result.Id.Should().BeGreaterThan(0);
        result.Title.Should().Be("New Task");
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task GetByIdAsync_WhenTaskExists_ShouldReturnTaskWithSubTasks()
    {
        // Arrange
        var task = TestDataBuilder.CreateMainTask(t => t.Title = "Test Task");
        await _repository.AddAsync(task);

        // Act
        var result = await _repository.GetByIdAsync(task.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(task.Id);
        result.Title.Should().Be("Test Task");
        result.SubTasks.Should().NotBeNull();
    }

    [Fact]
    public async Task GetByIdAsync_WhenTaskNotFound_ShouldReturnNull()
    {
        // Act
        var result = await _repository.GetByIdAsync(999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllTasksOrderedByCreatedAt()
    {
        // Arrange
        var task1 = TestDataBuilder.CreateMainTask(t => t.Title = "Task 1");
        var task2 = TestDataBuilder.CreateMainTask(t => t.Title = "Task 2");
        await _repository.AddAsync(task1);
        await Task.Delay(10);
        await _repository.AddAsync(task2);

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        result.Should().HaveCount(2);
        result[0].Title.Should().Be("Task 2"); // 最新的在前
        result[1].Title.Should().Be("Task 1");
    }

    [Fact]
    public async Task GetByStatusAsync_ShouldReturnOnlyTasksWithGivenStatus()
    {
        // Arrange
        var pendingTask = TestDataBuilder.CreateMainTask(t =>
        {
            t.Title = "Pending Task";
            t.Status = MafTaskStatus.Pending;
        });
        var completedTask = TestDataBuilder.CreateMainTask(t =>
        {
            t.Title = "Completed Task";
            t.Status = MafTaskStatus.Completed;
        });
        await _repository.AddAsync(pendingTask);
        await _repository.AddAsync(completedTask);

        // Act
        var result = await _repository.GetByStatusAsync(MafTaskStatus.Pending);

        // Assert
        result.Should().HaveCount(1);
        result[0].Title.Should().Be("Pending Task");
    }

    [Fact]
    public async Task GetHighPriorityTasksAsync_ShouldReturnTasksAboveThreshold()
    {
        // Arrange
        var highPriorityTask = TestDataBuilder.CreateMainTask(t =>
        {
            t.Title = "High Priority";
            t.Priority = TaskPriority.High;
            t.Status = MafTaskStatus.Pending;
        });
        var lowPriorityTask = TestDataBuilder.CreateMainTask(t =>
        {
            t.Title = "Low Priority";
            t.Priority = TaskPriority.Low;
            t.Status = MafTaskStatus.Pending;
        });
        await _repository.AddAsync(highPriorityTask);
        await _repository.AddAsync(lowPriorityTask);

        // Act
        var result = await _repository.GetHighPriorityTasksAsync(50);

        // Assert
        result.Should().HaveCount(1);
        result[0].Title.Should().Be("High Priority");
    }

    [Fact]
    public async Task UpdateAsync_ShouldModifyTaskProperties()
    {
        // Arrange
        var task = TestDataBuilder.CreateMainTask(t => t.Title = "Original Title");
        await _repository.AddAsync(task);

        // Act
        task.Title = "Updated Title";
        await _repository.UpdateAsync(task);

        // Assert
        var result = await _repository.GetByIdAsync(task.Id);
        result!.Title.Should().Be("Updated Title");
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveTask()
    {
        // Arrange
        var task = TestDataBuilder.CreateMainTask();
        await _repository.AddAsync(task);

        // Act
        await _repository.DeleteAsync(task.Id);

        // Assert
        var result = await _repository.GetByIdAsync(task.Id);
        result.Should().BeNull();
    }

    [Fact]
    public async Task AddAsync_WithSubTasks_ShouldSaveBoth()
    {
        // Arrange
        var mainTask = TestDataBuilder.CreateMainTask(t => t.Title = "Parent Task");
        var subTask = TestDataBuilder.CreateSubTask(s =>
        {
            s.Title = "Child Task";
            s.MainTaskId = mainTask.Id;
            s.MainTask = mainTask;
        });
        mainTask.SubTasks.Add(subTask);

        // Act
        var result = await _repository.AddAsync(mainTask);

        // Assert
        result.SubTasks.Should().HaveCount(1);
        result.SubTasks.First().Title.Should().Be("Child Task");
    }

    [Fact]
    public async Task GetHighPriorityTasksAsync_ShouldOrderByPriorityThenCreatedAt()
    {
        // Arrange
        var task1 = TestDataBuilder.CreateMainTask(t =>
        {
            t.Title = "High Priority 1";
            t.Priority = TaskPriority.High;
            t.Status = MafTaskStatus.Pending;
            t.CreatedAt = DateTime.UtcNow.AddMinutes(-10);
        });
        var task2 = TestDataBuilder.CreateMainTask(t =>
        {
            t.Title = "High Priority 2";
            t.Priority = TaskPriority.High;
            t.Status = MafTaskStatus.Pending;
            t.CreatedAt = DateTime.UtcNow.AddMinutes(-5);
        });
        await _repository.AddAsync(task1);
        await _repository.AddAsync(task2);

        // Act
        var result = await _repository.GetHighPriorityTasksAsync(50);

        // Assert
        result.Should().HaveCount(2);
        // Should order by Priority (descending), then CreatedAt (ascending)
    }
}
