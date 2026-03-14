namespace CKY.MultiAgentFramework.Core.Abstractions
{
    /// <summary>
    /// 工作单元接口
    /// 管理事务和变更保存
    /// </summary>
    public interface IUnitOfWork : IDisposable
    {
        /// <summary>
        /// 主任务仓储
        /// </summary>
        IMainTaskRepository MainTasks { get; }

        /// <summary>
        /// 子任务仓储
        /// </summary>
        ISubTaskRepository SubTasks { get; }

        /// <summary>
        /// 保存所有更改
        /// </summary>
        Task<int> SaveChangesAsync(CancellationToken ct = default);

        /// <summary>
        /// 开始事务
        /// </summary>
        Task BeginTransactionAsync(CancellationToken ct = default);

        /// <summary>
        /// 提交事务
        /// </summary>
        Task CommitTransactionAsync(CancellationToken ct = default);

        /// <summary>
        /// 回滚事务
        /// </summary>
        Task RollbackTransactionAsync(CancellationToken ct = default);
    }
}
