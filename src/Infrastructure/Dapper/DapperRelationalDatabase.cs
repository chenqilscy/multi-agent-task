using System.Data;
using System.Reflection;
using System.Linq.Expressions;
using Dapper;
using CKY.MultiAgentFramework.Core.Abstractions;
using Microsoft.Extensions.Logging;

namespace CKY.MultiAgentFramework.Infrastructure.Dapper;

/// <summary>
/// 基于 Dapper 的关系数据库实现
/// 提供高性能、轻量级的数据访问，无 Change Tracking 开销
/// </summary>
/// <remarks>
/// 特性：
/// - 高性能：接近原生 ADO.NET
/// - 轻量级：无 Change Tracking 开销
/// - SQL 完全可控：所有 SQL 都是手写的，避免隐藏查询
/// - 业务层解耦：业务实体是纯 POCO，不依赖任何 ORM
///
/// 使用场景：
/// - MAF 框架层（Agent Session、Task 等框架实体）
/// - 需要高性能的业务场景
/// - SQL 复杂查询场景
/// </remarks>
public sealed class DapperRelationalDatabase : IRelationalDatabase
{
    private readonly IDbConnection _connection;
    private readonly ILogger<DapperRelationalDatabase> _logger;

    public DapperRelationalDatabase(
        IDbConnection connection,
        ILogger<DapperRelationalDatabase> logger)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<T?> GetByIdAsync<T>(
        object id,
        CancellationToken ct = default) where T : class
    {
        var (tableName, schema) = GetTableName<T>();
        var sql = schema is null
            ? $"SELECT * FROM {tableName} WHERE Id = @Id"
            : $"SELECT * FROM {schema}.{tableName} WHERE Id = @Id";

        _logger.LogDebug("Executing SQL: {Sql}", sql);

        return await _connection.QueryFirstOrDefaultAsync<T>(
            new CommandDefinition(sql, new { Id = id }, cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task<List<T>> GetListAsync<T>(
        Expression<Func<T, bool>>? predicate = null,
        CancellationToken ct = default) where T : class
    {
        var (tableName, schema) = GetTableName<T>();
        var sql = schema is null
            ? $"SELECT * FROM {tableName}"
            : $"SELECT * FROM {schema}.{tableName}";

        // 注意：Dapper 不支持直接解析 Expression，这里简化处理
        // 如果需要复杂查询，建议使用 ExecuteSqlAsync 直接写 SQL
        if (predicate is not null)
        {
            _logger.LogWarning("Dapper implementation does not support Expression predicates. Use ExecuteSqlAsync for complex queries.");
            throw new NotSupportedException(
                "Dapper implementation does not support Expression predicates. " +
                "Use ExecuteSqlAsync for complex queries.");
        }

        _logger.LogDebug("Executing SQL: {Sql}", sql);

        var results = await _connection.QueryAsync<T>(
            new CommandDefinition(sql, cancellationToken: ct));
        return results.ToList();
    }

    /// <inheritdoc />
    public async Task<T> InsertAsync<T>(
        T entity,
        CancellationToken ct = default) where T : class
    {
        var (tableName, schema) = GetTableName<T>();
        var properties = typeof(T).GetProperties()
            .Where(p => p.Name != "Id" && p.CanWrite);

        var columnNames = string.Join(", ", properties.Select(p => p.Name));
        var paramNames = string.Join(", ", properties.Select(p => $"@{p.Name}"));

        var qualifiedTable = schema is null ? tableName : $"{schema}.{tableName}";
        var sql = $"INSERT INTO {qualifiedTable} ({columnNames}) VALUES ({paramNames}) " +
                  $"RETURNING *;"; // SQLite/PostgreSQL 支持返回插入的行

        _logger.LogDebug("Executing SQL: {Sql}", sql);

        var result = await _connection.QueryFirstOrDefaultAsync<T>(
            new CommandDefinition(sql, entity, cancellationToken: ct));

        return result ?? entity;
    }

    /// <inheritdoc />
    public async Task UpdateAsync<T>(
        T entity,
        CancellationToken ct = default) where T : class
    {
        var (tableName, schema) = GetTableName<T>();
        var properties = typeof(T).GetProperties()
            .Where(p => p.Name != "Id" && p.CanWrite);

        var setClause = string.Join(", ", properties.Select(p => $"{p.Name} = @{p.Name}"));

        var qualifiedTable = schema is null ? tableName : $"{schema}.{tableName}";
        var sql = $"UPDATE {qualifiedTable} SET {setClause} WHERE Id = @Id";

        _logger.LogDebug("Executing SQL: {Sql}", sql);

        await _connection.ExecuteAsync(
            new CommandDefinition(sql, entity, cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task DeleteAsync<T>(
        T entity,
        CancellationToken ct = default) where T : class
    {
        var (tableName, schema) = GetTableName<T>();
        var qualifiedTable = schema is null ? tableName : $"{schema}.{tableName}";
        var sql = $"DELETE FROM {qualifiedTable} WHERE Id = @Id";

        _logger.LogDebug("Executing SQL: {Sql}", sql);

        await _connection.ExecuteAsync(
            new CommandDefinition(sql, entity, cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task BulkInsertAsync<T>(
        IEnumerable<T> entities,
        CancellationToken ct = default) where T : class
    {
        var (tableName, schema) = GetTableName<T>();
        var properties = typeof(T).GetProperties()
            .Where(p => p.Name != "Id" && p.CanWrite);

        var columnNames = string.Join(", ", properties.Select(p => p.Name));
        var paramNames = string.Join(", ", properties.Select(p => $"@{p.Name}"));

        var qualifiedTable = schema is null ? tableName : $"{schema}.{tableName}";
        var sql = $"INSERT INTO {qualifiedTable} ({columnNames}) VALUES ({paramNames})";

        _logger.LogDebug("Executing Bulk Insert SQL: {Sql}", sql);

        await _connection.ExecuteAsync(
            new CommandDefinition(sql, entities, cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task<TResult> ExecuteInTransactionAsync<TResult>(
        Func<Task<TResult>> action,
        CancellationToken ct = default)
    {
        // Dapper 使用 IDbConnection 的事务支持
        var transaction = _connection.BeginTransaction();

        try
        {
            var result = await action();
            transaction.Commit();
            return result;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
        finally
        {
            transaction.Dispose();
        }
    }

    /// <inheritdoc />
    public async Task<List<TResult>> ExecuteSqlAsync<TResult>(
        string sql,
        object? parameters = null,
        CancellationToken ct = default)
    {
        _logger.LogDebug("Executing SQL: {Sql}", sql);

        var results = await _connection.QueryAsync<TResult>(
            new CommandDefinition(sql, parameters, cancellationToken: ct));
        return results.ToList();
    }

    /// <summary>
    /// 获取实体对应的表名和 Schema
    /// </summary>
    private static (string TableName, string? Schema) GetTableName<T>()
    {
        var tableAttr = typeof(T).GetCustomAttribute<TableAttribute>();

        if (tableAttr is not null)
        {
            return (tableAttr.Name, tableAttr.Schema);
        }

        // 默认约定：使用类名作为表名
        return (typeof(T).Name, null);
    }
}
