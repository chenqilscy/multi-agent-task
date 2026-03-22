using CKY.MultiAgentFramework.Demos.CustomerService.Data;
using CKY.MultiAgentFramework.Demos.CustomerService.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CKY.MultiAgentFramework.Demos.CustomerService.Services.Implementations;

/// <summary>
/// 持久化工单服务 - EF Core 实现
/// </summary>
public class PersistentTicketService : ITicketService
{
    private readonly CustomerServiceDbContext _db;
    private readonly ILogger<PersistentTicketService> _logger;

    public PersistentTicketService(CustomerServiceDbContext db, ILogger<PersistentTicketService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<string> CreateTicketAsync(TicketCreateRequest request, CancellationToken ct = default)
    {
        var ticketId = $"TKT-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(100, 999)}";

        // 查找关联的客户
        var customer = await _db.Customers.FirstOrDefaultAsync(c => c.CustomerId == request.UserId, ct);

        var ticket = new TicketEntity
        {
            TicketId = ticketId,
            CustomerId = customer?.Id ?? 0,
            Title = request.Title,
            Description = request.Description,
            Category = request.Category,
            Priority = request.Priority,
            Status = "open",
            RelatedOrderId = request.RelatedOrderId,
            CreatedAt = DateTime.UtcNow
        };

        // 如果客户不存在，创建一个基础记录
        if (customer == null)
        {
            customer = new CustomerEntity
            {
                CustomerId = request.UserId,
                Name = request.UserId,
                CreatedAt = DateTime.UtcNow
            };
            _db.Customers.Add(customer);
            await _db.SaveChangesAsync(ct);
            ticket.CustomerId = customer.Id;
        }

        _db.Tickets.Add(ticket);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("工单已创建: {TicketId}, 客户: {CustomerId}", ticketId, request.UserId);
        return ticketId;
    }

    public async Task<TicketInfo?> GetTicketAsync(string ticketId, CancellationToken ct = default)
    {
        var ticket = await _db.Tickets
            .Include(t => t.Comments)
            .Include(t => t.Customer)
            .FirstOrDefaultAsync(t => t.TicketId == ticketId, ct);

        return ticket == null ? null : MapToTicketInfo(ticket);
    }

    public async Task<bool> UpdateTicketAsync(string ticketId, TicketUpdateRequest update, CancellationToken ct = default)
    {
        var ticket = await _db.Tickets
            .Include(t => t.Comments)
            .FirstOrDefaultAsync(t => t.TicketId == ticketId, ct);

        if (ticket == null) return false;

        if (!string.IsNullOrEmpty(update.Status))
        {
            ticket.Status = update.Status;
            if (update.Status == "resolved")
                ticket.ResolvedAt = DateTime.UtcNow;
        }

        if (!string.IsNullOrEmpty(update.Comment))
        {
            ticket.Comments.Add(new TicketCommentEntity
            {
                Author = "客服系统",
                Content = update.Comment,
                IsStaff = true,
                CreatedAt = DateTime.UtcNow
            });
        }

        ticket.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("工单已更新: {TicketId}", ticketId);
        return true;
    }

    public async Task<List<TicketInfo>> GetUserTicketsAsync(string userId, CancellationToken ct = default)
    {
        var tickets = await _db.Tickets
            .Include(t => t.Comments)
            .Include(t => t.Customer)
            .Where(t => t.Customer.CustomerId == userId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(ct);

        return tickets.Select(MapToTicketInfo).ToList();
    }

    private static TicketInfo MapToTicketInfo(TicketEntity ticket) => new()
    {
        TicketId = ticket.TicketId,
        UserId = ticket.Customer?.CustomerId ?? string.Empty,
        Title = ticket.Title,
        Description = ticket.Description,
        Category = ticket.Category,
        Priority = ticket.Priority,
        Status = ticket.Status,
        CreatedAt = ticket.CreatedAt,
        UpdatedAt = ticket.UpdatedAt,
        ResolvedAt = ticket.ResolvedAt,
        Comments = ticket.Comments.Select(c => new TicketComment
        {
            Author = c.Author,
            Content = c.Content,
            IsStaff = c.IsStaff,
            CreatedAt = c.CreatedAt
        }).ToList()
    };
}
