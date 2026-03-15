using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Core.Enums;
using CKY.MultiAgentFramework.Core.Models.Task;
using CKY.MultiAgentFramework.Infrastructure.Repository.Data;
using CKY.MultiAgentFramework.Infrastructure.Repository.Relational;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CKY.MultiAgentFramework.IntegrationTests.Repository
{
    /// <summary>
    /// EfCoreRelationalDatabase 集成测试
    /// 使用 SQLite 内存数据库验证 CRUD、批量操作和事务功能
    /// </summary>
    public class EfCoreRelationalDatabaseTests : IAsyncLifetime
    {
        private MafDbContext _dbContext = null!;
        private IRelationalDatabase _database = null!;

        public async Task InitializeAsync()
        {
            var options = new DbContextOptionsBuilder<MafDbContext>()
                .UseSqlite("Data Source=:memory:")
                .Options;

            _dbContext = new MafDbContext(options);
            await _dbContext.Database.OpenConnectionAsync();
            await _dbContext.Database.EnsureCreatedAsync();

            _database = new EfCoreRelationalDatabase(
                _dbContext,
                NullLogger<EfCoreRelationalDatabase>.Instance);
        }

        public async Task DisposeAsync()
        {
            await _dbContext.Database.CloseConnectionAsync();
            await _dbContext.DisposeAsync();
        }

        [Fact]
        public async Task InsertAsync_ShouldPersistEntity()
        {
            // Arrange
            var task = new MainTask
            {
                Title = "Test Task",
                Description = "Integration test",
                Priority = TaskPriority.High,
                Status = MafTaskStatus.Pending,
            };

            // Act
            var inserted = await _database.InsertAsync(task);

            // Assert
            inserted.Id.Should().BeGreaterThan(0);
            inserted.Title.Should().Be("Test Task");
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnEntity()
        {
            // Arrange
            var task = new MainTask { Title = "Find Me", Priority = TaskPriority.Normal };
            var inserted = await _database.InsertAsync(task);

            // Act
            var found = await _database.GetByIdAsync<MainTask>(inserted.Id);

            // Assert
            found.Should().NotBeNull();
            found!.Title.Should().Be("Find Me");
        }

        [Fact]
        public async Task GetByIdAsync_NonExistent_ShouldReturnNull()
        {
            var found = await _database.GetByIdAsync<MainTask>(99999);
            found.Should().BeNull();
        }

        [Fact]
        public async Task UpdateAsync_ShouldModifyEntity()
        {
            // Arrange
            var task = new MainTask { Title = "Original", Status = MafTaskStatus.Pending };
            var inserted = await _database.InsertAsync(task);

            // Act
            inserted.Title = "Updated";
            inserted.Status = MafTaskStatus.Running;
            await _database.UpdateAsync(inserted);

            // Assert
            var found = await _database.GetByIdAsync<MainTask>(inserted.Id);
            found!.Title.Should().Be("Updated");
            found.Status.Should().Be(MafTaskStatus.Running);
        }

        [Fact]
        public async Task DeleteAsync_ShouldRemoveEntity()
        {
            // Arrange
            var task = new MainTask { Title = "Delete Me" };
            var inserted = await _database.InsertAsync(task);

            // Act
            await _database.DeleteAsync(inserted);

            // Assert
            var found = await _database.GetByIdAsync<MainTask>(inserted.Id);
            found.Should().BeNull();
        }

        [Fact]
        public async Task GetListAsync_WithPredicate_ShouldFilterResults()
        {
            // Arrange
            await _database.InsertAsync(new MainTask { Title = "High1", Priority = TaskPriority.High });
            await _database.InsertAsync(new MainTask { Title = "Low1", Priority = TaskPriority.Low });
            await _database.InsertAsync(new MainTask { Title = "High2", Priority = TaskPriority.High });

            // Act
            var highPriority = await _database.GetListAsync<MainTask>(t => t.Priority == TaskPriority.High);

            // Assert
            highPriority.Should().HaveCount(2);
            highPriority.Should().AllSatisfy(t => t.Priority.Should().Be(TaskPriority.High));
        }

        [Fact]
        public async Task GetListAsync_NoPredicate_ShouldReturnAll()
        {
            // Arrange
            await _database.InsertAsync(new MainTask { Title = "Task A" });
            await _database.InsertAsync(new MainTask { Title = "Task B" });

            // Act
            var all = await _database.GetListAsync<MainTask>();

            // Assert
            all.Should().HaveCountGreaterThanOrEqualTo(2);
        }

        [Fact]
        public async Task BulkInsertAsync_ShouldInsertMultipleEntities()
        {
            // Arrange
            var tasks = Enumerable.Range(1, 5).Select(i => new MainTask
            {
                Title = $"Bulk Task {i}",
                Priority = TaskPriority.Normal,
            }).ToList();

            // Act
            await _database.BulkInsertAsync(tasks);

            // Assert
            var all = await _database.GetListAsync<MainTask>(t => t.Title.StartsWith("Bulk Task"));
            all.Should().HaveCount(5);
        }

        [Fact]
        public async Task ExecuteInTransactionAsync_Success_ShouldCommit()
        {
            // Act
            var result = await _database.ExecuteInTransactionAsync(async () =>
            {
                var task1 = await _database.InsertAsync(new MainTask { Title = "Tx Task 1" });
                var task2 = await _database.InsertAsync(new MainTask { Title = "Tx Task 2" });
                return task1.Id + task2.Id;
            });

            // Assert
            result.Should().BeGreaterThan(0);
            var txTasks = await _database.GetListAsync<MainTask>(t => t.Title.StartsWith("Tx Task"));
            txTasks.Should().HaveCount(2);
        }

        [Fact]
        public async Task ExecuteInTransactionAsync_Failure_ShouldRollback()
        {
            // Arrange - insert a task before transaction
            await _database.InsertAsync(new MainTask { Title = "Pre-Tx Task" });

            // Act & Assert
            var act = () => _database.ExecuteInTransactionAsync<int>(async () =>
            {
                await _database.InsertAsync(new MainTask { Title = "Rollback Task" });
                throw new InvalidOperationException("Simulated failure");
            });

            await act.Should().ThrowAsync<InvalidOperationException>();

            // The "Rollback Task" should not be persisted
            var rollbackTasks = await _database.GetListAsync<MainTask>(t => t.Title == "Rollback Task");
            rollbackTasks.Should().BeEmpty("transaction should have been rolled back");
        }
    }
}
