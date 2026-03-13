using CKY.MultiAgentFramework.Core.Agents;
using CKY.MultiAgentFramework.Core.Models.LLM;

namespace CKY.MultiAgentFramework.Core.Abstractions
{
    /// <summary>
    /// LLM Agent 注册表接口
    /// 负责管理多个 LLM Agent 实例，根据场景和优先级动态选择
    /// </summary>
    public interface ILlmAgentRegistry
    {
        /// <summary>
        /// 注册 LLM Agent
        /// </summary>
        void RegisterAgent(LlmAgent agent);

        /// <summary>
        /// 批量注册 LLM Agent
        /// </summary>
        void RegisterAgents(IEnumerable<LlmAgent> agents);

        /// <summary>
        /// 获取指定场景的最佳 LLM Agent（基于优先级和可用性）
        /// </summary>
        Task<LlmAgent> GetBestAgentAsync(
            LlmScenario scenario,
            CancellationToken ct = default);

        /// <summary>
        /// 获取指定提供商的 LLM Agent
        /// </summary>
        Task<LlmAgent?> GetAgentByProviderAsync(
            string providerName,
            CancellationToken ct = default);

        /// <summary>
        /// 获取所有已注册的 LLM Agent
        /// </summary>
        IReadOnlyList<LlmAgent> GetAllAgents();

        /// <summary>
        /// 获取支持指定场景的所有 LLM Agent
        /// </summary>
        IReadOnlyList<LlmAgent> GetAgentsByScenario(LlmScenario scenario);

        /// <summary>
        /// 启用/禁用指定的 LLM Agent
        /// </summary>
        Task SetAgentEnabledAsync(string providerName, bool enabled, CancellationToken ct = default);

        /// <summary>
        /// 从数据库配置重新加载 LLM Agent
        /// </summary>
        Task ReloadFromDatabaseAsync(CancellationToken ct = default);
    }
}
