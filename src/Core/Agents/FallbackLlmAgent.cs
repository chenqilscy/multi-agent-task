using CKY.MultiAgentFramework.Core.Models.LLM;
using Microsoft.Extensions.Logging;

namespace CKY.MultiAgentFramework.Core.Agents
{
    /// <summary>
    /// 带有自动 Fallback 能力的 LLM Agent
    /// 当主 Agent 失败时，自动按优先级尝试备用 Agent
    /// </summary>
    /// <remarks>
    /// 设计模式：
    /// 1. 装饰器模式 - 包装主 Agent，添加 Fallback 能力
    /// 2. 责任链模式 - 按优先级依次尝试每个 Agent
    /// 3. 模板方法模式 - 在 ExecuteAsync 中实现 Fallback 逻辑
    ///
    /// 使用场景：
    /// - 关键任务需要高可用性
    /// - 需要在多个 LLM 提供商之间自动切换
    /// - 需要记录 Fallback 历史用于监控
    /// </remarks>
    public class FallbackLlmAgent : MafAiAgent
    {
        private readonly MafAiAgent _primaryAgent;
        private readonly List<MafAiAgent> _fallbackAgents;
        private readonly ILogger _logger;
        private readonly object _historyLock = new();

        /// <summary>
        /// Fallback 历史记录（用于监控和调试）
        /// 线程安全：通过 _historyLock 保护访问
        /// </summary>
        public List<FallbackAttempt> FallbackHistory { get; } = new();

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="primaryAgent">主 Agent（优先级最高）</param>
        /// <param name="fallbackAgents">备用 Agent 列表（按优先级排序）</param>
        /// <param name="logger">日志记录器</param>
        public FallbackLlmAgent(
            MafAiAgent primaryAgent,
            List<MafAiAgent> fallbackAgents,
            ILogger logger)
            : base(CreateFallbackConfig(primaryAgent, fallbackAgents), logger)
        {
            _primaryAgent = primaryAgent ?? throw new ArgumentNullException(nameof(primaryAgent));
            _fallbackAgents = fallbackAgents ?? throw new ArgumentNullException(nameof(fallbackAgents));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 创建 Fallback Agent 的组合配置
        /// </summary>
        private static LlmProviderConfig CreateFallbackConfig(
            MafAiAgent primaryAgent,
            List<MafAiAgent> fallbackAgents)
        {
            var allScenarios = new List<LlmScenario>();
            allScenarios.AddRange(primaryAgent.Config.SupportedScenarios);

            foreach (var agent in fallbackAgents)
            {
                allScenarios.AddRange(agent.Config.SupportedScenarios);
            }

            // 去重
            allScenarios = allScenarios.Distinct().OrderBy(s => s).ToList();

            return new LlmProviderConfig
            {
                ProviderName = "fallback-wrapper",
                ProviderDisplayName = $"Fallback Wrapper ({primaryAgent.Config.ProviderDisplayName} + {fallbackAgents.Count} fallbacks)",
                ApiBaseUrl = "internal://fallback",
                ApiKey = "internal",
                ModelId = "fallback-wrapper",
                ModelDisplayName = "Fallback Wrapper",
                SupportedScenarios = allScenarios,
                MaxTokens = primaryAgent.Config.MaxTokens,
                Temperature = primaryAgent.Config.Temperature,
                IsEnabled = true,
                Priority = 0, // 最高优先级
                AdditionalParameters = new Dictionary<string, object>
                {
                    ["fallbackCount"] = fallbackAgents.Count,
                    ["primaryProvider"] = primaryAgent.Config.ProviderName
                }
            };
        }

        /// <summary>
        /// 执行 LLM 调用（带自动 Fallback）
        /// </summary>
        public override async Task<string> ExecuteAsync(
            string modelId,
            string prompt,
            string? systemPrompt = null,
            CancellationToken ct = default)
        {
            var attempt = new FallbackAttempt
            {
                StartTime = DateTime.UtcNow,
                Prompt = string.IsNullOrEmpty(prompt) ? string.Empty :
                    (prompt.Length > 100 ? prompt[..100] : prompt)
            };

            // 收集所有 Agent（主 Agent + 备用 Agent）
            var allAgents = new List<MafAiAgent> { _primaryAgent };
            allAgents.AddRange(_fallbackAgents);

            // 依次尝试每个 Agent
            Exception? lastException = null;
            foreach (var agent in allAgents)
            {
                attempt.Attempts.Add(agent.AgentId);

                try
                {
                    _logger.LogInformation(
                        "[Fallback] Attempting agent {AgentId} ({AttemptCount}/{TotalCount})",
                        agent.AgentId,
                        attempt.Attempts.Count,
                        allAgents.Count);

                    var result = await agent.ExecuteAsync(modelId, prompt, systemPrompt, ct);

                    // 成功！
                    attempt.SuccessAgentId = agent.AgentId;
                    attempt.EndTime = DateTime.UtcNow;

                    lock (_historyLock)
                    {
                        FallbackHistory.Add(attempt);
                    }

                    if (agent.AgentId != _primaryAgent.AgentId)
                    {
                        _logger.LogWarning(
                            "[Fallback] Primary agent failed, succeeded with fallback {AgentId}",
                            agent.AgentId);
                    }
                    else
                    {
                        _logger.LogDebug(
                            "[Fallback] Primary agent succeeded");
                    }

                    return result;
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    _logger.LogWarning(ex,
                        "[Fallback] Agent {AgentId} failed: {Error}",
                        agent.AgentId,
                        ex.Message);

                    // 继续尝试下一个 Agent
                }
            }

            // 所有 Agent 都失败了
            attempt.ErrorMessage = $"All {allAgents.Count} agents failed. Last error: {lastException?.Message}";
            attempt.EndTime = DateTime.UtcNow;

            lock (_historyLock)
            {
                FallbackHistory.Add(attempt);
            }

            _logger.LogError(
                "[Fallback] All agents failed. Last error: {Error}",
                lastException?.Message);

            throw new InvalidOperationException(
                "All agents failed",
                lastException);
        }

        /// <summary>
        /// 执行流式 LLM 调用（带自动 Fallback）
        /// </summary>
        public override async IAsyncEnumerable<string> ExecuteStreamingAsync(
            string modelId,
            string prompt,
            string? systemPrompt = null,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
        {
            // TODO: Implement streaming support for Fallback
            // For now, fallback to non-streaming
            var result = await ExecuteAsync(modelId, prompt, systemPrompt, ct);
            yield return result;
        }

        /// <summary>
        /// 获取 Fallback 统计信息
        /// </summary>
        public FallbackStatistics GetStatistics()
        {
            var stats = new FallbackStatistics();

            List<FallbackAttempt> historySnapshot;
            lock (_historyLock)
            {
                if (FallbackHistory.Count == 0)
                {
                    return stats;
                }
                historySnapshot = FallbackHistory.ToList();
            }

            stats.TotalRequests = historySnapshot.Count;
            stats.SuccessfulRequests = historySnapshot.Count(h => h.SuccessAgentId != null);
            stats.FallbackRate = (double)historySnapshot.Count(h =>
                h.SuccessAgentId != null && h.SuccessAgentId != _primaryAgent.AgentId) / stats.TotalRequests;

            // 统计每个 Agent 的使用次数
            foreach (var history in historySnapshot)
            {
                if (history.SuccessAgentId != null)
                {
                    if (!stats.AgentUsageCounts.ContainsKey(history.SuccessAgentId))
                    {
                        stats.AgentUsageCounts[history.SuccessAgentId] = 0;
                    }
                    stats.AgentUsageCounts[history.SuccessAgentId]++;
                }
            }

            return stats;
        }

        /// <summary>
        /// 清空 Fallback 历史
        /// </summary>
        public void ClearHistory()
        {
            lock (_historyLock)
            {
                FallbackHistory.Clear();
            }
            _logger.LogInformation("[Fallback] History cleared");
        }
    }

    #region Fallback 相关数据模型

    /// <summary>
    /// Fallback 尝试记录
    /// </summary>
    public class FallbackAttempt
    {
        /// <summary>开始时间</summary>
        public DateTime StartTime { get; set; }

        /// <summary>结束时间</summary>
        public DateTime EndTime { get; set; }

        /// <summary>尝试的 Agent ID 列表（按顺序）</summary>
        public List<string> Attempts { get; set; } = new();

        /// <summary>成功的 Agent ID</summary>
        public string? SuccessAgentId { get; set; }

        /// <summary>错误消息（如果全部失败）</summary>
        public string? ErrorMessage { get; set; }

        /// <summary>提示词（前 100 个字符，用于调试）</summary>
        public string? Prompt { get; set; }

        /// <summary>总耗时</summary>
        public TimeSpan Duration => EndTime - StartTime;
    }

    /// <summary>
    /// Fallback 统计信息
    /// </summary>
    public class FallbackStatistics
    {
        /// <summary>总请求数</summary>
        public int TotalRequests { get; set; }

        /// <summary>成功请求数</summary>
        public int SuccessfulRequests { get; set; }

        /// <summary>Fallback 率（0-1）</summary>
        public double FallbackRate { get; set; }

        /// <summary>每个 Agent 的使用次数</summary>
        public Dictionary<string, int> AgentUsageCounts { get; set; } = new();

        /// <summary>成功率</summary>
        public double SuccessRate =>
            TotalRequests > 0 ? (double)SuccessfulRequests / TotalRequests : 0;
    }

    #endregion
}
