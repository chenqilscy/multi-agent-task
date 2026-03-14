using CKY.MultiAgentFramework.Core.Agents;
using CKY.MultiAgentFramework.Core.Models.LLM;

namespace CKY.MultiAgentFramework.Core.Abstractions
{
    /// <summary>
    /// LLM Agent 工厂接口
    /// 负责根据场景和配置创建 LLM Agent 实例
    /// </summary>
    public interface ILlmAgentFactory
    {
        /// <summary>
        /// 根据提供商配置创建 LLM Agent
        /// </summary>
        /// <param name="config">提供商配置</param>
        /// <param name="scenario">目标场景（用于验证 Agent 是否支持）</param>
        /// <param name="ct">取消令牌</param>
        /// <returns>创建的 LLM Agent 实例</returns>
        Task<MafAiAgent> CreateAgentAsync(
            LlmProviderConfig config,
            LlmScenario scenario,
            CancellationToken ct = default);

        /// <summary>
        /// 根据提供商名称创建 LLM Agent（从数据库加载配置）
        /// </summary>
        /// <param name="providerName">提供商名称（如 zhipuai, tongyi）</param>
        /// <param name="scenario">目标场景</param>
        /// <param name="ct">取消令牌</param>
        /// <returns>创建的 LLM Agent 实例</returns>
        Task<MafAiAgent> CreateAgentByProviderAsync(
            string providerName,
            LlmScenario scenario,
            CancellationToken ct = default);

        /// <summary>
        /// 根据场景创建最佳 LLM Agent（自动选择优先级最高的）
        /// </summary>
        /// <param name="scenario">目标场景</param>
        /// <param name="ct">取消令牌</param>
        /// <returns>创建的 LLM Agent 实例</returns>
        Task<MafAiAgent> CreateBestAgentForScenarioAsync(
            LlmScenario scenario,
            CancellationToken ct = default);

        /// <summary>
        /// 批量创建支持指定场景的所有 LLM Agent
        /// </summary>
        /// <param name="scenario">目标场景</param>
        /// <param name="ct">取消令牌</param>
        /// <returns>创建的 LLM Agent 实例列表（按优先级排序）</returns>
        Task<List<MafAiAgent>> CreateAllAgentsForScenarioAsync(
            LlmScenario scenario,
            CancellationToken ct = default);

        /// <summary>
        /// 创建带有 Fallback 链的 LLM Agent
        /// 当主 Agent 失败时，自动尝试下一个优先级的 Agent
        /// </summary>
        /// <param name="scenario">目标场景</param>
        /// <param name="ct">取消令牌</param>
        /// <returns>带有 Fallback 能力的 LLM Agent</returns>
        Task<MafAiAgent> CreateAgentWithFallbackAsync(
            LlmScenario scenario,
            CancellationToken ct = default);

        /// <summary>
        /// 检查指定的提供商是否支持该场景
        /// </summary>
        /// <param name="providerName">提供商名称</param>
        /// <param name="scenario">目标场景</param>
        /// <param name="ct">取消令牌</param>
        /// <returns>是否支持</returns>
        Task<bool> IsScenarioSupportedAsync(
            string providerName,
            LlmScenario scenario,
            CancellationToken ct = default);
    }
}
