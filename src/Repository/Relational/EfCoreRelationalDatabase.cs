using System.Linq.Expressions;
using CKY.MultiAgentFramework.Core.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace CKY.MultiAgentFramework.Repository.Relational
{
    /// <summary>
    /// EF Core 关系数据库实现
    /// 提供结构化数据持久化和事务管理
    /// </summary>
    public class EfCoreRelationalDatabase : IRelationalDatabase
    {
        private readonly DbContext _context;
        private readonly ILogger<EfCoreRelationalDatabase> _logger;

        public EfCoreRelationalDatabase(
            DbContext context,
            ILogger<EfCoreRelationalDatabase> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public async Task<T?> GetByIdAsync<T>(
            object id,
            CancellationToken ct = default) where T : class
        {
            _logger.LogDebug("Getting entity {EntityType} by ID {Id}", typeof(T).Name, id);
            return await _context.FindAsync<T>(new[] { id }, ct);
        }

        /// <inheritdoc />
        public async Task<List<T>> GetListAsync<T>(
            Expression<Func<T, bool>>? predicate = null,
            CancellationToken ct = default) where T : class
        {
            var query = _context.Set<T>().AsQueryable();

            if (predicate != null)
            {
                query = query.Where(predicate);
            }

            return await query.ToListAsync(ct);
        }

        /// <inheritdoc />
        public async Task<T> InsertAsync<T>(
            T entity,
            CancellationToken ct = default) where T : class
        {
            _logger.LogDebug("Inserting entity {EntityType}", typeof(T).Name);

            var entry = await _context.AddAsync(entity, ct);
            await _context.SaveChangesAsync(ct);

            _logger.LogDebug("Inserted entity {EntityType} with ID {Id}",
                typeof(T).Name, GetEntityId(entry.Entity));

            return entry.Entity;
        }

        /// <inheritdoc />
        public async Task UpdateAsync<T>(
            T entity,
            CancellationToken ct = default) where T : class
        {
            _logger.LogDebug("Updating entity {EntityType}", typeof(T).Name);

            _context.Update(entity);
            await _context.SaveChangesAsync(ct);

            _logger.LogDebug("Updated entity {EntityType} with ID {Id}",
                typeof(T).Name, GetEntityId(entity));
        }

        /// <inheritdoc />
        public async Task DeleteAsync<T>(
            T entity,
            CancellationToken ct = default) where T : class
        {
            _logger.LogDebug("Deleting entity {EntityType}", typeof(T).Name);

            _context.Remove(entity);
            await _context.SaveChangesAsync(ct);

            _logger.LogDebug("Deleted entity {EntityType}", typeof(T).Name);
        }

        /// <inheritdoc />
        public async Task BulkInsertAsync<T>(
            IEnumerable<T> entities,
            CancellationToken ct = default) where T : class
        {
            var entityList = entities.ToList();
            _logger.LogDebug("Bulk inserting {Count} entities of type {EntityType}",
                entityList.Count, typeof(T).Name);

            await _context.AddRangeAsync(entityList, ct);
            await _context.SaveChangesAsync(ct);

            _logger.LogDebug("Bulk inserted {Count} entities of type {EntityType}",
                entityList.Count, typeof(T).Name);
        }

        /// <inheritdoc />
        public async Task<TResult> ExecuteInTransactionAsync<TResult>(
            Func<Task<TResult>> action,
            CancellationToken ct = default)
        {
            _logger.LogDebug("Starting transaction");

            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync(ct);
                try
                {
                    var result = await action();
                    await transaction.CommitAsync(ct);
                    _logger.LogDebug("Transaction committed successfully");
                    return result;
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync(ct);
                    _logger.LogError(ex, "Transaction failed and rolled back");
                    throw;
                }
            });
        }

        /// <inheritdoc />
        public Task<List<TResult>> ExecuteSqlAsync<TResult>(
            string sql,
            object? parameters = null,
            CancellationToken ct = default)
        {
            // 不支持原生 SQL 执行，使用 LINQ 查询代替
            throw new NotSupportedException(
                "原生 SQL 执行不支持。请使用 GetListAsync<T>() 方法和 LINQ 表达式进行查询。" +
                "如需复杂查询，请考虑使用特定数据库提供程序的功能。");
        }

        /// <summary>
        /// 获取实体ID（用于日志记录）
        /// </summary>
        private static object? GetEntityId<T>(T entity) where T : class
        {
            var property = typeof(T).GetProperty("Id");
            return property?.GetValue(entity);
        }
    }
}
