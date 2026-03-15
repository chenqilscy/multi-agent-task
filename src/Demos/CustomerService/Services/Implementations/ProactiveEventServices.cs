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
}
