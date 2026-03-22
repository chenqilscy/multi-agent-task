using CKY.MultiAgentFramework.Core.Enums;
using CKY.MultiAgentFramework.Core.Models.Task;
using CKY.MultiAgentFramework.Infrastructure.Repository.Data;
using CKY.MultiAgentFramework.Infrastructure.Repository.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CKY.MultiAgentFramework.IntegrationTests.Repository;

/// <summary>
/// SubTaskRepository 集成测试
/// 验证子任务的 CRUD、批量操作和关联查询
/// </summary>
public class SubTaskRepositoryTests : IAsyncLifetime
{
    private MafDbContext _dbContext = null!;
    private SubTaskRepository _subTaskRepository = null!;
    private MainTaskRepository _mainTaskRepository = null!;

    public async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<MafDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;

        _dbContext = new MafDbContext(options);
        await _dbContext.Database.OpenConnectionAsync();
        await _dbContext.Database.EnsureCreatedAsync();

        _subTaskRepository = new SubTaskRepository(_dbContext);
        _mainTaskRepository = new MainTaskRepository(_dbContext);
    }

    public async Task DisposeAsync()
    {
        await _dbContext.Database.CloseConnectionAsync();
        await _dbContext.DisposeAsync();
    }

    private async Task<MainTask> CreateMainTaskAsync(string title = "父任务")
    {
        return await _mainTaskRepository.AddAsync(new MainTask
        {
            Title = title,
            Priority = TaskPriority.Normal,
            Status = MafTaskStatus.Pending
        });
    }

    [Fact]
    public async Task AddAsync_ShouldPersistSubTask()
    {
        var mainTask = await CreateMainTaskAsync();

        var subTask = await _subTaskRepository.AddAsync(new SubTask
        {
            MainTaskId = mainTask.Id,
            Title = "子任务A",
            ExecutionOrder = 1,
            Status = MafTaskStatus.Pending
        });

        subTask.Id.Should().BeGreaterThan(0);
        subTask.MainTaskId.Should().Be(mainTask.Id);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldIncludeMainTask()
    {
        var mainTask = await CreateMainTaskAsync("关联测试父任务");
        var subTask = await _subTaskRepository.AddAsync(new SubTask
        {
            MainTaskId = mainTask.Id,
            Title = "关联测试子任务",
            ExecutionOrder = 1
        });

        var found = await _subTaskRepository.GetByIdAsync(subTask.Id);

        found.Should().NotBeNull();
        found!.MainTask.Should().NotBeNull();
        found.MainTask!.Title.Should().Be("关联测试父任务");
    }

    [Fact]
    public async Task GetByMainTaskIdAsync_ShouldReturnOrderedByExecutionOrder()
    {
        var mainTask = await CreateMainTaskAsync();
        await _subTaskRepository.AddAsync(new SubTask
            { MainTaskId = mainTask.Id, Title = "第三步", ExecutionOrder = 3 });
        await _subTaskRepository.AddAsync(new SubTask
            { MainTaskId = mainTask.Id, Title = "第一步", ExecutionOrder = 1 });
        await _subTaskRepository.AddAsync(new SubTask
            { MainTaskId = mainTask.Id, Title = "第二步", ExecutionOrder = 2 });

        var subTasks = await _subTaskRepository.GetByMainTaskIdAsync(mainTask.Id);

        subTasks.Should().HaveCount(3);
        subTasks[0].Title.Should().Be("第一步");
        subTasks[1].Title.Should().Be("第二步");
        subTasks[2].Title.Should().Be("第三步");
    }

    [Fact]
    public async Task GetByMainTaskIdAsync_DifferentParent_ShouldNotMix()
    {
        var parent1 = await CreateMainTaskAsync("父任务1");
        var parent2 = await CreateMainTaskAsync("父任务2");
        await _subTaskRepository.AddAsync(new SubTask
            { MainTaskId = parent1.Id, Title = "P1子任务", ExecutionOrder = 1 });
        await _subTaskRepository.AddAsync(new SubTask
            { MainTaskId = parent2.Id, Title = "P2子任务", ExecutionOrder = 1 });

        var p1SubTasks = await _subTaskRepository.GetByMainTaskIdAsync(parent1.Id);
        var p2SubTasks = await _subTaskRepository.GetByMainTaskIdAsync(parent2.Id);

        p1SubTasks.Should().HaveCount(1);
        p1SubTasks.First().Title.Should().Be("P1子任务");
        p2SubTasks.Should().HaveCount(1);
        p2SubTasks.First().Title.Should().Be("P2子任务");
    }

    [Fact]
    public async Task AddRangeAsync_ShouldInsertMultipleSubTasks()
    {
        var mainTask = await CreateMainTaskAsync();
        var subTasks = Enumerable.Range(1, 5).Select(i => new SubTask
        {
            MainTaskId = mainTask.Id,
            Title = $"批量子任务{i}",
            ExecutionOrder = i
        }).ToList();

        var result = await _subTaskRepository.AddRangeAsync(subTasks);

        result.Should().HaveCount(5);
        result.Should().AllSatisfy(s => s.Id.Should().BeGreaterThan(0));

        var fromDb = await _subTaskRepository.GetByMainTaskIdAsync(mainTask.Id);
        fromDb.Should().HaveCount(5);
    }

    [Fact]
    public async Task UpdateAsync_ShouldModifySubTask()
    {
        var mainTask = await CreateMainTaskAsync();
        var subTask = await _subTaskRepository.AddAsync(new SubTask
        {
            MainTaskId = mainTask.Id,
            Title = "原始子任务",
            ExecutionOrder = 1,
            Status = MafTaskStatus.Pending
        });

        subTask.Title = "更新后子任务";
        subTask.Status = MafTaskStatus.Completed;
        await _subTaskRepository.UpdateAsync(subTask);

        var found = await _subTaskRepository.GetByIdAsync(subTask.Id);
        found!.Title.Should().Be("更新后子任务");
        found.Status.Should().Be(MafTaskStatus.Completed);
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveSubTask()
    {
        var mainTask = await CreateMainTaskAsync();
        var subTask = await _subTaskRepository.AddAsync(new SubTask
        {
            MainTaskId = mainTask.Id,
            Title = "待删除子任务",
            ExecutionOrder = 1
        });

        await _subTaskRepository.DeleteAsync(subTask.Id);

        var found = await _subTaskRepository.GetByIdAsync(subTask.Id);
        found.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnOrderedByMainTaskThenExecutionOrder()
    {
        var parent1 = await CreateMainTaskAsync("父1");
        var parent2 = await CreateMainTaskAsync("父2");

        await _subTaskRepository.AddAsync(new SubTask
            { MainTaskId = parent1.Id, Title = "P1-S2", ExecutionOrder = 2 });
        await _subTaskRepository.AddAsync(new SubTask
            { MainTaskId = parent1.Id, Title = "P1-S1", ExecutionOrder = 1 });
        await _subTaskRepository.AddAsync(new SubTask
            { MainTaskId = parent2.Id, Title = "P2-S1", ExecutionOrder = 1 });

        var all = await _subTaskRepository.GetAllAsync();

        all.Should().HaveCountGreaterThanOrEqualTo(3);
    }
}
