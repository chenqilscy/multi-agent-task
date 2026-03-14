using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Core.Enums;
using CKY.MultiAgentFramework.Core.Models.Task;
using CKY.MultiAgentFramework.Repository.Data;
using Microsoft.EntityFrameworkCore;

namespace CKY.MultiAgentFramework.Repository.Repositories
{
    /// <summary>
    /// 主任务仓储实现
    /// </summary>
    public class MainTaskRepository : IMainTaskRepository
    {
        private readonly MafDbContext _context;

        public MainTaskRepository(MafDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<MainTask?> GetByIdAsync(int id, CancellationToken ct = default)
        {
            return await _context.MainTasks
                .Include(m => m.SubTasks)
                .FirstOrDefaultAsync(m => m.Id == id, ct);
        }

        public async Task<List<MainTask>> GetAllAsync(CancellationToken ct = default)
        {
            return await _context.MainTasks
                .Include(m => m.SubTasks)
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync(ct);
        }

        public async Task<List<MainTask>> GetByStatusAsync(MafTaskStatus status, CancellationToken ct = default)
        {
            return await _context.MainTasks
                .Where(m => m.Status == status)
                .Include(m => m.SubTasks)
                .OrderByDescending(m => m.Priority)
                .ToListAsync(ct);
        }

        public async Task<List<MainTask>> GetHighPriorityTasksAsync(int minPriority, CancellationToken ct = default)
        {
            return await _context.MainTasks
                .Where(m => (int)m.Priority >= minPriority && m.Status == MafTaskStatus.Pending)
                .OrderByDescending(m => m.Priority)
                .ThenBy(m => m.CreatedAt)
                .ToListAsync(ct);
        }

        public async Task<MainTask> AddAsync(MainTask task, CancellationToken ct = default)
        {
            _context.MainTasks.Add(task);
            await _context.SaveChangesAsync(ct);
            return task;
        }

        public async Task UpdateAsync(MainTask task, CancellationToken ct = default)
        {
            _context.MainTasks.Update(task);
            await _context.SaveChangesAsync(ct);
        }

        public async Task DeleteAsync(int id, CancellationToken ct = default)
        {
            var task = await _context.MainTasks.FindAsync(new[] { id }, ct);
            if (task != null)
            {
                _context.MainTasks.Remove(task);
                await _context.SaveChangesAsync(ct);
            }
        }
    }
}
