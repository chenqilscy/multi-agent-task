// src/tests/UnitTests/Repository/SubTaskRepositoryTests.cs
using CKY.MultiAgentFramework.Core.Enums;
using CKY.MultiAgentFramework.Core.Models.Task;
using CKY.MultiAgentFramework.Repository.Repositories;
using CKY.MultiAgentFramework.Tests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace CKY.MultiAgentFramework.Tests.Repository;

public class SubTaskRepositoryTests : RepositoryTestBase
{
    private SubTaskRepository _repository
    {
        get
        {
            if (DbContext == null)
                throw new InvalidOperationException("DbContext not initialized. Call InitializeAsync first.");
            return new SubTaskRepository(DbContext);
        }
    }

    protected override async Task SeedTestDataAsync()
    {
        // 先创建一个 MainTask 用于测试
        var mainTask = TestDataBuilder.CreateMainTask(t => t.Title = "Parent Task");
        DbContext.MainTasks.Add(mainTask);
        await DbContext.SaveChangesAsync();
    }

    [Fact]
    public async Task AddAsync_ShouldAssignId()
    {
        // Arrange
        var mainTask = await DbContext.MainTasks.FirstAsync();
        var subTask = TestDataBuilder.CreateSubTask(s =>
        {
            s.Title = "New SubTask";
            s.MainTaskId = mainTask.Id;
        });

        // Act
        var result = await _repository.AddAsync(subTask);

        // Assert
        result.Id.Should().BeGreaterThan(0);
        result.Title.Should().Be("New SubTask");
    }

    [Fact]
    public async Task GetByIdAsync_ShouldIncludeMainTask()
    {
        // Arrange
        var mainTask = await DbContext.MainTasks.FirstAsync();
        var subTask = TestDataBuilder.CreateSubTask(s =>
        {
            s.Title = "Test SubTask";
            s.MainTaskId = mainTask.Id;
        });
        await _repository.AddAsync(subTask);

        // Act
        var result = await _repository.GetByIdAsync(subTask.Id);

        // Assert
        result.Should().NotBeNull();
        result!.MainTask.Should().NotBeNull();
        result.MainTask.Title.Should().Be("Parent Task");
    }

    [Fact]
    public async Task GetByMainTaskIdAsync_ShouldReturnSubTasksOrderedByExecutionOrder()
    {
        // Arrange
        var mainTask = await DbContext.MainTasks.FirstAsync();
        var subTask1 = TestDataBuilder.CreateSubTask(s =>
        {
            s.Title = "First";
            s.MainTaskId = mainTask.Id;
            s.ExecutionOrder = 1;
        });
        var subTask2 = TestDataBuilder.CreateSubTask(s =>
        {
            s.Title = "Second";
            s.MainTaskId = mainTask.Id;
            s.ExecutionOrder = 2;
        });
        await _repository.AddAsync(subTask2); // 故意乱序添加
        await _repository.AddAsync(subTask1);

        // Act
        var result = await _repository.GetByMainTaskIdAsync(mainTask.Id);

        // Assert
        result.Should().HaveCount(2);
        result[0].Title.Should().Be("First");
        result[1].Title.Should().Be("Second");
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllSubTasks()
    {
        // Arrange
        var mainTask = await DbContext.MainTasks.FirstAsync();
        var subTask1 = TestDataBuilder.CreateSubTask(s =>
        {
            s.Title = "SubTask 1";
            s.MainTaskId = mainTask.Id;
        });
        var subTask2 = TestDataBuilder.CreateSubTask(s =>
        {
            s.Title = "SubTask 2";
            s.MainTaskId = mainTask.Id;
        });
        await _repository.AddAsync(subTask1);
        await _repository.AddAsync(subTask2);

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        result.Should().HaveCountGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task AddRangeAsync_ShouldAddMultipleSubTasks()
    {
        // Arrange
        var mainTask = await DbContext.MainTasks.FirstAsync();
        var subTasks = new List<SubTask>
        {
            TestDataBuilder.CreateSubTask(s => { s.Title = "Batch 1"; s.MainTaskId = mainTask.Id; }),
            TestDataBuilder.CreateSubTask(s => { s.Title = "Batch 2"; s.MainTaskId = mainTask.Id; }),
            TestDataBuilder.CreateSubTask(s => { s.Title = "Batch 3"; s.MainTaskId = mainTask.Id; })
        };

        // Act
        var result = await _repository.AddRangeAsync(subTasks);

        // Assert
        result.Should().HaveCount(3);
        result.All(r => r.Id > 0).Should().BeTrue();
    }

    [Fact]
    public async Task UpdateAsync_ShouldModifySubTask()
    {
        // Arrange
        var mainTask = await DbContext.MainTasks.FirstAsync();
        var subTask = TestDataBuilder.CreateSubTask(s =>
        {
            s.Title = "Original Title";
            s.MainTaskId = mainTask.Id;
        });
        await _repository.AddAsync(subTask);

        // Act
        subTask.Title = "Updated Title";
        await _repository.UpdateAsync(subTask);

        // Assert
        var result = await _repository.GetByIdAsync(subTask.Id);
        result!.Title.Should().Be("Updated Title");
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveSubTask()
    {
        // Arrange
        var mainTask = await DbContext.MainTasks.FirstAsync();
        var subTask = TestDataBuilder.CreateSubTask(s =>
        {
            s.Title = "To Delete";
            s.MainTaskId = mainTask.Id;
        });
        await _repository.AddAsync(subTask);

        // Act
        await _repository.DeleteAsync(subTask.Id);

        // Assert
        var result = await _repository.GetByIdAsync(subTask.Id);
        result.Should().BeNull();
    }

    [Fact]
    public async Task AddRangeAsync_WithExecutionOrder_ShouldPreserveOrder()
    {
        // Arrange
        var mainTask = await DbContext.MainTasks.FirstAsync();
        var subTasks = new List<SubTask>
        {
            TestDataBuilder.CreateSubTask(s =>
            {
                s.Title = "Task 1";
                s.MainTaskId = mainTask.Id;
                s.ExecutionOrder = 3;
            }),
            TestDataBuilder.CreateSubTask(s =>
            {
                s.Title = "Task 2";
                s.MainTaskId = mainTask.Id;
                s.ExecutionOrder = 1;
            }),
            TestDataBuilder.CreateSubTask(s =>
            {
                s.Title = "Task 3";
                s.MainTaskId = mainTask.Id;
                s.ExecutionOrder = 2;
            })
        };

        // Act
        await _repository.AddRangeAsync(subTasks);
        var retrieved = await _repository.GetByMainTaskIdAsync(mainTask.Id);

        // Assert
        retrieved.Should().HaveCount(3);
        retrieved[0].ExecutionOrder.Should().Be(1); // Should be ordered by ExecutionOrder
        retrieved[1].ExecutionOrder.Should().Be(2);
        retrieved[2].ExecutionOrder.Should().Be(3);
    }
}
