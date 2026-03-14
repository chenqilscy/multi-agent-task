using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Core.Enums;
using CKY.MultiAgentFramework.Core.Models.Persisted;
using CKY.MultiAgentFramework.Infrastructure.Repository.Data;
using Microsoft.EntityFrameworkCore;

namespace CKY.MultiAgentFramework.Infrastructure.Repository.Repositories
{
    /// <summary>
    /// 调度计划仓储实现
    /// </summary>
    public class SchedulePlanRepository : ISchedulePlanRepository
    {
        private readonly MafDbContext _context;

        public SchedulePlanRepository(MafDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<SchedulePlanEntity?> GetByIdAsync(int id, CancellationToken ct = default)
        {
            return await _context.SchedulePlans
                .FirstOrDefaultAsync(p => p.Id == id, ct);
        }

        public async Task<SchedulePlanEntity?> GetByPlanIdAsync(string planId, CancellationToken ct = default)
        {
            return await _context.SchedulePlans
                .FirstOrDefaultAsync(p => p.PlanId == planId, ct);
        }

        public async Task<SchedulePlanEntity> AddAsync(SchedulePlanEntity plan, CancellationToken ct = default)
        {
            _context.SchedulePlans.Add(plan);
            await _context.SaveChangesAsync(ct);
            return plan;
        }

        public async Task UpdateAsync(SchedulePlanEntity plan, CancellationToken ct = default)
        {
            _context.SchedulePlans.Update(plan);
            await _context.SaveChangesAsync(ct);
        }

        public async Task DeleteAsync(int id, CancellationToken ct = default)
        {
            var plan = await _context.SchedulePlans.FindAsync(new object[] { id }, ct);
            if (plan != null)
            {
                _context.SchedulePlans.Remove(plan);
                await _context.SaveChangesAsync(ct);
            }
        }

        public async Task<List<SchedulePlanEntity>> GetByStatusAsync(
            SchedulePlanStatus status,
            CancellationToken ct = default)
        {
            return await _context.SchedulePlans
                .Where(p => p.Status == status)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync(ct);
        }

        public async Task<List<SchedulePlanEntity>> GetRecentPlansAsync(
            int count,
            CancellationToken ct = default)
        {
            return await _context.SchedulePlans
                .OrderByDescending(p => p.CreatedAt)
                .Take(count)
                .ToListAsync(ct);
        }
    }

    /// <summary>
    /// 执行计划仓储实现
    /// </summary>
    public class ExecutionPlanRepository : IExecutionPlanRepository
    {
        private readonly MafDbContext _context;

        public ExecutionPlanRepository(MafDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<ExecutionPlanEntity?> GetByIdAsync(int id, CancellationToken ct = default)
        {
            return await _context.ExecutionPlans
                .FirstOrDefaultAsync(p => p.Id == id, ct);
        }

        public async Task<ExecutionPlanEntity?> GetByPlanIdAsync(string planId, CancellationToken ct = default)
        {
            return await _context.ExecutionPlans
                .FirstOrDefaultAsync(p => p.PlanId == planId, ct);
        }

        public async Task<ExecutionPlanEntity> AddAsync(ExecutionPlanEntity plan, CancellationToken ct = default)
        {
            _context.ExecutionPlans.Add(plan);
            await _context.SaveChangesAsync(ct);
            return plan;
        }

        public async Task UpdateAsync(ExecutionPlanEntity plan, CancellationToken ct = default)
        {
            _context.ExecutionPlans.Update(plan);
            await _context.SaveChangesAsync(ct);
        }

        public async Task DeleteAsync(int id, CancellationToken ct = default)
        {
            var plan = await _context.ExecutionPlans.FindAsync(new object[] { id }, ct);
            if (plan != null)
            {
                _context.ExecutionPlans.Remove(plan);
                await _context.SaveChangesAsync(ct);
            }
        }

        public async Task<List<ExecutionPlanEntity>> GetByStatusAsync(
            ExecutionPlanStatus status,
            CancellationToken ct = default)
        {
            return await _context.ExecutionPlans
                .Where(p => p.Status == status)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync(ct);
        }

        public async Task<List<ExecutionPlanEntity>> GetByMultipleStatusAsync(
            List<ExecutionPlanStatus> statuses,
            int count,
            CancellationToken ct = default)
        {
            return await _context.ExecutionPlans
                .Where(p => statuses.Contains(p.Status))
                .OrderByDescending(p => p.CreatedAt)
                .Take(count)
                .ToListAsync(ct);
        }
    }

    /// <summary>
    /// 任务执行结果仓储实现
    /// </summary>
    public class TaskExecutionResultRepository : ITaskExecutionResultRepository
    {
        private readonly MafDbContext _context;

        public TaskExecutionResultRepository(MafDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<TaskExecutionResultEntity?> GetByIdAsync(int id, CancellationToken ct = default)
        {
            return await _context.TaskExecutionResults
                .FirstOrDefaultAsync(r => r.Id == id, ct);
        }

        public async Task<List<TaskExecutionResultEntity>> GetByTaskIdAsync(
            string taskId,
            CancellationToken ct = default)
        {
            return await _context.TaskExecutionResults
                .Where(r => r.TaskId == taskId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync(ct);
        }

        public async Task<List<TaskExecutionResultEntity>> GetByPlanIdAsync(
            string planId,
            CancellationToken ct = default)
        {
            return await _context.TaskExecutionResults
                .Where(r => r.PlanId == planId)
                .OrderBy(r => r.StartedAt)
                .ToListAsync(ct);
        }

        public async Task<TaskExecutionResultEntity> AddAsync(
            TaskExecutionResultEntity result,
            CancellationToken ct = default)
        {
            _context.TaskExecutionResults.Add(result);
            await _context.SaveChangesAsync(ct);
            return result;
        }

        public async Task<List<TaskExecutionResultEntity>> AddRangeAsync(
            List<TaskExecutionResultEntity> results,
            CancellationToken ct = default)
        {
            await _context.TaskExecutionResults.AddRangeAsync(results, ct);
            await _context.SaveChangesAsync(ct);
            return results;
        }

        public async Task UpdateAsync(
            TaskExecutionResultEntity result,
            CancellationToken ct = default)
        {
            _context.TaskExecutionResults.Update(result);
            await _context.SaveChangesAsync(ct);
        }
    }
}
