using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Core.Models.Task;
using CKY.MultiAgentFramework.Repository.Data;
using Microsoft.EntityFrameworkCore;

namespace CKY.MultiAgentFramework.Repository.Repositories
{
    /// <summary>
    /// 子任务仓储实现
    /// </summary>
    public class SubTaskRepository : ISubTaskRepository
    {
        private readonly MafDbContext _context;

        public SubTaskRepository(MafDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<SubTask?> GetByIdAsync(int id, CancellationToken ct = default)
        {
            return await _context.SubTasks
                .Include(s => s.MainTask)
                .FirstOrDefaultAsync(s => s.Id == id, ct);
        }

        public async Task<List<SubTask>> GetByMainTaskIdAsync(int mainTaskId, CancellationToken ct = default)
        {
            return await _context.SubTasks
                .Where(s => s.MainTaskId == mainTaskId)
                .OrderBy(s => s.ExecutionOrder)
                .ToListAsync(ct);
        }

        public async Task<List<SubTask>> GetAllAsync(CancellationToken ct = default)
        {
            return await _context.SubTasks
                .OrderByDescending(s => s.MainTaskId)
                .ThenBy(s => s.ExecutionOrder)
                .ToListAsync(ct);
        }

        public async Task<SubTask> AddAsync(SubTask subTask, CancellationToken ct = default)
        {
            _context.SubTasks.Add(subTask);
            await _context.SaveChangesAsync(ct);
            return subTask;
        }

        public async Task<List<SubTask>> AddRangeAsync(List<SubTask> subTasks, CancellationToken ct = default)
        {
            await _context.SubTasks.AddRangeAsync(subTasks, ct);
            await _context.SaveChangesAsync(ct);
            return subTasks;
        }

        public async Task UpdateAsync(SubTask subTask, CancellationToken ct = default)
        {
            _context.SubTasks.Update(subTask);
            await _context.SaveChangesAsync(ct);
        }

        public async Task DeleteAsync(int id, CancellationToken ct = default)
        {
            var subTask = await _context.SubTasks.FindAsync(id, ct);
            if (subTask != null)
            {
                _context.SubTasks.Remove(subTask);
                await _context.SaveChangesAsync(ct);
            }
        }
    }
}
