using CKY.MultiAgentFramework.Core.Models.LLM;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace CKY.MultiAgentFramework.Core.Agents
{
    /// <summary>
    /// LLM Agent 抽象基类（继承自 MS AF 的 AIAgent）
    /// </summary>
    /// <remarks>
    /// 设计原则：
    /// 1. 继承 Microsoft.Agents.AI.AIAgent 以充分利用框架能力
    /// 2. 提供统一的 LLM 调用接口
    /// 3. 具体厂商实现（智谱AI、通义千问等）继承此类
    ///
    /// 注意：由于 RC4 版本 API 限制，当前不重写 RunCoreAsync 等方法
    /// 而是提供 ExecuteAsync() 方法供外部直接调用
    /// </remarks>
    public abstract class LlmAgent : AIAgent
    {
        /// <summary>LLM 配置</summary>
        public readonly LlmProviderConfig Config;

        /// <summary>日志记录器</summary>
        protected readonly ILogger Logger;

        /// <summary>
        /// 构造函数
        /// </summary>
        protected LlmAgent(
            LlmProviderConfig config,
            ILogger logger)
        {
            Config = config ?? throw new ArgumentNullException(nameof(config));
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // 验证配置
            Config.Validate();
        }

        #region AIAgent 标识属性

        /// <summary>
        /// Agent 唯一标识（基于配置的 ProviderName 和 ModelId）
        /// </summary>
        public string AgentId => $"{Config.ProviderName}-{Config.ModelId}".ToLowerInvariant();

        /// <summary>
        /// Agent 显示名称
        /// </summary>
        public string AgentName => $"{Config.ProviderDisplayName} ({Config.ModelId})";

        /// <summary>
        /// Agent 描述
        /// </summary>
        public string AgentDescription => $"LLM Agent for {Config.ProviderName} - {Config.ModelId}";

        #endregion

        #region AgentSession 具体实现类

        /// <summary>
        /// 简单的 AgentSession 实现
        /// </summary>
        private class SimpleAgentSession : AgentSession
        {
            // 使用默认构造函数，让基类处理初始化
            public SimpleAgentSession()
            {
            }

            // 如果需要，可以重写其他方法或属性
        }

        #endregion

        #region AIAgent 抽象方法实现（最小化实现）

        /// <summary>
        /// 创建会话 - 简化版实现
        /// </summary>
        protected override ValueTask<AgentSession> CreateSessionCoreAsync(CancellationToken cancellationToken = default)
        {
            var session = new SimpleAgentSession();
            return ValueTask.FromResult((AgentSession)session);
        }

        /// <summary>
        /// 序列化会话 - 简化版实现
        /// </summary>
        protected override ValueTask<JsonElement> SerializeSessionCoreAsync(
            AgentSession session,
            System.Text.Json.JsonSerializerOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            // 简单序列化，使用空对象
            return ValueTask.FromResult(JsonSerializer.SerializeToElement(new { }));
        }

        /// <summary>
        /// 反序列化会话 - 简化版实现
        /// </summary>
        protected override ValueTask<AgentSession> DeserializeSessionCoreAsync(
            JsonElement serializedSession,
            System.Text.Json.JsonSerializerOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            // 简单创建新会话
            var session = new SimpleAgentSession();
            return ValueTask.FromResult((AgentSession)session);
        }

        /// <summary>
        /// 运行 Agent（非流式）- 桥接方法
        /// 注意：此方法为 AIAgent 框架要求，实际 LLM 调用请使用 ExecuteAsync()
        /// </summary>
        protected override async Task<AgentResponse> RunCoreAsync(
            IEnumerable<ChatMessage> messages,
            AgentSession? session = null,
            AgentRunOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            // 提取用户消息
            var userMessage = messages.FirstOrDefault(m => m.Role == ChatRole.User);
            if (userMessage == null)
            {
                throw new ArgumentException("No user message found", nameof(messages));
            }

            // 提取系统提示（如果有）
            var systemMessage = messages.FirstOrDefault(m => m.Role == ChatRole.System);
            var systemPrompt = systemMessage?.Text ?? "你是一个有用的AI助手。";

            // 提取用户消息文本
            var prompt = userMessage.Text ?? string.Empty;

            // 调用子类实现的 ExecuteAsync 方法
            var responseText = await ExecuteAsync(
                Config.ModelId,
                prompt,
                LlmScenario.Chat,
                systemPrompt,
                cancellationToken);

            // 构建响应消息列表
            var responseMessages = new List<ChatMessage>
            {
                new ChatMessage(ChatRole.Assistant, responseText)
            };

            // 返回 AgentResponse
            return new AgentResponse(responseMessages);
        }

        /// <summary>
        /// 运行 Agent（流式）- 暂不支持，使用非流式实现
        /// </summary>
        protected override async IAsyncEnumerable<AgentResponseUpdate> RunCoreStreamingAsync(
            IEnumerable<ChatMessage> messages,
            AgentSession? session = null,
            AgentRunOptions? options = null,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            // 暂不支持流式响应，调用非流式方法并返回单个更新
            var response = await RunCoreAsync(messages, session, options, cancellationToken);

            // 创建一个简单的响应更新
            yield return new AgentResponseUpdate();
        }

        #endregion

        #region LLM 调用核心方法

        /// <summary>
        /// 执行 LLM 调用（核心抽象方法）
        /// </summary>
        /// <param name="modelId">模型 ID（如 glm-4, qwen-max）</param>
        /// <param name="prompt">提示词</param>
        /// <param name="scenario">LLM 使用场景</param>
        /// <param name="systemPrompt">系统提示词（可选）</param>
        /// <param name="ct">取消令牌</param>
        /// <returns>LLM 响应文本</returns>
        public abstract Task<string> ExecuteAsync(
            string modelId,
            string prompt,
            LlmScenario scenario = LlmScenario.Chat,
            string? systemPrompt = null,
            CancellationToken ct = default);

        /// <summary>
        /// 批量执行 LLM 调用（用于并行处理多个请求）
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

            // 并行执行多个请求
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
