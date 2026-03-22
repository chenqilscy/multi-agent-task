namespace CKY.MultiAgentFramework.Demos.CustomerService.Entities;

/// <summary>
/// 客户信息实体
/// </summary>
public class CustomerEntity
{
    public int Id { get; set; }

    /// <summary>业务客户ID（如 CUST-001）</summary>
    public string CustomerId { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public string PreferredLanguage { get; set; } = "zh-CN";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? LastActiveAt { get; set; }

    // 导航属性
    public List<OrderEntity> Orders { get; set; } = new();
    public List<TicketEntity> Tickets { get; set; } = new();
    public List<ChatSessionEntity> ChatSessions { get; set; } = new();
}
