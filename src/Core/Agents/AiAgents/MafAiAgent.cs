using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Core.Models.LLM;
using CKY.MultiAgentFramework.Core.Models.Session;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace CKY.MultiAgentFramework.Core.Agents
{
    /// <summary>
    /// LLM AI Agent 抽象基类（继承自 MS AF 的 AIAgent）
    /// </summary>
    /// <remarks>
    /// 设计原则：
    /// 1. 继承 Microsoft.Agents.AI.AIAgent 以充分利用框架能力
    /// 2. 提供统一的 LLM 调用接口
    /// 3. 具体厂商实现（智谱AI、通义千问等）继承此类
    ///
    /// 注意：由于 RC4 版本 API 限制，当前不重写 RunCoreAsync 等方法
    /// 而是提供 ExecuteAsync() 方法供外部直接调用
    ///
    /// 命名说明：MafAiAgent 与 MafBusinessAgentBase 保持命名一致
    /// - MafAiAgent: 基础设施层 AI Agent 基类，继承 MS AF 的 AIAgent
    /// - MafBusinessAgentBase: 业务层 Agent 基类，使用组合模式
    /// </remarks>
    public abstract class MafAiAgent : AIAgent
    {
        /// <summary>LLM 配置</summary>
        public readonly LlmProviderConfig Config;

        /// <summary>日志记录器</summary>
        protected readonly ILogger Logger;

        /// <summary>会话管理器（可选）</summary>
        protected readonly IMafAiSessionStore? SessionStore;

        #region 监控指标

        private static readonly Meter MafMeter = new Meter("CKY.MultiAgentFramework");
        private static readonly Counter<long> InvocationsCounter =
            MafMeter.CreateCounter<long>("maf_agent_invocations_total", "invocations", "Total agent invocations");
        private static readonly Histogram<double> DurationHistogram =
            MafMeter.CreateHistogram<double>("maf_agent_duration_seconds", "s", "Agent duration");
        private static readonly Counter<long> LlmApiCallsCounter =
            MafMeter.CreateCounter<long>("maf_llm_api_calls_total", "calls", "Total LLM API calls");
        private static readonly Histogram<double> LlmLatencyHistogram =
            MafMeter.CreateHistogram<double>("maf_llm_api_latency_seconds", "s", "LLM API latency");

        #endregion

        /// <summary>
        /// 构造函数
        /// </summary>
        protected MafAiAgent(
            LlmProviderConfig config,
            ILogger logger,
            IMafAiSessionStore? sessionStore = null)
        {
            Config = config ?? throw new ArgumentNullException(nameof(config));
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            SessionStore = sessionStore;

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

        #region AIAgent 抽象方法实现

        /// <summary>
        /// 创建会话 - 使用 MafAgentSession
        /// </summary>
        protected override ValueTask<AgentSession> CreateSessionCoreAsync(CancellationToken cancellationToken = default)
        {
            var session = new MafAgentSession(SessionStore);
            return ValueTask.FromResult((AgentSession)session);
        }

        /// <summary>
        /// 序列化会话 - 序列化 MafSessionState 数据
        /// </summary>
        protected override ValueTask<JsonElement> SerializeSessionCoreAsync(
            AgentSession session,
            System.Text.Json.JsonSerializerOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            if (session is MafAgentSession mafSession)
            {
                // 序列化 MAF 会话数据
                var mafData = mafSession.MafSession;
                return ValueTask.FromResult(JsonSerializer.SerializeToElement(new
                {
                    SessionId = mafData.SessionId,
                    UserId = mafData.UserId,
                    CreatedAt = mafData.CreatedAt,
                    LastActivityAt = mafData.LastActivityAt,
                    ExpiresAt = mafData.ExpiresAt,
                    TotalTokensUsed = mafData.TotalTokensUsed,
                    TurnCount = mafData.TurnCount,
                    Status = (int)mafData.Status,
                    Metadata = mafData.Metadata,
                    Items = mafData.Items
                }, options));
            }

            // 默认序列化
            return ValueTask.FromResult(JsonSerializer.SerializeToElement(new { }));
        }

        /// <summary>
        /// 反序列化会话 - 恢复 MafSessionState 数据
        /// </summary>
        protected override ValueTask<AgentSession> DeserializeSessionCoreAsync(
            JsonElement serializedSession,
            System.Text.Json.JsonSerializerOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            var session = new MafAgentSession(SessionStore);

            // 尝试反序列化 MAF 会话数据
            try
            {
                if (serializedSession.ValueKind == JsonValueKind.Object)
                {
                    var sessionId = serializedSession.GetProperty("SessionId").GetString();
                    var userId = serializedSession.GetProperty("UserId").GetString();
                    var createdAt = serializedSession.GetProperty("CreatedAt").GetDateTime();
                    var lastActivityAt = serializedSession.GetProperty("LastActivityAt").GetDateTime();
                    var expiresAt = serializedSession.TryGetProperty("ExpiresAt", out var expiresAtElem)
                        ? expiresAtElem.GetDateTime() : (DateTime?)null;
                    var totalTokensUsed = serializedSession.GetProperty("TotalTokensUsed").GetInt64();
                    var turnCount = serializedSession.GetProperty("TurnCount").GetInt32();
                    var status = serializedSession.GetProperty("Status").GetInt32();

                    // 恢复 MafSessionState 数据
                    session.MafSession.SessionId = sessionId ?? string.Empty;
                    session.MafSession.UserId = userId ?? string.Empty;
                    session.MafSession.CreatedAt = createdAt;
                    session.MafSession.LastActivityAt = lastActivityAt;
                    session.MafSession.ExpiresAt = expiresAt;
                    session.MafSession.TotalTokensUsed = totalTokensUsed;
                    session.MafSession.TurnCount = turnCount;
                    session.MafSession.Status = (SessionStatus)status;
                }
            }
            catch (Exception ex)
            {
                // 反序列化失败，返回新会话
                Logger.LogWarning(ex, "[MafAiAgent] Failed to deserialize session, creating new one");
            }

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
            var stopwatch = Stopwatch.StartNew();
            var agentSuccess = false;

            try
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

                // 会话状态管理（使用 MafAgentSession）
                MafAgentSession? mafAgentSession = null;
                if (session is MafAgentSession mafSession)
                {
                    mafAgentSession = mafSession;

                    // 检查是否已有会话数据
                    if (!string.IsNullOrEmpty(mafAgentSession.MafSession.SessionId))
                    {
                        // 会话已存在，更新活动
                        mafAgentSession.UpdateActivity();
                        mafAgentSession.IncrementTurn();
                    }
                    else
                    {
                        // 新会话，生成会话 ID
                        mafAgentSession.MafSession.SessionId = Guid.NewGuid().ToString();
                    }
                }

                // LLM API 调用监控
                var llmStopwatch = Stopwatch.StartNew();
                string responseText;

                try
                {
                    // 调用子类实现的 ExecuteAsync 方法
                    responseText = await ExecuteAsync(
                        Config.ModelId,
                        prompt,
                        systemPrompt,
                        cancellationToken);

                    llmStopwatch.Stop();

                    // 记录 LLM API 调用指标
                    RecordLlmApiCall(success: true, latency: llmStopwatch.Elapsed);
                }
                catch (Exception ex)
                {
                    llmStopwatch.Stop();
                    RecordLlmApiCall(success: false, latency: llmStopwatch.Elapsed);
                    Logger.LogError(ex, "[MafAiAgent] LLM API call failed");
                    throw;
                }

                // 更新会话状态（如果启用）
                if (mafAgentSession != null)
                {
                    var estimatedTokens = EstimateTokenCount(prompt) + EstimateTokenCount(responseText);
                    mafAgentSession.UpdateActivity(estimatedTokens);

                    // 保存会话状态
                    await mafAgentSession.SaveAsync(cancellationToken);

                    // 保存聊天历史
                    var chatHistory = new List<ChatMessage> { userMessage, new ChatMessage(ChatRole.Assistant, responseText) };
                    await mafAgentSession.SaveChatHistoryAsync(chatHistory, cancellationToken);

                    Logger.LogDebug("[MafAiAgent] Session updated: {SessionId}, Turns: {TurnCount}, Tokens: {TokenCount}",
                        mafAgentSession.MafSession.SessionId,
                        mafAgentSession.MafSession.TurnCount,
                        mafAgentSession.MafSession.TotalTokensUsed);
                }

                // 构建响应消息列表
                var responseMessages = new List<ChatMessage>
                {
                    new ChatMessage(ChatRole.Assistant, responseText)
                };

                agentSuccess = true;
                stopwatch.Stop();

                // 返回 AgentResponse
                return new AgentResponse(responseMessages);
            }
            finally
            {
                // 记录 Agent 执行指标
                RecordAgentExecution(success: agentSuccess, duration: stopwatch.Elapsed);
            }
        }

        /// <summary>
        /// 运行 Agent（流式）
        /// </summary>
        protected override async IAsyncEnumerable<AgentResponseUpdate> RunCoreStreamingAsync(
            IEnumerable<ChatMessage> messages,
            AgentSession? session = null,
            AgentRunOptions? options = null,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
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

            // 会话状态管理（使用 MafAgentSession）
            MafAgentSession? mafAgentSession = null;
            if (session is MafAgentSession mafSession)
            {
                mafAgentSession = mafSession;

                // 检查是否已有会话数据
                if (!string.IsNullOrEmpty(mafAgentSession.MafSession.SessionId))
                {
                    // 会话已存在，更新活动
                    mafAgentSession.UpdateActivity();
                    mafAgentSession.IncrementTurn();
                }
                else
                {
                    // 新会话，生成会话 ID
                    mafAgentSession.MafSession.SessionId = Guid.NewGuid().ToString();
                }
            }

            // 调用流式 LLM API（由子类实现）
            await foreach (var chunk in ExecuteStreamingAsync(
                Config.ModelId,
                prompt,
                systemPrompt,
                cancellationToken))
            {
                // 流式场景下更新会话状态（Token 统计）需要在此处累计 token 数

                // 返回流式更新
                yield return new AgentResponseUpdate
                {
                    // 流式更新内容需根据 MS AF AgentResponseUpdate 结构设置（参考 MS AF 文档）
                };
            }

            // 流式完成后保存会话状态
            if (mafAgentSession != null)
            {
                await mafAgentSession.SaveAsync(cancellationToken);
            }
        }

        #endregion

        #region LLM 调用核心方法

        /// <summary>
        /// 执行 LLM 调用（核心抽象方法 - 仅仅是厂商 API 调用）
        /// </summary>
        /// <param name="modelId">模型 ID（如 glm-4, qwen-max）</param>
        /// <param name="prompt">提示词</param>
        /// <param name="systemPrompt">系统提示词（可选）</param>
        /// <param name="ct">取消令牌</param>
        /// <returns>LLM 响应文本</returns>
        /// <remarks>
        /// 注意：
        /// - 此方法仅仅是底层厂商 API 的调用封装
        /// - 不处理会话状态、不处理消息格式转换
        /// - 会话状态管理由 RunCoreAsync/RunCoreStreamingAsync 处理
        /// - 场景（Scenario）在 Agent 创建时已经通过 LlmProviderConfig 确定
        /// - 外部调用请使用 MS AF 的 RunAsync/RunStreamingAsync 或 MafBusinessAgentBase 的 CallLlmAsync
        /// </remarks>
        public abstract Task<string> ExecuteAsync(
            string modelId,
            string prompt,
            string? systemPrompt = null,
            CancellationToken ct = default);

        /// <summary>
        /// 执行流式 LLM 调用（核心抽象方法 - 仅仅是厂商 API 调用）
        /// </summary>
        /// <param name="modelId">模型 ID（如 glm-4, qwen-max）</param>
        /// <param name="prompt">提示词</param>
        /// <param name="systemPrompt">系统提示词（可选）</param>
        /// <param name="ct">取消令牌</param>
        /// <returns>LLM 流式响应（文本块）</returns>
        /// <remarks>
        /// 注意：
        /// - 此方法仅仅是底层厂商流式 API 的调用封装
        /// - 不处理会话状态、不处理消息格式转换
        /// - 会话状态管理由 RunCoreStreamingAsync 处理
        /// - 如果厂商不支持流式，子类可以返回单个块
        /// - 外部调用请使用 MS AF 的 RunStreamingAsync
        /// </remarks>
        public abstract IAsyncEnumerable<string> ExecuteStreamingAsync(
            string modelId,
            string prompt,
            string? systemPrompt = null,
            CancellationToken ct = default);

        /// <summary>
        /// 批量执行 LLM 调用（用于并行处理多个请求）
        /// </summary>
        internal virtual async Task<string[]> ExecuteBatchAsync(
            string modelId,
            string[] prompts,
            string? systemPrompt = null,
            CancellationToken ct = default)
        {
            if (prompts == null || prompts.Length == 0)
                return Array.Empty<string>();

            // 并行执行多个请求
            var tasks = prompts.Select(prompt =>
                ExecuteAsync(modelId, prompt, systemPrompt, ct));

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

        /// <summary>
        /// 估算文本的 Token 数量（粗略估算）
        /// </summary>
        /// <remarks>
        /// 这是一个简化的估算方法，实际 Token 数量取决于：
        /// - 模型的分词器
        /// - 文本的语言和内容
        /// - 特殊字符和空格
        ///
        /// 估算规则：
        /// - 英文：约 4 个字符 = 1 token
        /// - 中文：约 1.5 个汉字 = 1 token
        /// - 代码：约 3-4 个字符 = 1 token（更多关键字和标点）
        /// </remarks>
        protected int EstimateTokenCount(string text)
        {
            if (string.IsNullOrEmpty(text))
                return 0;

            // 简化的估算方法：中文字符 + 英文单词数
            var chineseCharCount = text.Count(c => c >= 0x4E00 && c <= 0x9FFF);
            var nonChineseText = System.Text.RegularExpressions.Regex.Replace(text, @"[\u4e00-\u9fff]", " ");
            var wordCount = nonChineseText.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;

            // 中文按 1.5 字符 = 1 token，英文按 4 字符 = 1 token（粗略）
            var estimatedTokens = (int)(chineseCharCount / 1.5) + wordCount;

            return Math.Max(1, estimatedTokens); // 至少 1 个 token
        }

        /// <summary>
        /// 记录 Agent 执行指标
        /// </summary>
        private void RecordAgentExecution(bool success, TimeSpan duration)
        {
            var tags = new TagList
            {
                { "agent", AgentId },
                { "provider", Config.ProviderName },
                { "model", Config.ModelId },
                { "success", success }
            };

            InvocationsCounter.Add(1, tags);
            DurationHistogram.Record(duration.TotalSeconds, tags);

            Logger.LogDebug("[MafMonitoring] Agent: {AgentId}, Success: {Success}, Duration: {Duration}s",
                AgentId, success, duration.TotalSeconds);
        }

        /// <summary>
        /// 记录 LLM API 调用指标
        /// </summary>
        private void RecordLlmApiCall(bool success, TimeSpan latency)
        {
            var tags = new TagList
            {
                { "provider", Config.ProviderName },
                { "model", Config.ModelId },
                { "success", success }
            };

            LlmApiCallsCounter.Add(1, tags);
            LlmLatencyHistogram.Record(latency.TotalSeconds, tags);

            Logger.LogDebug("[MafMonitoring] LLM API: {Provider}/{Model}, Success: {Success}, Latency: {Latency}s",
                Config.ProviderName, Config.ModelId, success, latency.TotalSeconds);
        }

        #endregion
    }
}
