using Microsoft.Extensions.Logging;

namespace CKY.MultiAgentFramework.Demos.CustomerService.Services.Implementations
{
    /// <summary>
    /// 故障注入接口 - 用于测试和演示故障场景
    /// </summary>
    public interface IFaultInjectable
    {
        /// <summary>注入故障</summary>
        void InjectFault(string faultType, string? message = null);

        /// <summary>清除所有故障</summary>
        void ClearFaults();
    }

    /// <summary>
    /// 故障注入基类
    /// </summary>
    public abstract class FaultInjectableServiceBase : IFaultInjectable
    {
        private readonly Dictionary<string, string> _faults = new(StringComparer.OrdinalIgnoreCase);

        public void InjectFault(string faultType, string? message = null)
        {
            _faults[faultType] = message ?? $"Injected fault: {faultType}";
        }

        public void ClearFaults()
        {
            _faults.Clear();
        }

        protected void ThrowIfFaultInjected(string faultType)
        {
            if (_faults.TryGetValue(faultType, out var message))
                throw new InvalidOperationException(message);
        }
    }

    /// <summary>
    /// 订单服务模拟实现
    /// 支持故障注入：timeout, service_unavailable
    /// </summary>
    public class SimulatedOrderService : FaultInjectableServiceBase, IOrderService
    {
        private readonly ILogger<SimulatedOrderService> _logger;

        // 模拟订单数据库
        private readonly Dictionary<string, OrderInfo> _orders = new()
        {
            ["ORD-2024-001"] = new OrderInfo
            {
                OrderId = "ORD-2024-001",
                UserId = "user-001",
                Status = "shipped",
                TotalAmount = 299.00m,
                CreatedAt = DateTime.Now.AddDays(-3),
                TrackingNumber = "SF1234567890",
                Items =
                [
                    new OrderItem { ProductId = "P001", ProductName = "无线蓝牙耳机", Quantity = 1, UnitPrice = 299.00m }
                ],
            },
            ["ORD-2024-002"] = new OrderInfo
            {
                OrderId = "ORD-2024-002",
                UserId = "user-001",
                Status = "delivered",
                TotalAmount = 599.00m,
                CreatedAt = DateTime.Now.AddDays(-10),
                TrackingNumber = "SF0987654321",
                Items =
                [
                    new OrderItem { ProductId = "P002", ProductName = "智能手环", Quantity = 1, UnitPrice = 599.00m }
                ],
            },
        };

        public SimulatedOrderService(ILogger<SimulatedOrderService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task<OrderInfo?> GetOrderAsync(string orderId, CancellationToken ct = default)
        {
            ThrowIfFaultInjected("timeout");
            ThrowIfFaultInjected("service_unavailable");
            _logger.LogInformation("Fetching order {OrderId}", orderId);
            _orders.TryGetValue(orderId, out var order);
            return Task.FromResult(order);
        }

        public Task<List<OrderInfo>> GetUserOrdersAsync(string userId, int pageSize = 10, CancellationToken ct = default)
        {
            var orders = _orders.Values
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.CreatedAt)
                .Take(pageSize)
                .ToList();
            return Task.FromResult(orders);
        }

        public Task<bool> CancelOrderAsync(string orderId, string reason, CancellationToken ct = default)
        {
            if (!_orders.TryGetValue(orderId, out var order))
                return Task.FromResult(false);

            if (order.Status is "delivered" or "cancelled")
                return Task.FromResult(false);

            order.Status = "cancelled";
            order.UpdatedAt = DateTime.Now;
            _logger.LogInformation("Order {OrderId} cancelled: {Reason}", orderId, reason);
            return Task.FromResult(true);
        }

        public Task<TrackingInfo?> GetShippingStatusAsync(string orderId, CancellationToken ct = default)
        {
            if (!_orders.TryGetValue(orderId, out var order) || order.TrackingNumber == null)
                return Task.FromResult<TrackingInfo?>(null);

            var info = new TrackingInfo
            {
                TrackingNumber = order.TrackingNumber,
                CurrentLocation = "北京分拨中心",
                Status = order.Status == "shipped" ? "运输中" : "已签收",
                EstimatedDelivery = order.Status == "shipped" ? DateTime.Today.AddDays(1) : null,
                Events =
                [
                    new TrackingEvent
                    {
                        Timestamp = order.CreatedAt.AddHours(2),
                        Location = "发货仓库",
                        Description = "商品已揽收"
                    },
                    new TrackingEvent
                    {
                        Timestamp = order.CreatedAt.AddHours(12),
                        Location = "上海转运中心",
                        Description = "已到达转运中心"
                    },
                    new TrackingEvent
                    {
                        Timestamp = order.CreatedAt.AddDays(1),
                        Location = "北京分拨中心",
                        Description = "已到达目的地分拨中心"
                    },
                ],
            };

            return Task.FromResult<TrackingInfo?>(info);
        }

        public Task<RefundResult> RequestRefundAsync(
            string orderId, RefundRequest request, CancellationToken ct = default)
        {
            if (!_orders.TryGetValue(orderId, out var order))
            {
                return Task.FromResult(new RefundResult
                {
                    Success = false,
                    Message = $"未找到订单 {orderId}"
                });
            }

            var refundAmount = request.Amount > 0 ? request.Amount : order.TotalAmount;
            var result = new RefundResult
            {
                Success = true,
                RefundId = $"REF-{Guid.NewGuid():N}"[..12],
                Message = $"退款申请已提交，预计3-5个工作日内退回到您的账户",
                RefundAmount = refundAmount,
                EstimatedDays = 5,
            };

            _logger.LogInformation("Refund requested for order {OrderId}: {Amount}", orderId, refundAmount);
            return Task.FromResult(result);
        }

        public Task<ExchangeResult> RequestExchangeAsync(
            string orderId, ExchangeRequest request, CancellationToken ct = default)
        {
            if (!_orders.TryGetValue(orderId, out var order))
            {
                return Task.FromResult(new ExchangeResult
                {
                    Success = false,
                    Message = $"未找到订单 {orderId}"
                });
            }

            if (order.Status != "delivered")
            {
                return Task.FromResult(new ExchangeResult
                {
                    Success = false,
                    Message = $"订单状态为 {order.Status}，仅已签收订单可申请换货"
                });
            }

            var result = new ExchangeResult
            {
                Success = true,
                ExchangeId = $"EXC-{Guid.NewGuid():N}"[..12],
                Message = "换货申请已提交，请将商品寄回指定地址，收到后1-3个工作日发出新商品"
            };

            _logger.LogInformation("Exchange requested for order {OrderId}", orderId);
            return Task.FromResult(result);
        }

        public Task<ReturnEligibility> CheckReturnEligibilityAsync(
            string orderId, CancellationToken ct = default)
        {
            if (!_orders.TryGetValue(orderId, out var order))
            {
                return Task.FromResult(new ReturnEligibility
                {
                    IsEligible = false,
                    Reason = $"未找到订单 {orderId}"
                });
            }

            // 模拟7天退货期限
            var daysSinceDelivery = (DateTime.Now - order.CreatedAt).Days;
            var isWithinPeriod = daysSinceDelivery <= 7;

            // 模拟特殊商品限制（内衣、食品等不可退）
            var specialItems = new[] { "内衣", "食品", "定制商品" };
            var isSpecial = order.Items.Any(i =>
                specialItems.Any(s => i.ProductName.Contains(s, StringComparison.OrdinalIgnoreCase)));

            var result = new ReturnEligibility
            {
                IsEligible = isWithinPeriod && !isSpecial,
                RemainingDays = Math.Max(0, 7 - daysSinceDelivery),
                IsSpecialItem = isSpecial,
                Reason = !isWithinPeriod ? "已超过7天退货期限"
                    : isSpecial ? "该商品属于特殊品类，不支持退货"
                    : "符合退货条件"
            };

            return Task.FromResult(result);
        }
    }

    /// <summary>
    /// 工单服务模拟实现
    /// 生产环境应替换为真实工单系统的适配器
    /// </summary>
    public class SimulatedTicketService : ITicketService
    {
        private readonly ILogger<SimulatedTicketService> _logger;
        private readonly Dictionary<string, TicketInfo> _tickets = new();

        public SimulatedTicketService(ILogger<SimulatedTicketService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task<string> CreateTicketAsync(TicketCreateRequest request, CancellationToken ct = default)
        {
            var ticketId = $"TKT-{DateTime.Now:yyyyMMdd}-{_tickets.Count + 1:D3}";
            var ticket = new TicketInfo
            {
                TicketId = ticketId,
                UserId = request.UserId,
                Title = request.Title,
                Description = request.Description,
                Category = request.Category,
                Priority = request.Priority,
                Status = "open",
                CreatedAt = DateTime.UtcNow,
            };
            _tickets[ticketId] = ticket;
            _logger.LogInformation("Created ticket {TicketId} for user {UserId}", ticketId, request.UserId);
            return Task.FromResult(ticketId);
        }

        public Task<TicketInfo?> GetTicketAsync(string ticketId, CancellationToken ct = default)
        {
            _tickets.TryGetValue(ticketId, out var ticket);
            return Task.FromResult(ticket);
        }

        public Task<bool> UpdateTicketAsync(
            string ticketId, TicketUpdateRequest update, CancellationToken ct = default)
        {
            if (!_tickets.TryGetValue(ticketId, out var ticket))
                return Task.FromResult(false);

            if (update.Status != null)
                ticket.Status = update.Status;

            if (!string.IsNullOrEmpty(update.Comment))
            {
                ticket.Comments.Add(new TicketComment
                {
                    Author = "客服系统",
                    Content = update.Comment,
                    IsStaff = true,
                    CreatedAt = DateTime.UtcNow,
                });
            }

            ticket.UpdatedAt = DateTime.UtcNow;
            return Task.FromResult(true);
        }

        public Task<List<TicketInfo>> GetUserTicketsAsync(string userId, CancellationToken ct = default)
        {
            var tickets = _tickets.Values
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.CreatedAt)
                .ToList();
            return Task.FromResult(tickets);
        }
    }

    /// <summary>
    /// 知识库服务模拟实现（基于静态 FAQ）
    /// 生产环境应集成 Qdrant 向量数据库实现 RAG
    /// </summary>
    public class SimulatedKnowledgeBaseService : IKnowledgeBaseService
    {
        private readonly List<FaqEntry> _faqs =
        [
            new FaqEntry
            {
                Id = "FAQ-001",
                Question = "如何查询我的订单？",
                Answer = "您可以通过以下方式查询订单：\n1. 告诉我您的订单号（如 ORD-2024-001）\n2. 说「查询我的最近订单」查看最新订单列表",
                Category = "order",
                Keywords = ["查询", "订单", "查单", "订单状态"],
            },
            new FaqEntry
            {
                Id = "FAQ-002",
                Question = "如何申请退款？",
                Answer = "申请退款步骤：\n1. 提供您的订单号\n2. 说明退款原因\n3. 我将为您提交退款申请\n\n注意：退款一般在3-5个工作日内到账，具体时间取决于您的支付方式。",
                Category = "refund",
                Keywords = ["退款", "退货", "退钱", "申请退款"],
            },
            new FaqEntry
            {
                Id = "FAQ-003",
                Question = "如何取消订单？",
                Answer = "取消订单条件：\n• 未发货的订单可以直接取消\n• 已发货的订单需要拒收后走退货退款流程\n\n请提供您的订单号，我来帮您处理。",
                Category = "order",
                Keywords = ["取消", "取消订单", "不要了", "撤单"],
            },
            new FaqEntry
            {
                Id = "FAQ-004",
                Question = "快递到哪里了？",
                Answer = "请提供您的订单号，我可以为您查询快递的实时物流信息，包括当前位置和预计送达时间。",
                Category = "shipping",
                Keywords = ["快递", "物流", "送到哪", "到哪了", "派送"],
            },
            new FaqEntry
            {
                Id = "FAQ-005",
                Question = "产品质量问题怎么处理？",
                Answer = "如果您收到的商品存在质量问题：\n1. 拍摄照片或视频记录问题\n2. 联系我们提交工单\n3. 我们会在24小时内给您答复\n\n质量问题在7天内可申请退款或换货。",
                Category = "product",
                Keywords = ["质量", "质量问题", "坏了", "损坏", "不好用", "有问题"],
            },
        ];

        public Task<KnowledgeSearchResult> SearchAsync(
            string query, int topK = 5, CancellationToken ct = default)
        {
            // 简单的关键字匹配（生产环境替换为向量检索）
            var matches = _faqs
                .Select(faq => new
                {
                    Faq = faq,
                    Score = CalculateRelevance(query, faq),
                })
                .Where(x => x.Score > 0)
                .OrderByDescending(x => x.Score)
                .Take(topK)
                .ToList();

            var result = new KnowledgeSearchResult
            {
                RelevantFaqs = matches.Select(m =>
                {
                    m.Faq.Relevance = m.Score;
                    return m.Faq;
                }).ToList(),
                Confidence = matches.Any() ? matches.Max(m => m.Score) : 0,
            };

            if (result.RelevantFaqs.Count > 0 && result.Confidence > 0.7)
                result.GeneratedAnswer = result.RelevantFaqs.First().Answer;

            return Task.FromResult(result);
        }

        public async Task<bool> HasDefinitiveAnswerAsync(string query, CancellationToken ct = default)
        {
            var result = await SearchAsync(query, 1, ct);
            return result.Confidence > 0.7;
        }

        public Task UpsertFaqAsync(FaqEntry entry, CancellationToken ct = default)
        {
            var existing = _faqs.FirstOrDefault(f => f.Id == entry.Id);
            if (existing != null)
                _faqs.Remove(existing);
            _faqs.Add(entry);
            return Task.CompletedTask;
        }

        private static double CalculateRelevance(string query, FaqEntry faq)
        {
            var queryLower = query.ToLower();
            double score = 0;

            // 关键词匹配（关键词已小写存储，避免循环内重复分配）
            foreach (var keyword in faq.Keywords)
            {
                if (queryLower.Contains(keyword))
                    score += 0.3;
            }

            // 问题相似度（简单包含匹配）
            if (queryLower.Contains(faq.Question.ToLower()))
                score += 0.5;

            return Math.Min(score, 1.0);
        }
    }

    /// <summary>
    /// 用户行为服务模拟实现
    /// 生产环境应持久化到数据库
    /// </summary>
    public class SimulatedUserBehaviorService : IUserBehaviorService
    {
        private readonly List<UserBehaviorRecord> _records = new();
        private readonly Dictionary<string, UserProfile> _profiles = new();

        public Task RecordAsync(UserBehaviorRecord record, CancellationToken ct = default)
        {
            _records.Add(record);
            UpdateProfile(record);
            return Task.CompletedTask;
        }

        public Task<UserProfile?> GetUserProfileAsync(string userId, CancellationToken ct = default)
        {
            _profiles.TryGetValue(userId, out var profile);
            return Task.FromResult(profile);
        }

        public async Task<Dictionary<string, string>> GetDefaultEntitiesAsync(
            string userId, CancellationToken ct = default)
        {
            var profile = await GetUserProfileAsync(userId, ct);
            return profile?.DefaultEntities ?? new Dictionary<string, string>();
        }

        private void UpdateProfile(UserBehaviorRecord record)
        {
            if (!_profiles.TryGetValue(record.UserId, out var profile))
            {
                profile = new UserProfile { UserId = record.UserId };
                _profiles[record.UserId] = profile;
            }

            // 更新意图频率
            profile.IntentFrequency.TryGetValue(record.Intent, out var count);
            profile.IntentFrequency[record.Intent] = count + 1;

            // 更新默认实体
            foreach (var entity in record.Entities)
                profile.DefaultEntities[entity.Key] = entity.Value;

            profile.LastActiveTime = record.Timestamp;
            profile.TotalInteractions++;
        }
    }

    /// <summary>
    /// 问题升级服务模拟实现
    /// 生产环境应对接客服工作台系统
    /// </summary>
    public class SimulatedEscalationService : IEscalationService
    {
        public Task<EscalationResult> EscalateToHumanAsync(
            string userId, string reason, string priority = "normal", CancellationToken ct = default)
        {
            var waitMinutes = priority switch
            {
                "urgent" => 1,
                "high" => 3,
                _ => 10,
            };

            return Task.FromResult(new EscalationResult
            {
                Success = true,
                AgentId = $"AGENT-{Random.Shared.Next(100, 999)}",
                EstimatedWaitMinutes = waitMinutes,
                Message = $"已为您转接人工客服，预计等待{waitMinutes}分钟，请稍候。"
            });
        }

        public Task<VipLevel> GetVipLevelAsync(string userId, CancellationToken ct = default)
        {
            // 模拟: user-001 是 Gold VIP
            var level = userId == "user-001" ? VipLevel.Gold : VipLevel.Normal;
            return Task.FromResult(level);
        }

        public Task<TransferResult> TransferToDepartmentAsync(
            string ticketId, string department, CancellationToken ct = default)
        {
            return Task.FromResult(new TransferResult
            {
                Success = true,
                Department = department,
                Message = $"工单 {ticketId} 已转接至{department}部门，专人将在1个工作日内联系您。"
            });
        }
    }
}
