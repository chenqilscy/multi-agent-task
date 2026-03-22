namespace CKY.MultiAgentFramework.Demos.CustomerService.Entities;

/// <summary>
/// 工单实体
/// </summary>
public class TicketEntity
{
    public int Id { get; set; }

    /// <summary>业务工单ID（如 TKT-20260319-001）</summary>
    public string TicketId { get; set; } = string.Empty;

    public int CustomerId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    /// <summary>order/product/payment/shipping/other</summary>
    public string Category { get; set; } = "other";

    /// <summary>low/normal/high/urgent</summary>
    public string Priority { get; set; } = "normal";

    /// <summary>open/in_progress/resolved/closed</summary>
    public string Status { get; set; } = "open";

    public string? RelatedOrderId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public DateTime? ResolvedAt { get; set; }

    // 导航属性
    public CustomerEntity Customer { get; set; } = null!;
    public List<TicketCommentEntity> Comments { get; set; } = new();
}

/// <summary>
/// 工单评论实体
/// </summary>
public class TicketCommentEntity
{
    public int Id { get; set; }

    public int TicketEntityId { get; set; }

    public string Author { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public bool IsStaff { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // 导航属性
    public TicketEntity Ticket { get; set; } = null!;
}
