using CKY.MultiAgentFramework.Core.Agents;
using CKY.MultiAgentFramework.Core.Models.LLM;

namespace CKY.MultiAgentFramework.Core.Abstractions
{
    /// <summary>
    /// MAF AI Agent 注册表接口
    /// 负责管理多个 MafAiAgent 实例，根据场景和优先级动态选择
    /// </summary>
    public interface IMafAiAgentRegistry
    {
        /// <summary>
        /// 注册 MafAiAgent
        /// </summary>
        void RegisterAgent(MafAiAgent agent);

        /// <summary>
        /// 批量注册 MafAiAgent
        /// </summary>
        void RegisterAgents(IEnumerable<MafAiAgent> agents);

        /// <summary>
        /// 获取指定场景的最佳 MafAiAgent（基于优先级和可用性）
        /// </summary>
        Task<MafAiAgent> GetBestAgentAsync(
            LlmScenario scenario,
            CancellationToken ct = default);

        /// <summary>
        /// 获取指定提供商的 MafAiAgent
        /// </summary>
        Task<MafAiAgent?> GetAgentByProviderAsync(
            string providerName,
            CancellationToken ct = default);

        /// <summary>
        /// 获取所有已注册的 MafAiAgent
        /// </summary>
        IReadOnlyList<MafAiAgent> GetAllAgents();

        /// <summary>
        /// 获取支持指定场景的所有 MafAiAgent
        /// </summary>
        IReadOnlyList<MafAiAgent> GetAgentsByScenario(LlmScenario scenario);

        /// <summary>
        /// 启用/禁用指定的 MafAiAgent
        /// </summary>
        Task SetAgentEnabledAsync(string providerName, bool enabled, CancellationToken ct = default);

        /// <summary>
        /// 从数据库配置重新加载 MafAiAgent
        /// </summary>
        Task ReloadFromDatabaseAsync(CancellationToken ct = default);
    }
}
