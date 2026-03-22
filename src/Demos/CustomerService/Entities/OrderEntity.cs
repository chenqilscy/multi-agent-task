namespace CKY.MultiAgentFramework.Demos.CustomerService.Entities;

/// <summary>
/// 订单实体
/// </summary>
public class OrderEntity
{
    public int Id { get; set; }

    /// <summary>业务订单ID（如 ORD-2024-001）</summary>
    public string OrderId { get; set; } = string.Empty;

    public int CustomerId { get; set; }

    /// <summary>pending/paid/shipped/delivered/cancelled</summary>
    public string Status { get; set; } = "pending";

    public decimal TotalAmount { get; set; }

    public string? TrackingNumber { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    // 导航属性
    public CustomerEntity Customer { get; set; } = null!;
    public List<OrderItemEntity> Items { get; set; } = new();
}

/// <summary>
/// 订单商品实体
/// </summary>
public class OrderItemEntity
{
    public int Id { get; set; }

    public int OrderEntityId { get; set; }

    public string ProductId { get; set; } = string.Empty;

    public string ProductName { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    // 导航属性
    public OrderEntity Order { get; set; } = null!;
}

/// <summary>
/// 物流追踪事件实体
/// </summary>
public class TrackingEventEntity
{
    public int Id { get; set; }

    public int OrderEntityId { get; set; }

    public string TrackingNumber { get; set; } = string.Empty;

    public string Location { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public DateTime Timestamp { get; set; }

    // 导航属性
    public OrderEntity Order { get; set; } = null!;
}
