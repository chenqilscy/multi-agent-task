using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Core.Enums;
using CKY.MultiAgentFramework.Core.Models.Task;
using CKY.MultiAgentFramework.Infrastructure.Repository.Data;
using CKY.MultiAgentFramework.Infrastructure.Repository.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CKY.MultiAgentFramework.IntegrationTests.Repository;

/// <summary>
/// UnitOfWork 事务集成测试
/// 验证跨 Repository 的事务操作、提交和回滚
/// </summary>
public class UnitOfWorkTests : IAsyncLifetime
{
    private MafDbContext _dbContext = null!;
    private UnitOfWork _unitOfWork = null!;

    public async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<MafDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;

        _dbContext = new MafDbContext(options);
        await _dbContext.Database.OpenConnectionAsync();
        await _dbContext.Database.EnsureCreatedAsync();

        _unitOfWork = new UnitOfWork(_dbContext);
    }

    public async Task DisposeAsync()
    {
        // UnitOfWork.Dispose() 会释放 _context，所以先关闭连接
        await _dbContext.Database.CloseConnectionAsync();
        _unitOfWork.Dispose();
    }

    [Fact]
    public void MainTasks_ShouldReturnRepository()
    {
        _unitOfWork.MainTasks.Should().NotBeNull();
        _unitOfWork.MainTasks.Should().BeAssignableTo<IMainTaskRepository>();
    }

    [Fact]
    public void SubTasks_ShouldReturnRepository()
    {
        _unitOfWork.SubTasks.Should().NotBeNull();
        _unitOfWork.SubTasks.Should().BeAssignableTo<ISubTaskRepository>();
    }

    [Fact]
    public async Task SaveChangesAsync_ShouldPersistChanges()
    {
        _dbContext.MainTasks.Add(new MainTask { Title = "直接添加", Priority = TaskPriority.Normal });

        var count = await _unitOfWork.SaveChangesAsync();

        count.Should().BeGreaterThan(0);

        var tasks = await _dbContext.MainTasks.ToListAsync();
        tasks.Should().ContainSingle(t => t.Title == "直接添加");
    }

    [Fact]
    public async Task Transaction_CommitAsync_ShouldPersistAll()
    {
        await _unitOfWork.BeginTransactionAsync();

        var mainTask = await _unitOfWork.MainTasks.AddAsync(new MainTask
        {
            Title = "事务测试主任务",
            Priority = TaskPriority.High,
            Status = MafTaskStatus.Pending
        });

        await _unitOfWork.SubTasks.AddAsync(new SubTask
        {
            MainTaskId = mainTask.Id,
            Title = "事务测试子任务",
            ExecutionOrder = 1
        });

        await _unitOfWork.CommitTransactionAsync();

        // 验证两个实体都已持久化
        var found = await _unitOfWork.MainTasks.GetByIdAsync(mainTask.Id);
        found.Should().NotBeNull();
        found!.SubTasks.Should().HaveCount(1);
    }

    [Fact]
    public async Task Transaction_RollbackAsync_ShouldDiscardChanges()
    {
        // 先插入一个基线任务
        await _unitOfWork.MainTasks.AddAsync(new MainTask { Title = "基线任务" });

        await _unitOfWork.BeginTransactionAsync();

        await _unitOfWork.MainTasks.AddAsync(new MainTask
        {
            Title = "回滚测试任务",
            Priority = TaskPriority.Critical
        });

        await _unitOfWork.RollbackTransactionAsync();

        var all = await _unitOfWork.MainTasks.GetAllAsync();
        all.Should().NotContain(t => t.Title == "回滚测试任务",
            "回滚后不应包含事务中添加的任务");
    }

    [Fact]
    public async Task CrossRepository_Transaction_ShouldBeAtomic()
    {
        await _unitOfWork.BeginTransactionAsync();

        // 通过 MainTasks repository 添加
        var mainTask = await _unitOfWork.MainTasks.AddAsync(new MainTask
        {
            Title = "跨仓库事务任务"
        });

        // 通过 SubTasks repository 添加
        await _unitOfWork.SubTasks.AddRangeAsync(new List<SubTask>
        {
            new() { MainTaskId = mainTask.Id, Title = "原子子任务1", ExecutionOrder = 1 },
            new() { MainTaskId = mainTask.Id, Title = "原子子任务2", ExecutionOrder = 2 }
        });

        await _unitOfWork.CommitTransactionAsync();

        // 验证跨 Repository 的原子性
        var subTasks = await _unitOfWork.SubTasks.GetByMainTaskIdAsync(mainTask.Id);
        subTasks.Should().HaveCount(2);
    }

    [Fact]
    public async Task CommitWithoutBeginTransaction_ShouldNotThrow()
    {
        var act = () => _unitOfWork.CommitTransactionAsync();
        await act.Should().NotThrowAsync("未开始事务时提交不应抛异常");
    }

    [Fact]
    public async Task RollbackWithoutBeginTransaction_ShouldNotThrow()
    {
        var act = () => _unitOfWork.RollbackTransactionAsync();
        await act.Should().NotThrowAsync("未开始事务时回滚不应抛异常");
    }
}
