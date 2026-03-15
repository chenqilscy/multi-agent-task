namespace CKY.MultiAgentFramework.Demos.CustomerService.Models
{
    /// <summary>
    /// 客服响应模型
    /// </summary>
    public class CustomerServiceResponse
    {
        public bool Success { get; set; }
        public string Result { get; set; } = string.Empty;
        public string? Error { get; set; }
        public bool NeedsClarification { get; set; }
        public string? ClarificationQuestion { get; set; }
        public List<string> ClarificationOptions { get; set; } = new();
        public bool ShouldEscalateToHuman { get; set; }
        public string? AgentName { get; set; }
    }

    /// <summary>
    /// 对话轮次（用于多轮会话管理）
    /// </summary>
    public class ConversationTurn
    {
        public string Role { get; set; } = string.Empty; // "user" or "assistant"
        public string Content { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string? Intent { get; set; }
        public Dictionary<string, string> Entities { get; set; } = new();
    }

    /// <summary>
    /// 会话上下文（多轮对话状态）
    /// </summary>
    public class ConversationContext
    {
        public string SessionId { get; set; } = Guid.NewGuid().ToString();
        public string UserId { get; set; } = string.Empty;

        /// <summary>完整历史（较短对话）</summary>
        public List<ConversationTurn> FullHistory { get; set; } = new();

        /// <summary>压缩后的历史摘要（长对话时使用）</summary>
        public string? HistorySummary { get; set; }

        /// <summary>最近N轮（长对话时保留近期详细内容）</summary>
        public List<ConversationTurn> RecentHistory { get; set; } = new();

        /// <summary>重要实体记忆（跨轮次保持）</summary>
        public Dictionary<string, string> ImportantEntities { get; set; } = new();

        /// <summary>当前话题（意图状态）</summary>
        public string? CurrentTopic { get; set; }

        public DateTime LastActiveAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// 多轮对话管理器
    /// 解决长会话中的上下文丢失、指代消解、意图漂移等问题
    /// </summary>
    public class ConversationManager
    {
        private readonly Dictionary<string, ConversationContext> _sessions = new();
        private const int MaxFullHistoryTurns = 20;
        private const int RecentHistoryTurns = 5;

        /// <summary>获取或创建会话上下文</summary>
        public ConversationContext GetOrCreateContext(string sessionId, string userId)
        {
            if (!_sessions.TryGetValue(sessionId, out var context))
            {
                context = new ConversationContext
                {
                    SessionId = sessionId,
                    UserId = userId,
                };
                _sessions[sessionId] = context;
            }
            return context;
        }

        /// <summary>
        /// 添加对话轮次，超过阈值时自动压缩
        /// 解决问题：Token 溢出、上下文丢失
        /// </summary>
        public void AddTurn(string sessionId, ConversationTurn turn)
        {
            if (!_sessions.TryGetValue(sessionId, out var context))
                return;

            context.FullHistory.Add(turn);
            context.LastActiveAt = DateTime.UtcNow;

            // 更新重要实体记忆
            foreach (var entity in turn.Entities)
                context.ImportantEntities[entity.Key] = entity.Value;

            // 超过阈值时压缩历史
            if (context.FullHistory.Count > MaxFullHistoryTurns)
            {
                CompressHistory(context);
            }
        }

        /// <summary>
        /// 从上下文中推断缺失实体
        /// 解决问题：指代消解（"那个订单"→具体订单ID）
        /// </summary>
        public Dictionary<string, string> InferMissingEntities(
            string sessionId, Dictionary<string, string> currentEntities)
        {
            if (!_sessions.TryGetValue(sessionId, out var context))
                return currentEntities;

            var result = new Dictionary<string, string>(currentEntities);

            // 从重要实体记忆中补充缺失实体
            foreach (var entity in context.ImportantEntities)
            {
                result.TryAdd(entity.Key, entity.Value);
            }

            return result;
        }

        /// <summary>
        /// 压缩历史对话（解决Token溢出问题）
        /// 策略：保留最近N轮详细内容 + 早期对话关键信息摘要
        /// </summary>
        private static void CompressHistory(ConversationContext context)
        {
            var toCompress = context.FullHistory
                .Take(context.FullHistory.Count - RecentHistoryTurns)
                .ToList();

            // 生成摘要（生产环境用LLM生成，这里用简单规则）
            var summary = GenerateSimpleSummary(toCompress, context.HistorySummary);

            context.HistorySummary = summary;
            context.RecentHistory = context.FullHistory.TakeLast(RecentHistoryTurns).ToList();
            context.FullHistory = context.RecentHistory.ToList();
        }

        private static string GenerateSimpleSummary(
            List<ConversationTurn> history, string? existingSummary)
        {
            var keyPoints = new List<string>();

            if (!string.IsNullOrEmpty(existingSummary))
                keyPoints.Add(existingSummary);

            // 提取用户主要诉求
            var userTurns = history.Where(t => t.Role == "user").ToList();
            if (userTurns.Count > 0)
            {
                keyPoints.Add($"用户曾询问：{string.Join("；", userTurns.Take(3).Select(t => t.Content))}");
            }

            // 提取重要实体
            var entities = history
                .SelectMany(t => t.Entities)
                .GroupBy(e => e.Key)
                .ToDictionary(g => g.Key, g => g.Last().Value);

            if (entities.Count > 0)
            {
                keyPoints.Add($"已知信息：{string.Join("，", entities.Select(e => $"{e.Key}={e.Value}"))}");
            }

            return string.Join("\n", keyPoints);
        }
    }
}
