using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Core.Models.LLM;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace CKY.MultiAgentFramework.Core.Agents
{
    /// <summary>
    /// MAF Agent 基类（使用组合模式而非继承 AIAgent）
    /// </summary>
    /// <remarks>
    /// 设计调整：
    /// 由于 Microsoft.Agents.AI 的 AIAgent 基类 API 与预期不同，
    /// 采用组合模式而非继承模式来构建 LLM Agent。
    ///
    /// 架构设计：
    /// 1. 不继承 AIAgent，而是使用 AIContextProvider 管道
    /// 2. MafAgentBase 提供统一的 LLM 调用接口
    /// 3. 具体厂商实现（智谱AI、通义千问等）继承此类
    /// </remarks>
    public abstract class MafAgentBase
    {
        /// <summary>LLM 配置</summary>
        public readonly LlmProviderConfig Config;

        /// <summary>日志记录器</summary>
        protected readonly ILogger Logger;

        /// <summary>
        /// 构造函数
        /// </summary>
        protected MafAgentBase(
            LlmProviderConfig config,
            ILogger logger)
        {
            Config = config ?? throw new ArgumentNullException(nameof(config));
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Agent 唯一标识
        /// </summary>
        public virtual string Id => $"{Config.ProviderName}-{Config.ModelId}".ToLowerInvariant();

        /// <summary>
        /// Agent 显示名称
        /// </summary>
        public virtual string Name => $"{Config.ProviderDisplayName} ({Config.ModelId})";

        /// <summary>
        /// Agent 描述
        /// </summary>
        public virtual string Description => $"LLM Agent for {Config.ProviderName} - {Config.ModelId}";

        #region LLM 调用核心方法

        /// <summary>
        /// 执行 LLM 调用（核心抽象方法）
        /// </summary>
        public abstract Task<string> ExecuteAsync(
            string modelId,
            string prompt,
            LlmScenario scenario = LlmScenario.Chat,
            string? systemPrompt = null,
            CancellationToken ct = default);

        /// <summary>
        /// 批量执行 LLM 调用
        /// </summary>
        public virtual async Task<string[]> ExecuteBatchAsync(
            string modelId,
            string[] prompts,
            LlmScenario scenario = LlmScenario.Chat,
            string? systemPrompt = null,
            CancellationToken ct = default)
        {
            if (prompts == null || prompts.Length == 0)
                return Array.Empty<string>();

            var tasks = prompts.Select(prompt =>
                ExecuteAsync(modelId, prompt, scenario, systemPrompt, ct));

            return await Task.WhenAll(tasks);
        }

        /// <summary>
        /// 检查模型是否支持指定场景
        /// </summary>
        public virtual bool SupportsScenario(LlmScenario scenario)
        {
            return Config.SupportedScenarios.Contains(scenario);
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 获取当前配置的模型 ID
        /// </summary>
        public string GetCurrentModelId()
        {
            return Config.ModelId;
        }

        /// <summary>
        /// 获取 API 密钥
        /// </summary>
        protected string GetApiKey()
        {
            if (string.IsNullOrWhiteSpace(Config.ApiKey))
                throw new InvalidOperationException($"API Key is not configured for {Config.ProviderName}");

            return Config.ApiKey;
        }

        #endregion
    }
}
