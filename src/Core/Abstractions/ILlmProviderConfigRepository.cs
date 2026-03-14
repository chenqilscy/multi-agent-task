using CKY.MultiAgentFramework.Core.Models.LLM;

namespace CKY.MultiAgentFramework.Core.Abstractions
{
    /// <summary>
    /// LLM 提供商配置仓储接口
    /// 用于从数据库加载和保存 LLM 提供商配置
    /// </summary>
    public interface ILlmProviderConfigRepository
    {
        /// <summary>
        /// 根据提供商名称获取配置
        /// </summary>
        /// <param name="providerName">提供商名称（如 zhipuai, tongyi）</param>
        /// <param name="ct">取消令牌</param>
        /// <returns>提供商配置，如果不存在则返回 null</returns>
        Task<LlmProviderConfig?> GetByNameAsync(
            string providerName,
            CancellationToken ct = default);

        /// <summary>
        /// 获取所有启用的提供商配置
        /// </summary>
        /// <param name="ct">取消令牌</param>
        /// <returns>启用的提供商配置列表</returns>
        Task<List<LlmProviderConfig>> GetAllEnabledAsync(
            CancellationToken ct = default);

        /// <summary>
        /// 获取所有提供商配置
        /// </summary>
        /// <param name="ct">取消令牌</param>
        /// <returns>所有提供商配置列表</returns>
        Task<List<LlmProviderConfig>> GetAllAsync(
            CancellationToken ct = default);

        /// <summary>
        /// 根据场景获取支持的提供商配置
        /// </summary>
        /// <param name="scenario">目标场景</param>
        /// <param name="ct">取消令牌</param>
        /// <returns>支持该场景的提供商配置列表（按优先级排序）</returns>
        Task<List<LlmProviderConfig>> GetByScenarioAsync(
            LlmScenario scenario,
            CancellationToken ct = default);

        /// <summary>
        /// 保存或更新提供商配置
        /// </summary>
        /// <param name="config">提供商配置</param>
        /// <param name="ct">取消令牌</param>
        /// <returns>保存的配置（包含生成的 ID）</returns>
        Task<LlmProviderConfig> SaveAsync(
            LlmProviderConfig config,
            CancellationToken ct = default);

        /// <summary>
        /// 删除提供商配置
        /// </summary>
        /// <param name="providerName">提供商名称</param>
        /// <param name="ct">取消令牌</param>
        /// <returns>是否删除成功</returns>
        Task<bool> DeleteAsync(
            string providerName,
            CancellationToken ct = default);

        /// <summary>
        /// 检查提供商是否存在
        /// </summary>
        /// <param name="providerName">提供商名称</param>
        /// <param name="ct">取消令牌</param>
        /// <returns>是否存在</returns>
        Task<bool> ExistsAsync(
            string providerName,
            CancellationToken ct = default);

        /// <summary>
        /// 更新最后使用时间
        /// </summary>
        /// <param name="providerName">提供商名称</param>
        /// <param name="ct">取消令牌</param>
        Task UpdateLastUsedAsync(
            string providerName,
            CancellationToken ct = default);
    }
}
