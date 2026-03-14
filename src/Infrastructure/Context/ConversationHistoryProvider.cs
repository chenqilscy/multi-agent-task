using Microsoft.Agents.AI;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace CKY.MultiAgentFramework.Infrastructure.Context
{
    /// <summary>
    /// 对话历史上下文提供器
    /// 为 LlmAgent 提供多轮对话的历史记录管理
    /// </summary>
    /// <remarks>
    /// 功能特性：
    /// - 会话隔离：每个 sessionId 独立维护对话历史
    /// - 自动清理：超过最大轮数的历史自动清理
    /// - 灵活配置：支持自定义最大历史轮数
    /// - 内存管理：支持设置过期时间自动清理
    ///
    /// 使用场景：
    /// - 智能客服多轮对话
    /// - 个人助理应用
    /// - 教学辅导系统
    /// </remarks>
    public class ConversationHistoryProvider : IAIContextProvider
    {
        private readonly ILogger<ConversationHistoryProvider> _logger;
        private readonly ConcurrentDictionary<string, Queue<ConversationMessage>> _sessions;
        private readonly ConcurrentDictionary<string, DateTime> _lastActivity;
        private readonly ConversationHistoryOptions _options;

        /// <summary>
        /// 构造函数
        /// </summary>
        public ConversationHistoryProvider(
            ILogger<ConversationHistoryProvider> logger,
            ConversationHistoryOptions? options = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options ?? new ConversationHistoryOptions();
            _sessions = new ConcurrentDictionary<string, Queue<ConversationMessage>>();
            _lastActivity = new ConcurrentDictionary<string, DateTime>();
        }

        /// <summary>
        /// 在 LLM 调用前加载对话历史
        /// </summary>
        public async Task<AIContext> PrepareContextAsync(AIContext currentContext, CancellationToken cancellationToken)
        {
            await Task.Yield();

            try
            {
                // 从上下文中提取 sessionId
                if (!currentContext.ContextData.TryGetValue("session_id", out var sessionIdObj) ||
                    sessionIdObj is not string sessionId)
                {
                    _logger.LogDebug("[ConversationHistory] 未找到 sessionId，跳过历史加载");
                    return currentContext;
                }

                // 获取会话历史
                var history = GetHistory(sessionId);
                if (history.Count == 0)
                {
                    _logger.LogDebug("[ConversationHistory] 会话 {SessionId} 无历史记录", sessionId);
                    return currentContext;
                }

                // 将历史消息注入到上下文中
                var historyMessages = history
                    .Select(msg => new ChatMessage(msg.Role, msg.Content))
                    .ToList();

                // MS AF 的 AIContext 支持直接设置 Messages
                // 这里我们将历史消息添加到现有消息之前
                var allMessages = historyMessages.Concat(currentContext.Messages).ToList();

                // 更新上下文的消息列表
                currentContext = currentContext with { Messages = allMessages };

                _logger.LogInformation(
                    "[ConversationHistory] 为会话 {SessionId} 加载了 {Count} 条历史消息",
                    sessionId,
                    history.Count);

                return currentContext;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ConversationHistory] 加载历史失败");
                return currentContext; // 降级：返回原始上下文
            }
        }

        /// <summary>
        /// 在 LLM 调用后保存当前对话
        /// </summary>
        public async Task<AIContext> ProcessContextAsync(AIContext context, AIResult result, CancellationToken cancellationToken)
        {
            await Task.Yield();

            try
            {
                // 从上下文中提取 sessionId
                if (!context.ContextData.TryGetValue("session_id", out var sessionIdObj) ||
                    sessionIdObj is not string sessionId)
                {
                    return context;
                }

                // 保存用户消息
                if (context.Input != null)
                {
                    AddMessage(sessionId, ChatRole.User, context.Input.Text ?? string.Empty);
                }

                // 保存助手响应
                if (result.Output != null)
                {
                    AddMessage(sessionId, ChatRole.Assistant, result.Output.Text ?? string.Empty);
                }

                // 更新最后活跃时间
                _lastActivity.AddOrUpdate(sessionId, _ => DateTime.UtcNow, (_, _) => DateTime.UtcNow);

                _logger.LogDebug("[ConversationHistory] 已保存会话 {SessionId} 的当前对话", sessionId);

                return context;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ConversationHistory] 保存对话失败");
                return context;
            }
        }

        /// <summary>
        /// 获取会话历史
        /// </summary>
        public IReadOnlyList<ConversationMessage> GetHistory(string sessionId)
        {
            if (_sessions.TryGetValue(sessionId, out var queue))
            {
                lock (queue)
                {
                    return queue.ToArray();
                }
            }
            return Array.Empty<ConversationMessage>();
        }

        /// <summary>
        /// 添加消息到历史
        /// </summary>
        public void AddMessage(string sessionId, ChatRole role, string content)
        {
            var queue = _sessions.GetOrAdd(sessionId, _ => new Queue<ConversationMessage>());

            lock (queue)
            {
                queue.Enqueue(new ConversationMessage
                {
                    Role = role,
                    Content = content,
                    Timestamp = DateTime.UtcNow
                });

                // 超过最大轮数时清理最旧的
                while (queue.Count > _options.MaxHistoryTurns)
                {
                    queue.Dequeue();
                }
            }
        }

        /// <summary>
        /// 清除指定会话的历史
        /// </summary>
        public void ClearSession(string sessionId)
        {
            _sessions.TryRemove(sessionId, out _);
            _lastActivity.TryRemove(sessionId, out _);
            _logger.LogInformation("[ConversationHistory] 已清除会话 {SessionId}", sessionId);
        }

        /// <summary>
        /// 清理过期的会话
        /// </summary>
        public void CleanupExpiredSessions()
        {
            if (_options.SessionExpiration <= TimeSpan.Zero)
            {
                return;
            }

            var now = DateTime.UtcNow;
            var expiredSessions = _lastActivity
                .Where(kvp => (now - kvp.Value) > _options.SessionExpiration)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var sessionId in expiredSessions)
            {
                ClearSession(sessionId);
            }

            if (expiredSessions.Count > 0)
            {
                _logger.LogInformation(
                    "[ConversationHistory] 清理了 {Count} 个过期会话",
                    expiredSessions.Count);
            }
        }

        /// <summary>
        /// 获取当前会话数
        /// </summary>
        public int GetActiveSessionCount()
        {
            return _sessions.Count;
        }
    }

    /// <summary>
    /// 对话历史配置选项
    /// </summary>
    public class ConversationHistoryOptions
    {
        /// <summary>
        /// 最大历史轮数（默认 20）
        /// </summary>
        public int MaxHistoryTurns { get; set; } = 20;

        /// <summary>
        /// 会话过期时间（默认 24 小时）
        /// </summary>
        public TimeSpan SessionExpiration { get; set; } = TimeSpan.FromHours(24);

        /// <summary>
        /// 是否自动清理过期会话（默认 true）
        /// </summary>
        public bool AutoCleanup { get; set; } = true;
    }

    /// <summary>
    /// 对话消息记录
    /// </summary>
    public class ConversationMessage
    {
        /// <summary>
        /// 消息角色
        /// </summary>
        public ChatRole Role { get; set; }

        /// <summary>
        /// 消息内容
        /// </summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// 时间戳
        /// </summary>
        public DateTime Timestamp { get; set; }
    }
}
