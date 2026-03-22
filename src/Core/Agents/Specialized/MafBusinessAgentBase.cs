using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Core.Diagnostics;
using CKY.MultiAgentFramework.Core.Models.Task;
using CKY.MultiAgentFramework.Core.Models.LLM;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace CKY.MultiAgentFramework.Core.Agents
{
    /// <summary>
    /// MAF 业务 Agent 基类（纯业务层，不继承 AIAgent）
    /// </summary>
    /// <remarks>
    /// 架构设计：
    /// 1. 不继承 AIAgent，是纯业务逻辑抽象类
    /// 2. 通过组合方式使用 IMafAiAgentRegistry 调用LLM
    /// 3. 提供业务逻辑执行的抽象方法
    /// 4. Demo agents 继承此基类实现具体业务逻辑
    ///
    /// 层次关系：
    /// - Demo Agents → MafBusinessAgentBase (业务层，纯POCO)
    /// - MafBusinessAgentBase → IMafAiAgentRegistry (通过组合调用LLM)
    /// - MafAiAgent : AIAgent (LLM层，继承MS Agent Framework)
    ///
    /// 命名说明：
    /// - MafBusinessAgentBase: 业务层基类，强调业务逻辑
    /// - MafAiAgent: LLM层基类，强调LLM调用
    /// </remarks>
    public abstract class MafBusinessAgentBase
    {
        /// <summary>日志记录器</summary>
        protected readonly ILogger Logger;

        /// <summary>LLM Agent 注册表（用于获取合适的LLM实例）</summary>
        protected readonly IMafAiAgentRegistry LlmRegistry;

        #region 监控指标

        private static readonly Meter BusinessAgentMeter = new("CKY.MAF.BusinessAgent", "1.0.0");
        private static readonly Counter<long> AgentExecutionsCounter =
            BusinessAgentMeter.CreateCounter<long>("maf_business_agent_executions_total", "invocations", "Total business agent executions");
        private static readonly Counter<long> AgentErrorsCounter =
            BusinessAgentMeter.CreateCounter<long>("maf_business_agent_errors_total", "errors", "Total business agent execution errors");
        private static readonly Histogram<double> AgentDurationHistogram =
            BusinessAgentMeter.CreateHistogram<double>("maf_business_agent_duration_seconds", "s", "Business agent execution duration");
        private static readonly Counter<long> LlmCallsCounter =
            BusinessAgentMeter.CreateCounter<long>("maf_business_agent_llm_calls_total", "calls", "Total LLM calls from business agents");
        private static readonly Histogram<double> LlmCallDurationHistogram =
            BusinessAgentMeter.CreateHistogram<double>("maf_business_agent_llm_duration_seconds", "s", "LLM call duration from business agents");

        #endregion

        /// <summary>
        /// 构造函数
        /// </summary>
        protected MafBusinessAgentBase(
            IMafAiAgentRegistry llmRegistry,
            ILogger logger)
        {
            LlmRegistry = llmRegistry ?? throw new ArgumentNullException(nameof(llmRegistry));
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #region Agent 标识属性

        /// <summary>Agent 唯一标识（由子类实现）</summary>
        public abstract string AgentId { get; }

        /// <summary>Agent 显示名称（由子类实现）</summary>
        public abstract string Name { get; }

        /// <summary>Agent 描述（由子类实现）</summary>
        public abstract string Description { get; }

        /// <summary>Agent 能力列表（由子类实现）</summary>
        public abstract IReadOnlyList<string> Capabilities { get; }

        #endregion

        #region 业务逻辑抽象方法（由子类实现）

        /// <summary>
        /// 执行业务逻辑（核心抽象方法）
        /// </summary>
        public abstract Task<MafTaskResponse> ExecuteBusinessLogicAsync(
            MafTaskRequest request,
            CancellationToken ct = default);

        #endregion

        #region LLM 调用辅助方法

        /// <summary>
        /// 调用 LLM 处理文本（简单版本，无历史消息）
        /// </summary>
        /// <param name="prompt">提示词</param>
        /// <param name="scenario">目标场景（用于选择合适的 LLM Agent）</param>
        /// <param name="systemPrompt">系统提示词（可选）</param>
        /// <param name="ct">取消令牌</param>
        /// <returns>LLM 响应文本</returns>
        protected async Task<string> CallLlmAsync(
            string prompt,
            LlmScenario scenario = LlmScenario.Chat,
            string? systemPrompt = null,
            CancellationToken ct = default)
        {
            using var activity = MafActivitySource.Agent.StartActivity("business_agent.call_llm");
            activity?.SetTag("agent.id", AgentId);
            activity?.SetTag("agent.name", Name);
            activity?.SetTag("llm.scenario", scenario.ToString());
            activity?.SetTag("llm.prompt_length", prompt.Length);

            var stopwatch = Stopwatch.StartNew();
            LlmCallsCounter.Add(1, new KeyValuePair<string, object?>("agent", Name));

            var llmAgent = await LlmRegistry.GetBestAgentAsync(scenario, ct);

            activity?.SetTag("llm.provider", llmAgent.Config.ProviderName);
            activity?.SetTag("llm.model", llmAgent.Config.ModelId);

            var result = await llmAgent.ExecuteAsync(
                llmAgent.GetCurrentModelId(),
                prompt,
                systemPrompt,
                ct);

            stopwatch.Stop();
            LlmCallDurationHistogram.Record(stopwatch.Elapsed.TotalSeconds,
                new KeyValuePair<string, object?>("agent", Name),
                new KeyValuePair<string, object?>("provider", llmAgent.Config.ProviderName));

            activity?.SetTag("llm.response_length", result.Length);
            return result;
        }

        /// <summary>
        /// 调用 LLM 处理多轮对话（带历史消息）
        /// </summary>
        /// <param name="messages">对话消息列表（包含历史消息）</param>
        /// <param name="scenario">目标场景（用于选择合适的 LLM Agent）</param>
        /// <param name="ct">取消令牌</param>
        /// <returns>LLM 响应文本</returns>
        /// <remarks>
        /// 使用场景：
        /// - 多轮对话：需要传递上下文历史
        /// - 复杂任务：需要多轮交互才能完成
        /// - 对话式应用：聊天机器人、智能客服等
        ///
        /// 示例：
        /// <code>
        /// var messages = new List&lt;ChatMessage&gt;
        /// {
        ///     new ChatMessage(ChatRole.System, "你是一个有用的AI助手"),
        ///     new ChatMessage(ChatRole.User, "你好"),
        ///     new ChatMessage(ChatRole.Assistant, "你好！有什么我可以帮助你的吗？"),
        ///     new ChatMessage(ChatRole.User, "介绍一下你自己")
        /// };
        /// var response = await CallLlmChatAsync(messages, LlmScenario.Chat);
        /// </code>
        /// </remarks>
        protected async Task<string> CallLlmChatAsync(
            IEnumerable<ChatMessage> messages,
            LlmScenario scenario = LlmScenario.Chat,
            CancellationToken ct = default)
        {
            var llmAgent = await LlmRegistry.GetBestAgentAsync(scenario, ct);

            // 将 ChatMessage 列表转换为单个 prompt（如果 Agent 不支持多轮对话）
            // 注意：这里简化处理，实际应该让支持多轮对话的 Agent 直接处理 messages
            var prompt = BuildPromptFromMessages(messages);

            return await llmAgent.ExecuteAsync(
                llmAgent.GetCurrentModelId(),
                prompt,
                null,
                ct);
        }

        /// <summary>
        /// 批量调用 LLM
        /// </summary>
        /// <param name="prompts">提示词数组</param>
        /// <param name="scenario">目标场景（用于选择合适的 LLM Agent）</param>
        /// <param name="systemPrompt">系统提示词（可选）</param>
        /// <param name="ct">取消令牌</param>
        /// <returns>LLM 响应文本数组</returns>
        protected async Task<string[]> CallLlmBatchAsync(
            string[] prompts,
            LlmScenario scenario = LlmScenario.Chat,
            string? systemPrompt = null,
            CancellationToken ct = default)
        {
            var llmAgent = await LlmRegistry.GetBestAgentAsync(scenario, ct);
            return await llmAgent.ExecuteBatchAsync(
                llmAgent.GetCurrentModelId(),
                prompts,
                systemPrompt,
                ct);
        }

        /// <summary>
        /// 从 ChatMessage 列表构建单个 prompt（兼容不支持多轮对话的 Agent）
        /// </summary>
        private static string BuildPromptFromMessages(IEnumerable<ChatMessage> messages)
        {
            var sb = new System.Text.StringBuilder();

            foreach (var message in messages)
            {
                // 使用 if-else 而不是 switch，因为 ChatRole 可能不是枚举类型
                if (message.Role.ToString() == "System")
                {
                    sb.AppendLine($"[System]: {message.Text}");
                }
                else if (message.Role.ToString() == "User")
                {
                    sb.AppendLine($"[User]: {message.Text}");
                }
                else if (message.Role.ToString() == "Assistant")
                {
                    sb.AppendLine($"[Assistant]: {message.Text}");
                }
                else
                {
                    sb.AppendLine($"[{message.Role}]: {message.Text}");
                }
            }

            return sb.ToString();
        }

        #endregion

        #region 参数辅助方法

        /// <summary>
        /// 从请求中提取参数（带类型转换和默认值）
        /// </summary>
        protected T GetParameter<T>(MafTaskRequest request, string key, T defaultValue)
        {
            if (request.Parameters.TryGetValue(key, out var value))
            {
                try
                {
                    return (T)Convert.ChangeType(value, typeof(T));
                }
                catch
                {
                    return defaultValue;
                }
            }
            return defaultValue;
        }

        #endregion
    }
}
