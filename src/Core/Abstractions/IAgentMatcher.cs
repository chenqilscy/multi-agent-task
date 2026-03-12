using CKY.MultiAgentFramework.Core.Models.Task;

namespace CKY.MultiAgentFramework.Core.Abstractions
{
    /// <summary>
    /// Agent匹配器接口
    /// 根据子任务的能力需求，从注册的Agent池中选择最合适的Agent
    /// </summary>
    public interface IAgentMatcher
    {
        /// <summary>
        /// 根据能力找到最佳Agent
        /// </summary>
        Task<string> FindBestAgentIdAsync(
            string requiredCapability,
            CancellationToken ct = default);

        /// <summary>
        /// 批量匹配Agent（返回任务到AgentId的映射）
        /// </summary>
        Task<IDictionary<string, string>> MatchBatchAsync(
            List<DecomposedTask> tasks,
            CancellationToken ct = default);

        /// <summary>
        /// 获取所有可用Agent的ID列表
        /// </summary>
        Task<List<string>> GetAvailableAgentIdsAsync(CancellationToken ct = default);
    }
}
