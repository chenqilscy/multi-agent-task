using CKY.MultiAgentFramework.Core.Enums;
using CKY.MultiAgentFramework.Core.Models.Persisted;

namespace CKY.MultiAgentFramework.Core.Abstractions
{
    /// <summary>
    /// 调度计划仓储接口
    /// </summary>
    public interface ISchedulePlanRepository
    {
        /// <summary>
        /// 根据 ID 获取调度计划
        /// </summary>
        Task<SchedulePlanEntity?> GetByIdAsync(int id, CancellationToken ct = default);

        /// <summary>
        /// 根据 PlanId 获取调度计划
        /// </summary>
        Task<SchedulePlanEntity?> GetByPlanIdAsync(string planId, CancellationToken ct = default);

        /// <summary>
        /// 添加调度计划
        /// </summary>
        Task<SchedulePlanEntity> AddAsync(SchedulePlanEntity plan, CancellationToken ct = default);

        /// <summary>
        /// 更新调度计划
        /// </summary>
        Task UpdateAsync(SchedulePlanEntity plan, CancellationToken ct = default);

        /// <summary>
        /// 删除调度计划
        /// </summary>
        Task DeleteAsync(int id, CancellationToken ct = default);

        /// <summary>
        /// 根据状态获取调度计划列表
        /// </summary>
        Task<List<SchedulePlanEntity>> GetByStatusAsync(
            SchedulePlanStatus status,
            CancellationToken ct = default);

        /// <summary>
        /// 获取最近的调度计划
        /// </summary>
        Task<List<SchedulePlanEntity>> GetRecentPlansAsync(
            int count,
            CancellationToken ct = default);
    }

    /// <summary>
    /// 执行计划仓储接口
    /// </summary>
    public interface IExecutionPlanRepository
    {
        /// <summary>
        /// 根据 ID 获取执行计划
        /// </summary>
        Task<ExecutionPlanEntity?> GetByIdAsync(int id, CancellationToken ct = default);

        /// <summary>
        /// 根据 PlanId 获取执行计划
        /// </summary>
        Task<ExecutionPlanEntity?> GetByPlanIdAsync(string planId, CancellationToken ct = default);

        /// <summary>
        /// 添加执行计划
        /// </summary>
        Task<ExecutionPlanEntity> AddAsync(ExecutionPlanEntity plan, CancellationToken ct = default);

        /// <summary>
        /// 更新执行计划
        /// </summary>
        Task UpdateAsync(ExecutionPlanEntity plan, CancellationToken ct = default);

        /// <summary>
        /// 删除执行计划
        /// </summary>
        Task DeleteAsync(int id, CancellationToken ct = default);

        /// <summary>
        /// 根据状态获取执行计划列表
        /// </summary>
        Task<List<ExecutionPlanEntity>> GetByStatusAsync(
            ExecutionPlanStatus status,
            CancellationToken ct = default);

        /// <summary>
        /// 根据多个状态获取执行计划列表（避免 N+1 查询）
        /// </summary>
        Task<List<ExecutionPlanEntity>> GetByMultipleStatusAsync(
            List<ExecutionPlanStatus> statuses,
            int count,
            CancellationToken ct = default);
    }

    /// <summary>
    /// 任务执行结果仓储接口
    /// </summary>
    public interface ITaskExecutionResultRepository
    {
        /// <summary>
        /// 根据 ID 获取任务执行结果
        /// </summary>
        Task<TaskExecutionResultEntity?> GetByIdAsync(int id, CancellationToken ct = default);

        /// <summary>
        /// 根据任务ID获取执行结果
        /// </summary>
        Task<List<TaskExecutionResultEntity>> GetByTaskIdAsync(
            string taskId,
            CancellationToken ct = default);

        /// <summary>
        /// 根据计划ID获取所有执行结果
        /// </summary>
        Task<List<TaskExecutionResultEntity>> GetByPlanIdAsync(
            string planId,
            CancellationToken ct = default);

        /// <summary>
        /// 添加任务执行结果
        /// </summary>
        Task<TaskExecutionResultEntity> AddAsync(
            TaskExecutionResultEntity result,
            CancellationToken ct = default);

        /// <summary>
        /// 批量添加任务执行结果
        /// </summary>
        Task<List<TaskExecutionResultEntity>> AddRangeAsync(
            List<TaskExecutionResultEntity> results,
            CancellationToken ct = default);

        /// <summary>
        /// 更新任务执行结果
        /// </summary>
        Task UpdateAsync(
            TaskExecutionResultEntity result,
            CancellationToken ct = default);
    }
}
