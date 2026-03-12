using System.Linq.Expressions;

namespace CKY.MultiAgentFramework.Core.Abstractions
{
    /// <summary>
    /// 关系数据库抽象接口
    /// 支持结构化数据持久化和事务管理
    /// </summary>
    public interface IRelationalDatabase
    {
        /// <summary>
        /// 查询单个实体
        /// </summary>
        Task<T?> GetByIdAsync<T>(
            object id,
            CancellationToken ct = default) where T : class;

        /// <summary>
        /// 查询列表
        /// </summary>
        Task<List<T>> GetListAsync<T>(
            Expression<Func<T, bool>>? predicate = null,
            CancellationToken ct = default) where T : class;

        /// <summary>
        /// 插入实体
        /// </summary>
        Task<T> InsertAsync<T>(
            T entity,
            CancellationToken ct = default) where T : class;

        /// <summary>
        /// 更新实体
        /// </summary>
        Task UpdateAsync<T>(
            T entity,
            CancellationToken ct = default) where T : class;

        /// <summary>
        /// 删除实体
        /// </summary>
        Task DeleteAsync<T>(
            T entity,
            CancellationToken ct = default) where T : class;

        /// <summary>
        /// 批量插入
        /// </summary>
        Task BulkInsertAsync<T>(
            IEnumerable<T> entities,
            CancellationToken ct = default) where T : class;

        /// <summary>
        /// 执行事务
        /// </summary>
        Task<TResult> ExecuteInTransactionAsync<TResult>(
            Func<Task<TResult>> action,
            CancellationToken ct = default);

        /// <summary>
        /// 执行原生SQL查询
        /// </summary>
        Task<List<TResult>> ExecuteSqlAsync<TResult>(
            string sql,
            object? parameters = null,
            CancellationToken ct = default);
    }
}
