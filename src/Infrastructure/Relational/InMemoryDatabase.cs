using System.Collections.Concurrent;
using System.Linq.Expressions;
using CKY.MultiAgentFramework.Core.Abstractions;

namespace CKY.MultiAgentFramework.Infrastructure.Relational
{
    /// <summary>
    /// 内存数据库实现（用于开发和测试）
    /// </summary>
    public class InMemoryDatabase : IRelationalDatabase
    {
        private readonly ConcurrentDictionary<Type, ConcurrentDictionary<string, object>> _store = new();

        private ConcurrentDictionary<string, object> GetTable<T>()
            => _store.GetOrAdd(typeof(T), _ => new ConcurrentDictionary<string, object>());

        private static string GetKey(object entity)
        {
            var idProp = entity.GetType().GetProperty("Id")
                ?? entity.GetType().GetProperty($"{entity.GetType().Name}Id")
                ?? entity.GetType().GetProperties().FirstOrDefault(p =>
                    p.Name.EndsWith("Id", StringComparison.OrdinalIgnoreCase));

            return idProp?.GetValue(entity)?.ToString() ?? Guid.NewGuid().ToString();
        }

        /// <inheritdoc />
        public Task<T?> GetByIdAsync<T>(object id, CancellationToken ct = default) where T : class
        {
            var table = GetTable<T>();
            if (table.TryGetValue(id.ToString()!, out var entity))
            {
                return Task.FromResult((T?)entity);
            }
            return Task.FromResult<T?>(null);
        }

        /// <inheritdoc />
        public Task<List<T>> GetListAsync<T>(
            Expression<Func<T, bool>>? predicate = null,
            CancellationToken ct = default) where T : class
        {
            var table = GetTable<T>();
            var items = table.Values.Cast<T>();

            if (predicate != null)
            {
                items = items.Where(predicate.Compile());
            }

            return Task.FromResult(items.ToList());
        }

        /// <inheritdoc />
        public Task<T> InsertAsync<T>(T entity, CancellationToken ct = default) where T : class
        {
            var table = GetTable<T>();
            var key = GetKey(entity);
            table[key] = entity;
            return Task.FromResult(entity);
        }

        /// <inheritdoc />
        public Task UpdateAsync<T>(T entity, CancellationToken ct = default) where T : class
        {
            var table = GetTable<T>();
            var key = GetKey(entity);
            table[key] = entity;
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task DeleteAsync<T>(T entity, CancellationToken ct = default) where T : class
        {
            var table = GetTable<T>();
            var key = GetKey(entity);
            table.TryRemove(key, out _);
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public async Task BulkInsertAsync<T>(IEnumerable<T> entities, CancellationToken ct = default) where T : class
        {
            foreach (var entity in entities)
            {
                await InsertAsync(entity, ct);
            }
        }

        /// <inheritdoc />
        public async Task<TResult> ExecuteInTransactionAsync<TResult>(
            Func<Task<TResult>> action,
            CancellationToken ct = default)
        {
            // 内存实现不需要真正的事务
            return await action();
        }

        /// <inheritdoc />
        public Task<List<TResult>> ExecuteSqlAsync<TResult>(
            string sql,
            object? parameters = null,
            CancellationToken ct = default)
        {
            // 内存实现不支持SQL查询
            return Task.FromResult(new List<TResult>());
        }
    }
}
