using CKY.MultiAgentFramework.Core.Enums;
using CKY.MultiAgentFramework.Core.Models.Task;

namespace CKY.MultiAgentFramework.Core.Abstractions
{
    /// <summary>
    /// 子任务仓储接口
    /// </summary>
    public interface ISubTaskRepository
    {
        /// <summary>
        /// 根据 ID 获取子任务
        /// </summary>
        Task<SubTask?> GetByIdAsync(int id, CancellationToken ct = default);

        /// <summary>
        /// 根据主任务 ID 获取子任务列表
        /// </summary>
        Task<List<SubTask>> GetByMainTaskIdAsync(int mainTaskId, CancellationToken ct = default);

        /// <summary>
        /// 获取所有子任务
        /// </summary>
        Task<List<SubTask>> GetAllAsync(CancellationToken ct = default);

        /// <summary>
        /// 添加子任务
        /// </summary>
        Task<SubTask> AddAsync(SubTask subTask, CancellationToken ct = default);

        /// <summary>
        /// 批量添加子任务
        /// </summary>
        Task<List<SubTask>> AddRangeAsync(List<SubTask> subTasks, CancellationToken ct = default);

        /// <summary>
        /// 更新子任务
        /// </summary>
        Task UpdateAsync(SubTask subTask, CancellationToken ct = default);

        /// <summary>
        /// 删除子任务
        /// </summary>
        Task DeleteAsync(int id, CancellationToken ct = default);
    }
}
