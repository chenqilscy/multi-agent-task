using CKY.MultiAgentFramework.Demos.CustomerService.Data;
using CKY.MultiAgentFramework.Demos.CustomerService.ExternalApis;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CKY.MultiAgentFramework.Demos.CustomerService.Services.Implementations;

/// <summary>
/// 持久化订单服务 - 本地数据库 + 外部 API 适配器
/// </summary>
public class PersistentOrderService : IOrderService
{
    private readonly CustomerServiceDbContext _db;
    private readonly IExternalOrderApi _externalApi;
    private readonly IExternalLogisticsApi _logisticsApi;
    private readonly ILogger<PersistentOrderService> _logger;

    public PersistentOrderService(
        CustomerServiceDbContext db,
        IExternalOrderApi externalApi,
        IExternalLogisticsApi logisticsApi,
        ILogger<PersistentOrderService> logger)
    {
        _db = db;
        _externalApi = externalApi;
        _logisticsApi = logisticsApi;
        _logger = logger;
    }

    public async Task<OrderInfo?> GetOrderAsync(string orderId, CancellationToken ct = default)
    {
        var order = await _db.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.OrderId == orderId, ct);

        if (order != null)
            return MapToOrderInfo(order);

        // 本地没有，尝试从外部系统查询
        _logger.LogInformation("本地数据库未找到订单 {OrderId}，尝试外部 API", orderId);
        var externalOrder = await _externalApi.QueryOrderAsync(orderId, ct);
        if (externalOrder == null) return null;

        return new OrderInfo
        {
            OrderId = externalOrder.OrderId,
            UserId = externalOrder.UserId,
            Status = externalOrder.Status,
            TotalAmount = externalOrder.TotalAmount,
            TrackingNumber = externalOrder.TrackingNumber,
            CreatedAt = externalOrder.CreatedAt,
            Items = externalOrder.Items.Select(i => new OrderItem
            {
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice
            }).ToList()
        };
    }

    public async Task<List<OrderInfo>> GetUserOrdersAsync(string userId, int pageSize = 10, CancellationToken ct = default)
    {
        var orders = await _db.Orders
            .Include(o => o.Items)
            .Include(o => o.Customer)
            .Where(o => o.Customer.CustomerId == userId)
            .OrderByDescending(o => o.CreatedAt)
            .Take(pageSize)
            .ToListAsync(ct);

        return orders.Select(MapToOrderInfo).ToList();
    }

    public async Task<bool> CancelOrderAsync(string orderId, string reason, CancellationToken ct = default)
    {
        // 通过外部 API 取消
        var result = await _externalApi.CancelOrderAsync(orderId, reason, ct);
        if (!result.Success)
        {
            _logger.LogWarning("取消订单失败: {OrderId}, {Message}", orderId, result.Message);
            return false;
        }

        // 同步本地状态
        var localOrder = await _db.Orders.FirstOrDefaultAsync(o => o.OrderId == orderId, ct);
        if (localOrder != null)
        {
            localOrder.Status = "cancelled";
            localOrder.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
        }

        return true;
    }

    public async Task<TrackingInfo?> GetShippingStatusAsync(string orderId, CancellationToken ct = default)
    {
        var order = await _db.Orders.FirstOrDefaultAsync(o => o.OrderId == orderId, ct);
        if (order?.TrackingNumber == null) return null;

        // 从外部物流 API 获取最新信息
        var tracking = await _logisticsApi.GetTrackingAsync(order.TrackingNumber, ct);
        if (tracking == null) return null;

        return new TrackingInfo
        {
            TrackingNumber = tracking.TrackingNumber,
            CurrentLocation = tracking.CurrentLocation,
            Status = tracking.Status,
            EstimatedDelivery = tracking.EstimatedDelivery,
            Events = tracking.Events.Select(e => new TrackingEvent
            {
                Timestamp = e.Timestamp,
                Location = e.Location,
                Description = e.Description
            }).ToList()
        };
    }

    public async Task<RefundResult> RequestRefundAsync(string orderId, RefundRequest request, CancellationToken ct = default)
    {
        var result = await _externalApi.RequestRefundAsync(orderId, request.Amount, request.Reason, ct);

        return new RefundResult
        {
            Success = result.Success,
            RefundId = result.RefundId,
            Message = result.Message,
            RefundAmount = result.RefundAmount,
            EstimatedDays = result.EstimatedDays
        };
    }

    public async Task<ExchangeResult> RequestExchangeAsync(
        string orderId, ExchangeRequest request, CancellationToken ct = default)
    {
        var order = await _db.Orders.FirstOrDefaultAsync(o => o.OrderId == orderId, ct);
        if (order == null)
            return new ExchangeResult { Success = false, Message = $"未找到订单 {orderId}" };

        if (order.Status != "delivered")
            return new ExchangeResult { Success = false, Message = $"订单状态为 {order.Status}，仅已签收订单可申请换货" };

        return new ExchangeResult
        {
            Success = true,
            ExchangeId = $"EXC-{Guid.NewGuid():N}"[..12],
            Message = "换货申请已提交，请将商品寄回指定地址"
        };
    }

    public async Task<ReturnEligibility> CheckReturnEligibilityAsync(
        string orderId, CancellationToken ct = default)
    {
        var order = await _db.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.OrderId == orderId, ct);

        if (order == null)
            return new ReturnEligibility { IsEligible = false, Reason = $"未找到订单 {orderId}" };

        var daysSinceOrder = (DateTime.Now - order.CreatedAt).Days;
        var isWithinPeriod = daysSinceOrder <= 7;
        var specialItems = new[] { "内衣", "食品", "定制商品" };
        var isSpecial = order.Items.Any(i =>
            specialItems.Any(s => i.ProductName.Contains(s, StringComparison.OrdinalIgnoreCase)));

        return new ReturnEligibility
        {
            IsEligible = isWithinPeriod && !isSpecial,
            RemainingDays = Math.Max(0, 7 - daysSinceOrder),
            IsSpecialItem = isSpecial,
            Reason = !isWithinPeriod ? "已超过7天退货期限"
                : isSpecial ? "该商品属于特殊品类，不支持退货"
                : "符合退货条件"
        };
    }

    private static OrderInfo MapToOrderInfo(Entities.OrderEntity order) => new()
    {
        OrderId = order.OrderId,
        UserId = order.Customer?.CustomerId ?? string.Empty,
        Status = order.Status,
        TotalAmount = order.TotalAmount,
        TrackingNumber = order.TrackingNumber,
        CreatedAt = order.CreatedAt,
        UpdatedAt = order.UpdatedAt,
        Items = order.Items.Select(i => new OrderItem
        {
            ProductId = i.ProductId,
            ProductName = i.ProductName,
            Quantity = i.Quantity,
            UnitPrice = i.UnitPrice
        }).ToList()
    };
}
