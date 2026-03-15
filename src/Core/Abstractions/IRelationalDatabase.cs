using System.Linq.Expressions;

namespace CKY.MultiAgentFramework.Core.Abstractions
{
    /// <summary>
    /// 关系数据库抽象接口
    /// 支持结构化数据持久化和事务管理
    /// </summary>
    /// <remarks>
    /// <para><b>默认推荐实现：</b>EfCoreRelationalDatabase</para>
    /// <para><b>支持的数据库：</b></para>
    /// <list type="bullet">
    ///   <item><b>SQLite</b>（默认）：零配置，文件数据库。适合 Demo 和单机部署。</item>
    ///   <item><b>PostgreSQL</b>（生产）：企业级数据库。支持高并发、事务、复制。</item>
    /// </list>
    /// <para><b>配置方式：</b></para>
    /// <list type="bullet">
    ///   <item>SQLite：无需配置（自动使用文件数据库）</item>
    ///   <item>PostgreSQL：在 appsettings.json 中配置 Provider: "PostgreSQL"</item>
    /// </list>
    /// <para><b>替代方案：</b>DapperPostgreSqlDatabase（轻量级 Dapper 实现）</para>
    /// </remarks>
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
