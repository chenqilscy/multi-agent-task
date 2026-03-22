using System.Text.Json;
using CKY.MultiAgentFramework.Demos.CustomerService.Data;
using CKY.MultiAgentFramework.Demos.CustomerService.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CKY.MultiAgentFramework.Demos.CustomerService.Services.Implementations;

/// <summary>
/// 持久化对话历史服务 - EF Core 实现
/// </summary>
public class PersistentChatHistoryService : IChatHistoryService
{
    private readonly CustomerServiceDbContext _db;
    private readonly ILogger<PersistentChatHistoryService> _logger;

    public PersistentChatHistoryService(CustomerServiceDbContext db, ILogger<PersistentChatHistoryService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<string> CreateSessionAsync(string? customerId = null, CancellationToken ct = default)
    {
        var sessionId = Guid.NewGuid().ToString("N");

        int? customerDbId = null;
        if (!string.IsNullOrEmpty(customerId))
        {
            var customer = await _db.Customers.FirstOrDefaultAsync(c => c.CustomerId == customerId, ct);
            customerDbId = customer?.Id;
        }

        var session = new ChatSessionEntity
        {
            SessionId = sessionId,
            CustomerId = customerDbId,
            Status = "active",
            StartedAt = DateTime.UtcNow
        };

        _db.ChatSessions.Add(session);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("对话会话已创建: {SessionId}, 客户: {CustomerId}", sessionId, customerId ?? "匿名");
        return sessionId;
    }

    public async Task SaveMessageAsync(string sessionId, string role, string content,
        string? intent = null, Dictionary<string, string>? entities = null,
        CancellationToken ct = default)
    {
        var session = await _db.ChatSessions.FirstOrDefaultAsync(s => s.SessionId == sessionId, ct);
        if (session == null)
        {
            _logger.LogWarning("会话不存在: {SessionId}，自动创建", sessionId);
            session = new ChatSessionEntity
            {
                SessionId = sessionId,
                Status = "active",
                StartedAt = DateTime.UtcNow
            };
            _db.ChatSessions.Add(session);
            await _db.SaveChangesAsync(ct);
        }

        var message = new ChatMessageEntity
        {
            ChatSessionEntityId = session.Id,
            Role = role,
            Content = content,
            Intent = intent,
            EntitiesJson = entities != null ? JsonSerializer.Serialize(entities) : null,
            Timestamp = DateTime.UtcNow
        };

        _db.ChatMessages.Add(message);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<List<ChatMessageEntity>> GetSessionMessagesAsync(string sessionId,
        int? limit = null, CancellationToken ct = default)
    {
        var query = _db.ChatMessages
            .Where(m => m.ChatSession.SessionId == sessionId)
            .OrderBy(m => m.Timestamp)
            .AsQueryable();

        if (limit.HasValue)
            query = query.Take(limit.Value);

        return await query.ToListAsync(ct);
    }

    public async Task CloseSessionAsync(string sessionId, string? summary = null,
        CancellationToken ct = default)
    {
        var session = await _db.ChatSessions.FirstOrDefaultAsync(s => s.SessionId == sessionId, ct);
        if (session == null) return;

        session.Status = "closed";
        session.EndedAt = DateTime.UtcNow;
        session.Summary = summary;

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("对话会话已关闭: {SessionId}", sessionId);
    }

    public async Task<List<ChatSessionEntity>> GetCustomerSessionsAsync(string customerId,
        int pageSize = 20, CancellationToken ct = default)
    {
        return await _db.ChatSessions
            .Include(s => s.Customer)
            .Where(s => s.Customer != null && s.Customer.CustomerId == customerId)
            .OrderByDescending(s => s.StartedAt)
            .Take(pageSize)
            .ToListAsync(ct);
    }
}
