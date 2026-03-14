using CKY.MultiAgentFramework.Core.Enums;
using CKY.MultiAgentFramework.Core.Models.Task;

namespace CKY.MultiAgentFramework.Core.Abstractions
{
    /// <summary>
    /// 主任务仓储接口
    /// </summary>
    public interface IMainTaskRepository
    {
        /// <summary>
        /// 根据 ID 获取主任务
        /// </summary>
        Task<MainTask?> GetByIdAsync(int id, CancellationToken ct = default);

        /// <summary>
        /// 获取所有主任务
        /// </summary>
        Task<List<MainTask>> GetAllAsync(CancellationToken ct = default);

        /// <summary>
        /// 根据状态获取主任务
        /// </summary>
        Task<List<MainTask>> GetByStatusAsync(MafTaskStatus status, CancellationToken ct = default);

        /// <summary>
        /// 获取高优先级任务
        /// </summary>
        Task<List<MainTask>> GetHighPriorityTasksAsync(int minPriority, CancellationToken ct = default);

        /// <summary>
        /// 添加主任务
        /// </summary>
        Task<MainTask> AddAsync(MainTask task, CancellationToken ct = default);

        /// <summary>
        /// 更新主任务
        /// </summary>
        Task UpdateAsync(MainTask task, CancellationToken ct = default);

        /// <summary>
        /// 删除主任务
        /// </summary>
        Task DeleteAsync(int id, CancellationToken ct = default);
    }
}
