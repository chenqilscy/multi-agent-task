using CKY.MultiAgentFramework.Demos.CustomerService.Data;
using Microsoft.EntityFrameworkCore;

namespace CKY.MultiAgentFramework.Demos.CustomerService.ExternalApis;

/// <summary>
/// 模拟外部订单系统 API（从本地数据库读取，模拟 ERP 调用）
/// </summary>
public class MockExternalOrderApi : IExternalOrderApi
{
    private readonly CustomerServiceDbContext _db;

    public MockExternalOrderApi(CustomerServiceDbContext db)
    {
        _db = db;
    }

    public async Task<ExternalOrderResponse?> QueryOrderAsync(string orderId, CancellationToken ct = default)
    {
        var order = await _db.Orders
            .Include(o => o.Items)
            .Include(o => o.Customer)
            .FirstOrDefaultAsync(o => o.OrderId == orderId, ct);

        if (order == null) return null;

        return new ExternalOrderResponse
        {
            OrderId = order.OrderId,
            UserId = order.Customer.CustomerId,
            Status = order.Status,
            TotalAmount = order.TotalAmount,
            TrackingNumber = order.TrackingNumber,
            CreatedAt = order.CreatedAt,
            Items = order.Items.Select(i => new ExternalOrderItemResponse
            {
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice
            }).ToList()
        };
    }

    public async Task<List<ExternalOrderResponse>> QueryUserOrdersAsync(string userId, int pageSize = 10, CancellationToken ct = default)
    {
        var orders = await _db.Orders
            .Include(o => o.Items)
            .Include(o => o.Customer)
            .Where(o => o.Customer.CustomerId == userId)
            .OrderByDescending(o => o.CreatedAt)
            .Take(pageSize)
            .ToListAsync(ct);

        return orders.Select(o => new ExternalOrderResponse
        {
            OrderId = o.OrderId,
            UserId = o.Customer.CustomerId,
            Status = o.Status,
            TotalAmount = o.TotalAmount,
            TrackingNumber = o.TrackingNumber,
            CreatedAt = o.CreatedAt,
            Items = o.Items.Select(i => new ExternalOrderItemResponse
            {
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice
            }).ToList()
        }).ToList();
    }

    public async Task<ExternalCancelResponse> CancelOrderAsync(string orderId, string reason, CancellationToken ct = default)
    {
        var order = await _db.Orders.FirstOrDefaultAsync(o => o.OrderId == orderId, ct);
        if (order == null)
            return new ExternalCancelResponse { Success = false, Message = "订单不存在" };

        if (order.Status is "shipped" or "delivered")
            return new ExternalCancelResponse { Success = false, Message = "已发货的订单无法取消，请走退货退款流程" };

        order.Status = "cancelled";
        order.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return new ExternalCancelResponse { Success = true, Message = "订单已成功取消" };
    }

    public async Task<ExternalRefundResponse> RequestRefundAsync(string orderId, decimal amount, string reason, CancellationToken ct = default)
    {
        var order = await _db.Orders.FirstOrDefaultAsync(o => o.OrderId == orderId, ct);
        if (order == null)
            return new ExternalRefundResponse { Success = false, Message = "订单不存在" };

        return new ExternalRefundResponse
        {
            Success = true,
            RefundId = $"RF-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}",
            Message = "退款申请已提交，预计3-5个工作日到账",
            RefundAmount = amount > 0 ? amount : order.TotalAmount,
            EstimatedDays = 5
        };
    }
}

/// <summary>
/// 模拟外部物流系统 API（从本地数据库读取物流事件）
/// </summary>
public class MockExternalLogisticsApi : IExternalLogisticsApi
{
    private readonly CustomerServiceDbContext _db;

    public MockExternalLogisticsApi(CustomerServiceDbContext db)
    {
        _db = db;
    }

    public async Task<ExternalTrackingResponse?> GetTrackingAsync(string trackingNumber, CancellationToken ct = default)
    {
        var events = await _db.TrackingEvents
            .Where(t => t.TrackingNumber == trackingNumber)
            .OrderBy(t => t.Timestamp)
            .ToListAsync(ct);

        if (events.Count == 0) return null;

        var lastEvent = events.Last();
        return new ExternalTrackingResponse
        {
            TrackingNumber = trackingNumber,
            CurrentLocation = lastEvent.Location,
            Status = lastEvent.Description,
            EstimatedDelivery = DateTime.UtcNow.AddDays(2),
            Events = events.Select(e => new ExternalTrackingEvent
            {
                Timestamp = e.Timestamp,
                Location = e.Location,
                Description = e.Description
            }).ToList()
        };
    }
}

/// <summary>
/// 模拟外部支付系统 API
/// </summary>
public class MockExternalPaymentApi : IExternalPaymentApi
{
    private readonly CustomerServiceDbContext _db;

    public MockExternalPaymentApi(CustomerServiceDbContext db)
    {
        _db = db;
    }

    public async Task<ExternalPaymentStatusResponse?> GetPaymentStatusAsync(string orderId, CancellationToken ct = default)
    {
        var order = await _db.Orders.FirstOrDefaultAsync(o => o.OrderId == orderId, ct);
        if (order == null) return null;

        var paymentStatus = order.Status switch
        {
            "pending" => "unpaid",
            "cancelled" => "refunded",
            _ => "paid"
        };

        return new ExternalPaymentStatusResponse
        {
            PaymentId = $"PAY-{order.OrderId}",
            OrderId = order.OrderId,
            Status = paymentStatus,
            Amount = order.TotalAmount,
            PaymentMethod = "支付宝",
            PaidAt = order.Status != "pending" ? order.CreatedAt.AddMinutes(5) : null
        };
    }

    public async Task<ExternalRefundResponse> InitiateRefundAsync(string orderId, decimal amount, string reason, CancellationToken ct = default)
    {
        var order = await _db.Orders.FirstOrDefaultAsync(o => o.OrderId == orderId, ct);
        if (order == null)
            return new ExternalRefundResponse { Success = false, Message = "订单不存在" };

        return new ExternalRefundResponse
        {
            Success = true,
            RefundId = $"RF-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}",
            Message = "退款已发起，请等待处理",
            RefundAmount = amount > 0 ? amount : order.TotalAmount,
            EstimatedDays = 3
        };
    }
}
