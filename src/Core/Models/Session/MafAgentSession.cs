using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace CKY.MultiAgentFramework.Core.Models.Session
{
    /// <summary>
    /// MAF Agent 会话管理类
    /// </summary>
    /// <remarks>
    /// 设计原则：
    /// 1. 继承 MS AF 的 AgentSession，充分利用框架能力
    /// 2. 扩展 MAF 特定的会话管理功能
    /// 3. 与 MafSessionState 配合使用，实现会话状态管理
    ///
    /// 架构关系：
    /// - MafAgentSession : AgentSession（MS AF 框架集成）
    ///   ├─ 包含：MafSessionState（会话状态数据）
    ///   └─ 管理：ChatHistory（聊天消息历史）
    ///
    /// 使用方式：
    /// - MS AF 框架会自动创建和管理 AgentSession
    /// - MafAiAgent 在 RunCoreAsync/RunCoreStreamingAsync 中使用此类
    /// - 自动集成 IMafAiSessionStore 实现持久化
    /// - 支持聊天历史的 L2+L3 存储策略
    /// </remarks>
    public class MafAgentSession : AgentSession
    {
        /// <summary>
        /// MAF 会话状态数据（包含状态、Token 统计等）
        /// </summary>
        private MafSessionState? _mafSession;

        /// <summary>
        /// 会话存储（用于持久化会话状态）
        /// </summary>
        private readonly Core.Abstractions.IMafAiSessionStore? _sessionStore;

        /// <summary>
        /// L2 缓存存储（用于快速访问聊天历史）
        /// </summary>
        private readonly Core.Abstractions.ICacheStore? _l2Cache;

        /// <summary>
        /// L3 数据库存储（用于持久化聊天历史）
        /// </summary>
        private readonly Core.Abstractions.IRelationalDatabase? _l3Database;

        /// <summary>
        /// L2 缓存 TTL（默认 24 小时）
        /// </summary>
        private static readonly TimeSpan L2CacheTtl = TimeSpan.FromHours(24);

        /// <summary>
        /// 构造函数
        /// </summary>
        public MafAgentSession(
            Core.Abstractions.IMafAiSessionStore? sessionStore = null,
            Core.Abstractions.ICacheStore? l2Cache = null,
            Core.Abstractions.IRelationalDatabase? l3Database = null)
        {
            _sessionStore = sessionStore;
            _l2Cache = l2Cache;
            _l3Database = l3Database;
        }

        /// <summary>
        /// 获取或创建 MAF 会话状态数据
        /// </summary>
        public MafSessionState MafSession
        {
            get
            {
                if (_mafSession == null)
                {
                    _mafSession = new MafSessionState
                    {
                        SessionId = Guid.NewGuid().ToString(),
                        CreatedAt = DateTime.UtcNow,
                        LastActivityAt = DateTime.UtcNow,
                        ExpiresAt = DateTime.UtcNow.AddHours(24)
                    };
                }
                return _mafSession;
            }
        }

        /// <summary>
        /// 加载现有会话
        /// </summary>
        public async System.Threading.Tasks.Task<bool> LoadAsync(string sessionId, CancellationToken cancellationToken = default)
        {
            if (_sessionStore == null)
                return false;

            var session = await _sessionStore.LoadAsync(sessionId, cancellationToken);
            if (session != null)
            {
                _mafSession = session;
                return true;
            }
            return false;
        }

        /// <summary>
        /// 保存会话状态
        /// </summary>
        public async System.Threading.Tasks.Task SaveAsync(CancellationToken cancellationToken = default)
        {
            if (_sessionStore == null || _mafSession == null)
                return;

            await _sessionStore.SaveAsync(_mafSession, cancellationToken);
        }

        /// <summary>
        /// 更新活跃时间并增加 Token 使用量
        /// </summary>
        public void UpdateActivity(int tokensUsed = 0)
        {
            if (_mafSession != null)
            {
                _mafSession.UpdateActivity();
                if (tokensUsed > 0)
                {
                    _mafSession.AddTokens(tokensUsed);
                }
            }
        }

        /// <summary>
        /// 增加对话轮次
        /// </summary>
        public void IncrementTurn()
        {
            _mafSession?.IncrementTurn();
        }

        /// <summary>
        /// 检查会话是否已过期
        /// </summary>
        public bool IsExpired => _mafSession?.IsExpired ?? false;

        /// <summary>
        /// 检查会话是否活跃
        /// </summary>
        public bool IsActive => _mafSession?.IsActive ?? false;

        #region 聊天历史管理

        /// <summary>
        /// 加载聊天历史（L2 + L3 分层加载）
        /// </summary>
        /// <param name="maxMessages">最大消息数量（默认 50）</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>聊天消息列表</returns>
        public async System.Threading.Tasks.Task<List<ChatMessage>> LoadChatHistoryAsync(
            int maxMessages = 50,
            CancellationToken cancellationToken = default)
        {
            var sessionId = MafSession.SessionId;

            // 1. 尝试从 Metadata 获取最近的消息（快速路径）
            if (MafSession.Metadata.TryGetValue("RecentChatHistory", out var historyObj) && historyObj is List<ChatMessage> recentHistory)
            {
                return recentHistory.Take(maxMessages).ToList();
            }

            // 2. 从 L2 缓存加载
            if (_l2Cache != null)
            {
                try
                {
                    var cacheKey = GetL2CacheKey(sessionId);
                    var cached = await _l2Cache.GetAsync<List<ChatMessage>>(cacheKey, cancellationToken);
                    if (cached != null && cached.Count > 0)
                    {
                        // 更新 Metadata 缓存
                        MafSession.Metadata["RecentChatHistory"] = cached.Take(maxMessages).ToList();
                        return cached.Take(maxMessages).ToList();
                    }
                }
                catch (Exception ex)
                {
                    // L2 缓存失败，继续尝试 L3
                    System.Diagnostics.Debug.WriteLine($"[MafAgentSession] L2 cache load failed: {ex.Message}");
                }
            }

            // 3. 从 L3 数据库加载
            if (_l3Database != null)
            {
                try
                {
                    var messages = await _l3Database.ExecuteSqlAsync<ChatMessage>(
                        "SELECT * FROM chat_messages WHERE session_id = @sid ORDER BY created_at DESC LIMIT @limit",
                        new { sid = sessionId, limit = maxMessages },
                        cancellationToken);

                    var messageList = messages.ToList();

                    // 反转顺序（最早的在前）
                    messageList.Reverse();

                    // 更新 L2 缓存和 Metadata
                    if (_l2Cache != null && messageList.Count > 0)
                    {
                        var cacheKey = GetL2CacheKey(sessionId);
                        await _l2Cache.SetAsync(cacheKey, messageList, L2CacheTtl, cancellationToken);
                    }

                    MafSession.Metadata["RecentChatHistory"] = messageList;
                    return messageList;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[MafAgentSession] L3 database load failed: {ex.Message}");
                }
            }

            // 4. 返回空列表
            return new List<ChatMessage>();
        }

        /// <summary>
        /// 保存聊天历史（同时更新 L2 和 L3）
        /// </summary>
        /// <param name="messages">聊天消息列表</param>
        /// <param name="cancellationToken">取消令牌</param>
        public async System.Threading.Tasks.Task SaveChatHistoryAsync(
            List<ChatMessage> messages,
            CancellationToken cancellationToken = default)
        {
            if (messages == null || messages.Count == 0)
                return;

            var sessionId = MafSession.SessionId;

            // 1. 保存到 L2 缓存（最近 50 条）
            if (_l2Cache != null)
            {
                try
                {
                    var cacheKey = GetL2CacheKey(sessionId);
                    var recentMessages = messages.TakeLast(50).ToList();
                    await _l2Cache.SetAsync(cacheKey, recentMessages, L2CacheTtl, cancellationToken);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[MafAgentSession] L2 cache save failed: {ex.Message}");
                }
            }

            // 2. 保存到 L3 数据库（完整历史）
            if (_l3Database != null)
            {
                try
                {
                    // 为每条消息添加会话 ID 和时间戳
                    var messageEntities = messages.Select(m => new ChatMessageEntity
                    {
                        SessionId = sessionId,
                        Role = m.Role.ToString(),
                        Content = m.Text ?? string.Empty,
                        CreatedAt = DateTime.UtcNow
                    });

                    await _l3Database.BulkInsertAsync(messageEntities, cancellationToken);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[MafAgentSession] L3 database save failed: {ex.Message}");
                }
            }

            // 3. 更新 Metadata（仅保留最近 50 条）
            MafSession.Metadata["RecentChatHistory"] = messages.TakeLast(50).ToList();
        }

        /// <summary>
        /// 添加单条消息到聊天历史
        /// </summary>
        /// <param name="message">聊天消息</param>
        /// <param name="cancellationToken">取消令牌</param>
        public async System.Threading.Tasks.Task AddMessageAsync(ChatMessage message, CancellationToken cancellationToken = default)
        {
            var currentHistory = await LoadChatHistoryAsync(maxMessages: 1000, cancellationToken);
            currentHistory.Add(message);
            await SaveChatHistoryAsync(currentHistory, cancellationToken);
        }

        /// <summary>
        /// 清除聊天历史（L2 和 L3）
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        public async System.Threading.Tasks.Task ClearChatHistoryAsync(CancellationToken cancellationToken = default)
        {
            var sessionId = MafSession.SessionId;

            // 1. 清除 L2 缓存
            if (_l2Cache != null)
            {
                try
                {
                    var cacheKey = GetL2CacheKey(sessionId);
                    await _l2Cache.DeleteAsync(cacheKey, cancellationToken);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[MafAgentSession] L2 cache clear failed: {ex.Message}");
                }
            }

            // 2. 清除 L3 数据库中的该会话消息
            if (_l3Database != null)
            {
                try
                {
                    await _l3Database.ExecuteSqlAsync<object>(
                        "DELETE FROM chat_messages WHERE session_id = @sid",
                        new { sid = sessionId },
                        cancellationToken);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[MafAgentSession] L3 database clear failed: {ex.Message}");
                }
            }

            // 3. 清除 Metadata
            MafSession.Metadata.Remove("RecentChatHistory");
        }

        /// <summary>
        /// 获取 L2 缓存键
        /// </summary>
        private static string GetL2CacheKey(string sessionId) => $"maf:chat:history:{sessionId}";

        #endregion
    }

    /// <summary>
    /// 聊天消息实体（用于数据库存储）
    /// </summary>
    public class ChatMessageEntity
    {
        public string SessionId { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
