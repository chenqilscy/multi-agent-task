using CKY.MultiAgentFramework.Demos.CustomerService.Services;
using Microsoft.Extensions.Logging;

namespace CKY.MultiAgentFramework.Demos.CustomerService.Services.Implementations
{
    /// <summary>
    /// 主动服务事件总线模拟实现
    /// 生产环境应替换为 RabbitMQ/Redis Pub-Sub 等消息中间件
    /// </summary>
    public class InMemoryProactiveEventBus : IProactiveEventBus
    {
        private readonly ILogger<InMemoryProactiveEventBus> _logger;
        private readonly Dictionary<ProactiveEventType, IProactiveEventHandler> _handlers = new();
        private readonly List<ProactiveEvent> _events = new();

        public InMemoryProactiveEventBus(ILogger<InMemoryProactiveEventBus> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task PublishAsync(ProactiveEvent proactiveEvent, CancellationToken ct = default)
        {
            _logger.LogInformation("Publishing proactive event: {EventType} for user {UserId}",
                proactiveEvent.EventType, proactiveEvent.UserId);

            _events.Add(proactiveEvent);

            if (_handlers.TryGetValue(proactiveEvent.EventType, out var handler))
            {
                try
                {
                    var message = await handler.HandleEventAsync(proactiveEvent, ct);
                    proactiveEvent.IsHandled = true;
                    _logger.LogInformation("Event {EventId} handled: {Message}",
                        proactiveEvent.EventId, message);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to handle event {EventId}", proactiveEvent.EventId);
                }
            }
        }

        public void RegisterHandler(IProactiveEventHandler handler)
        {
            _handlers[handler.EventType] = handler;
            _logger.LogInformation("Registered handler for event type: {EventType}", handler.EventType);
        }

        public Task<List<ProactiveEvent>> GetPendingEventsAsync(
            string userId, CancellationToken ct = default)
        {
            var pending = _events
                .Where(e => e.UserId == userId && !e.IsHandled)
                .OrderByDescending(e => e.CreatedAt)
                .ToList();
            return Task.FromResult(pending);
        }
    }

    /// <summary>
    /// 发货延迟事件处理器
    /// </summary>
    public class ShippingDelayEventHandler : IProactiveEventHandler
    {
        public ProactiveEventType EventType => ProactiveEventType.ShippingDelay;

        public Task<string> HandleEventAsync(ProactiveEvent proactiveEvent, CancellationToken ct = default)
        {
            var orderId = proactiveEvent.Data.GetValueOrDefault("orderId", "未知");
            var delayDays = proactiveEvent.Data.GetValueOrDefault("delayDays", 1);
            var message = $"📦 您的订单 {orderId} 因物流原因预计延迟 {delayDays} 天送达，我们深表歉意。" +
                          $"如需帮助，请随时联系客服。";
            return Task.FromResult(message);
        }
    }

    /// <summary>
    /// 会员权益到期提醒事件处理器
    /// </summary>
    public class MembershipExpiringEventHandler : IProactiveEventHandler
    {
        public ProactiveEventType EventType => ProactiveEventType.MembershipExpiring;

        public Task<string> HandleEventAsync(ProactiveEvent proactiveEvent, CancellationToken ct = default)
        {
            var expiryDate = proactiveEvent.Data.GetValueOrDefault("expiryDate", "");
            var message = $"🎫 您的会员权益将于 {expiryDate} 到期，续费可享 8 折优惠。" +
                          $"是否需要我为您办理续费？";
            return Task.FromResult(message);
        }
    }

    /// <summary>
    /// 生日祝福事件处理器
    /// </summary>
    public class BirthdayGreetingEventHandler : IProactiveEventHandler
    {
        public ProactiveEventType EventType => ProactiveEventType.BirthdayGreeting;

        public Task<string> HandleEventAsync(ProactiveEvent proactiveEvent, CancellationToken ct = default)
        {
            var couponCode = proactiveEvent.Data.GetValueOrDefault("couponCode", "BDAY2026");
            var message = $"🎂 生日快乐！为您准备了专属优惠券 {couponCode}，全场满100减20，有效期7天。";
            return Task.FromResult(message);
        }
    }

    /// <summary>
    /// 促销活动推荐事件处理器
    /// </summary>
    public class PromotionRecommendationEventHandler : IProactiveEventHandler
    {
        public ProactiveEventType EventType => ProactiveEventType.PromotionRecommendation;

        public Task<string> HandleEventAsync(ProactiveEvent proactiveEvent, CancellationToken ct = default)
        {
            var promotionName = proactiveEvent.Data.GetValueOrDefault("promotionName", "限时特惠");
            var discount = proactiveEvent.Data.GetValueOrDefault("discount", "8折");
            var message = $"🎉 {promotionName}活动进行中！您关注的商品正在{discount}优惠，快来看看吧！";
            return Task.FromResult(message);
        }
    }

    /// <summary>
    /// 异常交易主动核实事件处理器
    /// </summary>
    public class AnomalousTransactionEventHandler : IProactiveEventHandler
    {
        public ProactiveEventType EventType => ProactiveEventType.AnomalousTransaction;

        public Task<string> HandleEventAsync(ProactiveEvent proactiveEvent, CancellationToken ct = default)
        {
            var orderId = proactiveEvent.Data.GetValueOrDefault("orderId", "未知");
            var amount = proactiveEvent.Data.GetValueOrDefault("amount", "0");
            var message = $"⚠️ 安全提醒：检测到一笔异常交易（订单 {orderId}，金额 {amount} 元）。" +
                          $"如非本人操作，请立即回复「冻结账户」或联系人工客服。";
            return Task.FromResult(message);
        }
    }

    /// <summary>
    /// 订单状态变更通知事件处理器
    /// </summary>
    public class OrderStatusChangeEventHandler : IProactiveEventHandler
    {
        public ProactiveEventType EventType => ProactiveEventType.OrderStatusChange;

        public Task<string> HandleEventAsync(ProactiveEvent proactiveEvent, CancellationToken ct = default)
        {
            var orderId = proactiveEvent.Data.GetValueOrDefault("orderId", "未知");
            var newStatus = proactiveEvent.Data.GetValueOrDefault("newStatus", "未知");
            var trackingNumber = proactiveEvent.Data.GetValueOrDefault("trackingNumber", "");
            var tracking = string.IsNullOrEmpty(trackingNumber?.ToString()) ? "" : $"快递单号：{trackingNumber}。";
            var message = $"📋 您的订单 {orderId} 状态已更新为「{newStatus}」。{tracking}如有疑问请随时联系客服。";
            return Task.FromResult(message);
        }
    }

    /// <summary>
    /// 服务满意度调查事件处理器
    /// </summary>
    public class SatisfactionSurveyEventHandler : IProactiveEventHandler
    {
        public ProactiveEventType EventType => ProactiveEventType.SatisfactionSurvey;

        public Task<string> HandleEventAsync(ProactiveEvent proactiveEvent, CancellationToken ct = default)
        {
            var ticketId = proactiveEvent.Data.GetValueOrDefault("ticketId", "未知");
            var message = $"📝 您的工单 {ticketId} 已处理完毕，请对本次服务进行满意度评价。" +
                          $"您的反馈是我们改进服务的动力！";
            return Task.FromResult(message);
        }
    }
}
