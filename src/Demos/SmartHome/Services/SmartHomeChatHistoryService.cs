using CKY.MultiAgentFramework.Core.Abstractions;
using Microsoft.Extensions.Logging;

namespace CKY.MultiAgentFramework.Demos.SmartHome.Services;

/// <summary>
/// 智能家居对话历史持久化服务
/// 将对话历史存储到框架数据库的 ChatMessages 表中
/// </summary>
public sealed class SmartHomeChatHistoryService
{
    private readonly IRelationalDatabase _db;
    private readonly ILogger<SmartHomeChatHistoryService> _logger;

    public SmartHomeChatHistoryService(
        IRelationalDatabase db,
        ILogger<SmartHomeChatHistoryService> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// 创建新的对话会话，返回 SessionId
    /// </summary>
    public async Task<string> CreateSessionAsync(CancellationToken ct = default)
    {
        var sessionId = Guid.NewGuid().ToString();
        var sql = @"INSERT INTO MafAiSessions (SessionId, UserId, Status, CreatedAt, LastActivityAt, TotalTokensUsed, TurnCount)
                    VALUES (@SessionId, @UserId, @Status, @CreatedAt, @LastActivityAt, 0, 0)";
        await _db.ExecuteSqlAsync<int>(sql, new
        {
            SessionId = sessionId,
            UserId = "smarthome-demo",
            Status = "Active",
            CreatedAt = DateTime.UtcNow.ToString("o"),
            LastActivityAt = DateTime.UtcNow.ToString("o"),
        }, ct);

        _logger.LogDebug("Created SmartHome chat session: {SessionId}", sessionId);
        return sessionId;
    }

    /// <summary>
    /// 保存对话消息
    /// </summary>
    public async Task SaveMessageAsync(string sessionId, string role, string content, CancellationToken ct = default)
    {
        var sql = @"INSERT INTO ChatMessages (SessionId, Role, Content, CreatedAt)
                    VALUES (@SessionId, @Role, @Content, @CreatedAt)";
        await _db.ExecuteSqlAsync<int>(sql, new
        {
            SessionId = sessionId,
            Role = role,
            Content = content,
            CreatedAt = DateTime.UtcNow.ToString("o"),
        }, ct);
    }

    /// <summary>
    /// 加载对话历史消息
    /// </summary>
    public async Task<List<ChatHistoryMessage>> LoadMessagesAsync(string sessionId, int limit = 50, CancellationToken ct = default)
    {
        var sql = @"SELECT Role, Content, CreatedAt FROM ChatMessages
                    WHERE SessionId = @SessionId
                    ORDER BY Id ASC
                    LIMIT @Limit";
        return await _db.ExecuteSqlAsync<ChatHistoryMessage>(sql, new
        {
            SessionId = sessionId,
            Limit = limit,
        }, ct);
    }
}

/// <summary>
/// 对话历史消息 DTO
/// </summary>
public class ChatHistoryMessage
{
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string CreatedAt { get; set; } = string.Empty;
}
