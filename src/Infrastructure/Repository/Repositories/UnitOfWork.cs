using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Infrastructure.Repository.Data;
using Microsoft.EntityFrameworkCore.Storage;

namespace CKY.MultiAgentFramework.Infrastructure.Repository.Repositories
{
    /// <summary>
    /// 工作单元实现
    /// </summary>
    public class UnitOfWork : IUnitOfWork
    {
        private readonly MafDbContext _context;
        private IDbContextTransaction? _transaction;
        private readonly IMainTaskRepository _mainTasks;
        private readonly ISubTaskRepository _subTasks;

        public UnitOfWork(MafDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _mainTasks = new MainTaskRepository(_context);
            _subTasks = new SubTaskRepository(_context);
        }

        public IMainTaskRepository MainTasks => _mainTasks;
        public ISubTaskRepository SubTasks => _subTasks;

        public async Task<int> SaveChangesAsync(CancellationToken ct = default)
        {
            return await _context.SaveChangesAsync(ct);
        }

        public async Task BeginTransactionAsync(CancellationToken ct = default)
        {
            _transaction = await _context.Database.BeginTransactionAsync(ct);
        }

        public async Task CommitTransactionAsync(CancellationToken ct = default)
        {
            if (_transaction != null)
            {
                await _transaction.CommitAsync(ct);
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public async Task RollbackTransactionAsync(CancellationToken ct = default)
        {
            if (_transaction != null)
            {
                await _transaction.RollbackAsync(ct);
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public void Dispose()
        {
            _transaction?.Dispose();
            _context.Dispose();
        }
    }
}
