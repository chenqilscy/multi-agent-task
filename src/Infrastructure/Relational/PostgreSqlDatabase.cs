using System.Linq.Expressions;
using CKY.MultiAgentFramework.Core.Abstractions;
using Dapper;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace CKY.MultiAgentFramework.Infrastructure.Relational
{
    /// <summary>
    /// PostgreSQL数据库实现
    /// 使用 Npgsql + Dapper
    /// </summary>
    public class PostgreSqlDatabase : IRelationalDatabase
    {
        private readonly string _connectionString;
        private readonly ILogger<PostgreSqlDatabase> _logger;

        public PostgreSqlDatabase(string connectionString, ILogger<PostgreSqlDatabase> logger)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        private NpgsqlConnection CreateConnection()
            => new(_connectionString);

        /// <inheritdoc />
        public async Task<T?> GetByIdAsync<T>(object id, CancellationToken ct = default) where T : class
        {
            var tableName = GetTableName<T>();
            var sql = $"SELECT * FROM {tableName} WHERE id = @Id";

            try
            {
                await using var conn = CreateConnection();
                var result = await conn.QueryFirstOrDefaultAsync<T>(sql, new { Id = id });
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting {Type} by id {Id}", typeof(T).Name, id);
                throw;
            }
        }

        /// <inheritdoc />
        public Task<List<T>> GetListAsync<T>(
            Expression<Func<T, bool>>? predicate = null,
            CancellationToken ct = default) where T : class
        {
            // 注意：完整实现需要表达式树转SQL，这里提供基本骨架
            var tableName = GetTableName<T>();
            _logger.LogDebug("GetListAsync for table {TableName}", tableName);

            // 简化实现：返回空列表
            // 实际实现需要根据predicate生成WHERE子句
            return Task.FromResult(new List<T>());
        }

        /// <inheritdoc />
        public async Task<T> InsertAsync<T>(T entity, CancellationToken ct = default) where T : class
        {
            _logger.LogDebug("Inserting {Type}", typeof(T).Name);

            try
            {
                await using var conn = CreateConnection();
                // 简化实现：实际应该使用Dapper.Contrib或手动构建INSERT语句
                return entity;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inserting {Type}", typeof(T).Name);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task UpdateAsync<T>(T entity, CancellationToken ct = default) where T : class
        {
            _logger.LogDebug("Updating {Type}", typeof(T).Name);

            try
            {
                await using var conn = CreateConnection();
                // 简化实现：实际应该使用Dapper.Contrib或手动构建UPDATE语句
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating {Type}", typeof(T).Name);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task DeleteAsync<T>(T entity, CancellationToken ct = default) where T : class
        {
            _logger.LogDebug("Deleting {Type}", typeof(T).Name);

            try
            {
                await using var conn = CreateConnection();
                // 简化实现：实际应该使用Dapper.Contrib或手动构建DELETE语句
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting {Type}", typeof(T).Name);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task BulkInsertAsync<T>(IEnumerable<T> entities, CancellationToken ct = default) where T : class
        {
            _logger.LogDebug("Bulk inserting {Type}", typeof(T).Name);

            try
            {
                await using var conn = CreateConnection();
                await conn.OpenAsync(ct);
                await using var transaction = await conn.BeginTransactionAsync(ct);

                foreach (var entity in entities)
                {
                    await InsertAsync(entity, ct);
                }

                await transaction.CommitAsync(ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error bulk inserting {Type}", typeof(T).Name);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<TResult> ExecuteInTransactionAsync<TResult>(
            Func<Task<TResult>> action,
            CancellationToken ct = default)
        {
            await using var conn = CreateConnection();
            await conn.OpenAsync(ct);
            await using var transaction = await conn.BeginTransactionAsync(ct);

            try
            {
                var result = await action();
                await transaction.CommitAsync(ct);
                return result;
            }
            catch
            {
                await transaction.RollbackAsync(ct);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<List<TResult>> ExecuteSqlAsync<TResult>(
            string sql,
            object? parameters = null,
            CancellationToken ct = default)
        {
            try
            {
                await using var conn = CreateConnection();
                var results = await conn.QueryAsync<TResult>(sql, parameters);
                return results.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing SQL: {Sql}", sql);
                throw;
            }
        }

        private static string GetTableName<T>()
        {
            var type = typeof(T);
            // 将PascalCase转换为snake_case
            var name = type.Name;
            return string.Concat(name.Select((c, i) =>
                i > 0 && char.IsUpper(c) ? "_" + char.ToLower(c) : char.ToLower(c).ToString()));
        }
    }
}
