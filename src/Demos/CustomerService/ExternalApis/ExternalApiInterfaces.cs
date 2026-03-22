namespace CKY.MultiAgentFramework.Demos.CustomerService.ExternalApis;

// ============================
// 外部订单系统
// ============================

/// <summary>外部订单查询响应</summary>
public class ExternalOrderResponse
{
    public string OrderId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string? TrackingNumber { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<ExternalOrderItemResponse> Items { get; set; } = new();
}

public class ExternalOrderItemResponse
{
    public string ProductId { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}

/// <summary>外部取消订单响应</summary>
public class ExternalCancelResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}

/// <summary>外部退款响应</summary>
public class ExternalRefundResponse
{
    public bool Success { get; set; }
    public string? RefundId { get; set; }
    public string Message { get; set; } = string.Empty;
    public decimal RefundAmount { get; set; }
    public int EstimatedDays { get; set; }
}

/// <summary>
/// 外部订单系统 API 接口
/// 后续对接真实 ERP 系统时，替换此实现
/// </summary>
public interface IExternalOrderApi
{
    /// <summary>查询订单</summary>
    Task<ExternalOrderResponse?> QueryOrderAsync(string orderId, CancellationToken ct = default);

    /// <summary>查询用户订单列表</summary>
    Task<List<ExternalOrderResponse>> QueryUserOrdersAsync(string userId, int pageSize = 10, CancellationToken ct = default);

    /// <summary>取消订单</summary>
    Task<ExternalCancelResponse> CancelOrderAsync(string orderId, string reason, CancellationToken ct = default);

    /// <summary>申请退款</summary>
    Task<ExternalRefundResponse> RequestRefundAsync(string orderId, decimal amount, string reason, CancellationToken ct = default);
}

// ============================
// 外部物流系统
// ============================

/// <summary>外部物流追踪响应</summary>
public class ExternalTrackingResponse
{
    public string TrackingNumber { get; set; } = string.Empty;
    public string CurrentLocation { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? EstimatedDelivery { get; set; }
    public List<ExternalTrackingEvent> Events { get; set; } = new();
}

public class ExternalTrackingEvent
{
    public DateTime Timestamp { get; set; }
    public string Location { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// 外部物流系统 API 接口
/// 后续对接真实物流平台时，替换此实现
/// </summary>
public interface IExternalLogisticsApi
{
    /// <summary>查询物流轨迹</summary>
    Task<ExternalTrackingResponse?> GetTrackingAsync(string trackingNumber, CancellationToken ct = default);
}

// ============================
// 外部支付系统
// ============================

/// <summary>支付查询响应</summary>
public class ExternalPaymentStatusResponse
{
    public string PaymentId { get; set; } = string.Empty;
    public string OrderId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty; // paid/refunding/refunded/failed
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public DateTime? PaidAt { get; set; }
    public DateTime? RefundedAt { get; set; }
}

/// <summary>
/// 外部支付系统 API 接口
/// 后续对接真实支付网关时，替换此实现
/// </summary>
public interface IExternalPaymentApi
{
    /// <summary>查询支付状态</summary>
    Task<ExternalPaymentStatusResponse?> GetPaymentStatusAsync(string orderId, CancellationToken ct = default);

    /// <summary>发起退款</summary>
    Task<ExternalRefundResponse> InitiateRefundAsync(string orderId, decimal amount, string reason, CancellationToken ct = default);
}
