using CKY.MultiAgentFramework.Core.Enums;
using CKY.MultiAgentFramework.Core.Models.Task;
using CKY.MultiAgentFramework.Infrastructure.Repository.Data;
using CKY.MultiAgentFramework.Infrastructure.Repository.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CKY.MultiAgentFramework.IntegrationTests.Repository;

/// <summary>
/// MainTaskRepository 集成测试
/// 使用 SQLite 内存数据库验证 Repository 模式的 CRUD 和查询功能
/// </summary>
public class MainTaskRepositoryTests : IAsyncLifetime
{
    private MafDbContext _dbContext = null!;
    private MainTaskRepository _repository = null!;

    public async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<MafDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;

        _dbContext = new MafDbContext(options);
        await _dbContext.Database.OpenConnectionAsync();
        await _dbContext.Database.EnsureCreatedAsync();

        _repository = new MainTaskRepository(_dbContext);
    }

    public async Task DisposeAsync()
    {
        await _dbContext.Database.CloseConnectionAsync();
        await _dbContext.DisposeAsync();
    }

    [Fact]
    public async Task AddAsync_ShouldPersistAndReturnTask()
    {
        var task = new MainTask
        {
            Title = "集成测试任务",
            Description = "验证 Repository 持久化",
            Priority = TaskPriority.High,
            Status = MafTaskStatus.Pending
        };

        var result = await _repository.AddAsync(task);

        result.Id.Should().BeGreaterThan(0);
        result.Title.Should().Be("集成测试任务");
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task GetByIdAsync_ShouldIncludeSubTasks()
    {
        var task = new MainTask
        {
            Title = "带子任务的主任务",
            Priority = TaskPriority.Normal,
            SubTasks = new List<SubTask>
            {
                new() { Title = "子任务1", ExecutionOrder = 1, Status = MafTaskStatus.Pending },
                new() { Title = "子任务2", ExecutionOrder = 2, Status = MafTaskStatus.Pending }
            }
        };
        var inserted = await _repository.AddAsync(task);

        var found = await _repository.GetByIdAsync(inserted.Id);

        found.Should().NotBeNull();
        found!.SubTasks.Should().HaveCount(2);
        found.SubTasks.Should().Contain(s => s.Title == "子任务1");
        found.SubTasks.Should().Contain(s => s.Title == "子任务2");
    }

    [Fact]
    public async Task GetByIdAsync_NonExistent_ShouldReturnNull()
    {
        var found = await _repository.GetByIdAsync(99999);
        found.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnOrderedByCreatedAtDesc()
    {
        await _repository.AddAsync(new MainTask { Title = "旧任务", Priority = TaskPriority.Low });
        await Task.Delay(50);
        await _repository.AddAsync(new MainTask { Title = "新任务", Priority = TaskPriority.High });

        var all = await _repository.GetAllAsync();

        all.Should().HaveCountGreaterThanOrEqualTo(2);
        all.First().Title.Should().Be("新任务", "应按创建时间降序排列");
    }

    [Fact]
    public async Task GetByStatusAsync_ShouldFilterAndOrderByPriority()
    {
        await _repository.AddAsync(new MainTask
            { Title = "低优先级待处理", Priority = TaskPriority.Low, Status = MafTaskStatus.Pending });
        await _repository.AddAsync(new MainTask
            { Title = "高优先级待处理", Priority = TaskPriority.High, Status = MafTaskStatus.Pending });
        await _repository.AddAsync(new MainTask
            { Title = "已完成", Priority = TaskPriority.Critical, Status = MafTaskStatus.Completed });

        var pending = await _repository.GetByStatusAsync(MafTaskStatus.Pending);

        pending.Should().HaveCount(2);
        pending.Should().AllSatisfy(t => t.Status.Should().Be(MafTaskStatus.Pending));
        ((int)pending.First().Priority).Should().BeGreaterThanOrEqualTo((int)pending.Last().Priority,
            "应按优先级降序排列");
    }

    [Fact]
    public async Task GetHighPriorityTasksAsync_ShouldFilterByThreshold()
    {
        await _repository.AddAsync(new MainTask
            { Title = "Critical", Priority = TaskPriority.Critical, Status = MafTaskStatus.Pending });
        await _repository.AddAsync(new MainTask
            { Title = "High", Priority = TaskPriority.High, Status = MafTaskStatus.Pending });
        await _repository.AddAsync(new MainTask
            { Title = "Low", Priority = TaskPriority.Low, Status = MafTaskStatus.Pending });
        await _repository.AddAsync(new MainTask
            { Title = "已运行", Priority = TaskPriority.Critical, Status = MafTaskStatus.Running });

        // minPriority = 3 应匹配 High(3) 和 Critical(4)
        var highPriority = await _repository.GetHighPriorityTasksAsync(3);

        highPriority.Should().OnlyContain(t => t.Status == MafTaskStatus.Pending);
        highPriority.Should().OnlyContain(t => (int)t.Priority >= 3);
    }

    [Fact]
    public async Task UpdateAsync_ShouldModifyExistingTask()
    {
        var task = new MainTask { Title = "原始标题", Status = MafTaskStatus.Pending };
        var inserted = await _repository.AddAsync(task);

        inserted.Title = "更新后标题";
        inserted.Status = MafTaskStatus.Running;
        await _repository.UpdateAsync(inserted);

        var found = await _repository.GetByIdAsync(inserted.Id);
        found!.Title.Should().Be("更新后标题");
        found.Status.Should().Be(MafTaskStatus.Running);
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveTask()
    {
        var task = await _repository.AddAsync(new MainTask { Title = "待删除" });

        await _repository.DeleteAsync(task.Id);

        var found = await _repository.GetByIdAsync(task.Id);
        found.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_NonExistent_ShouldNotThrow()
    {
        var act = () => _repository.DeleteAsync(99999);
        await act.Should().NotThrowAsync();
    }
}
