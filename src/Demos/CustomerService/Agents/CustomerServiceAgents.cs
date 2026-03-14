using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Core.Agents;
using CKY.MultiAgentFramework.Core.Models.Task;
using CKY.MultiAgentFramework.Demos.CustomerService.Services;
using Microsoft.Extensions.Logging;

namespace CKY.MultiAgentFramework.Demos.CustomerService.Agents
{
    /// <summary>
    /// 知识库查询 Agent
    /// 处理 FAQ 查询和知识库检索（RAG）
    /// </summary>
    public class KnowledgeBaseAgent : MafBusinessAgentBase
    {
        private readonly IKnowledgeBaseService _knowledgeBaseService;

        public override string AgentId => "cs:knowledge-base-agent:001";
        public override string Name => "KnowledgeBaseAgent";
        public override string Description => "知识库查询 Agent，支持 FAQ 检索和 RAG 语义问答";
        public override IReadOnlyList<string> Capabilities =>
            ["faq", "knowledge-query", "product-info", "policy-query"];

        public KnowledgeBaseAgent(
            IKnowledgeBaseService knowledgeBaseService,
            IMafAiAgentRegistry llmRegistry,
            ILogger<KnowledgeBaseAgent> logger)
            : base(llmRegistry, logger)
        {
            _knowledgeBaseService = knowledgeBaseService
                ?? throw new ArgumentNullException(nameof(knowledgeBaseService));
        }

        public override async Task<MafTaskResponse> ExecuteBusinessLogicAsync(
            MafTaskRequest request,
            CancellationToken ct = default)
        {
            Logger.LogInformation("KnowledgeBaseAgent processing: {UserInput}", request.UserInput);

            var result = await _knowledgeBaseService.SearchAsync(request.UserInput, topK: 3, ct);

            if (result.GeneratedAnswer != null && result.Confidence > 0.6)
            {
                return new MafTaskResponse
                {
                    TaskId = request.TaskId,
                    Success = true,
                    Result = result.GeneratedAnswer,
                    Data = new { Sources = result.SourceReferences, Confidence = result.Confidence },
                };
            }

            if (result.RelevantFaqs.Count > 0)
            {
                var bestFaq = result.RelevantFaqs.First();
                return new MafTaskResponse
                {
                    TaskId = request.TaskId,
                    Success = true,
                    Result = $"根据您的问题，为您提供以下解答：\n\n{bestFaq.Answer}",
                    Data = new { FaqId = bestFaq.Id, Confidence = result.Confidence },
                };
            }

            // 知识库无法回答，转交人工/工单
            return new MafTaskResponse
            {
                TaskId = request.TaskId,
                Success = false,
                Result = "抱歉，我暂时无法解答您的问题。建议您提交工单，由专属客服为您处理。",
                Data = new { ShouldEscalate = true },
            };
        }
    }

    /// <summary>
    /// 订单查询与处理 Agent
    /// 处理订单查询、取消、退款等操作
    /// </summary>
    public class OrderAgent : MafBusinessAgentBase
    {
        private readonly IOrderService _orderService;

        public override string AgentId => "cs:order-agent:001";
        public override string Name => "OrderAgent";
        public override string Description => "订单处理 Agent，支持订单查询、取消、物流查询和退款申请";
        public override IReadOnlyList<string> Capabilities =>
            ["order-query", "order-cancel", "shipping-track", "refund-request"];

        public OrderAgent(
            IOrderService orderService,
            IMafAiAgentRegistry llmRegistry,
            ILogger<OrderAgent> logger)
            : base(llmRegistry, logger)
        {
            _orderService = orderService ?? throw new ArgumentNullException(nameof(orderService));
        }

        public override async Task<MafTaskResponse> ExecuteBusinessLogicAsync(
            MafTaskRequest request,
            CancellationToken ct = default)
        {
            Logger.LogInformation("OrderAgent processing: {UserInput}", request.UserInput);

            var userInput = request.UserInput;

            // 提取订单ID
            string? orderId = ExtractOrderId(userInput, request.Parameters);

            // 订单查询
            if (userInput.Contains("查询") || userInput.Contains("查看") || userInput.Contains("查一下"))
            {
                return await HandleOrderQueryAsync(orderId, request, ct);
            }

            // 物流查询
            if (userInput.Contains("快递") || userInput.Contains("物流") || userInput.Contains("到哪"))
            {
                return await HandleShippingQueryAsync(orderId, request, ct);
            }

            // 取消订单
            if (userInput.Contains("取消") || userInput.Contains("不要了"))
            {
                return await HandleCancelOrderAsync(orderId, request, ct);
            }

            // 退款申请
            if (userInput.Contains("退款") || userInput.Contains("退货") || userInput.Contains("退钱"))
            {
                return await HandleRefundAsync(orderId, request, ct);
            }

            // 兜底：查询所有订单
            return await HandleOrderQueryAsync(null, request, ct);
        }

        private async Task<MafTaskResponse> HandleOrderQueryAsync(
            string? orderId, MafTaskRequest request, CancellationToken ct)
        {
            if (!string.IsNullOrEmpty(orderId))
            {
                var order = await _orderService.GetOrderAsync(orderId, ct);
                if (order == null)
                {
                    return new MafTaskResponse
                    {
                        TaskId = request.TaskId,
                        Success = false,
                        Result = $"未找到订单 {orderId}，请检查订单号是否正确。",
                        NeedsClarification = true,
                        ClarificationQuestion = "请提供您的订单号（格式如 ORD-2024-001）",
                    };
                }

                var statusLabel = TranslateStatus(order.Status);
                return new MafTaskResponse
                {
                    TaskId = request.TaskId,
                    Success = true,
                    Result = $"📦 订单 {order.OrderId}\n" +
                             $"• 状态：{statusLabel}\n" +
                             $"• 商品：{string.Join("、", order.Items.Select(i => i.ProductName))}\n" +
                             $"• 金额：¥{order.TotalAmount:F2}\n" +
                             $"• 下单时间：{order.CreatedAt:yyyy-MM-dd HH:mm}",
                    Data = order,
                };
            }

            // 没有订单ID，返回澄清
            return new MafTaskResponse
            {
                TaskId = request.TaskId,
                Success = false,
                NeedsClarification = true,
                ClarificationQuestion = "请提供您的订单号（格式如 ORD-2024-001），我来帮您查询。",
                Result = "请提供订单号以便为您查询。",
            };
        }

        private async Task<MafTaskResponse> HandleShippingQueryAsync(
            string? orderId, MafTaskRequest request, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(orderId))
            {
                return new MafTaskResponse
                {
                    TaskId = request.TaskId,
                    Success = false,
                    NeedsClarification = true,
                    ClarificationQuestion = "请提供您的订单号以查询物流信息。",
                    Result = "请提供订单号。",
                };
            }

            var tracking = await _orderService.GetShippingStatusAsync(orderId, ct);
            if (tracking == null)
            {
                return new MafTaskResponse
                {
                    TaskId = request.TaskId,
                    Success = false,
                    Result = $"订单 {orderId} 暂无物流信息，可能尚未发货。",
                };
            }

            var lastEvent = tracking.Events.LastOrDefault();
            return new MafTaskResponse
            {
                TaskId = request.TaskId,
                Success = true,
                Result = $"🚚 快递单号：{tracking.TrackingNumber}\n" +
                         $"• 状态：{tracking.Status}\n" +
                         $"• 当前位置：{tracking.CurrentLocation}\n" +
                         $"• 最新动态：{lastEvent?.Description ?? "暂无"}\n" +
                         (tracking.EstimatedDelivery.HasValue
                             ? $"• 预计送达：{tracking.EstimatedDelivery.Value:MM-dd HH:mm}"
                             : ""),
                Data = tracking,
            };
        }

        private async Task<MafTaskResponse> HandleCancelOrderAsync(
            string? orderId, MafTaskRequest request, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(orderId))
            {
                return new MafTaskResponse
                {
                    TaskId = request.TaskId,
                    Success = false,
                    NeedsClarification = true,
                    ClarificationQuestion = "请提供您要取消的订单号。",
                    Result = "请提供订单号。",
                };
            }

            var success = await _orderService.CancelOrderAsync(orderId, "用户主动取消", ct);
            return new MafTaskResponse
            {
                TaskId = request.TaskId,
                Success = success,
                Result = success
                    ? $"✅ 订单 {orderId} 已成功取消。如已付款，退款将在3-5个工作日内到账。"
                    : $"❌ 订单 {orderId} 无法取消（可能已发货或已完成），如需帮助请提交工单。",
            };
        }

        private async Task<MafTaskResponse> HandleRefundAsync(
            string? orderId, MafTaskRequest request, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(orderId))
            {
                return new MafTaskResponse
                {
                    TaskId = request.TaskId,
                    Success = false,
                    NeedsClarification = true,
                    ClarificationQuestion = "请提供您要申请退款的订单号。",
                    Result = "请提供订单号。",
                };
            }

            var refundResult = await _orderService.RequestRefundAsync(orderId,
                new RefundRequest { Reason = "用户申请退款" }, ct);

            return new MafTaskResponse
            {
                TaskId = request.TaskId,
                Success = refundResult.Success,
                Result = refundResult.Success
                    ? $"✅ 退款申请已提交\n• 退款单号：{refundResult.RefundId}\n• 退款金额：¥{refundResult.RefundAmount:F2}\n• {refundResult.Message}"
                    : $"❌ 退款申请失败：{refundResult.Message}",
                Data = refundResult,
            };
        }

        private static string? ExtractOrderId(string input, Dictionary<string, object> parameters)
        {
            if (parameters.TryGetValue("orderId", out var p) && p != null)
                return p.ToString();

            // 使用正则提取 ORD-YYYY-NNN 格式的订单号（更健壮，支持无分隔符场景）
            var match = System.Text.RegularExpressions.Regex.Match(
                input, @"ORD-\d{4}-\d{3,}", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            return match.Success ? match.Value.ToUpper() : null;
        }

        private static string TranslateStatus(string status) => status switch
        {
            "pending" => "待付款",
            "paid" => "已付款，待发货",
            "shipped" => "已发货，运输中",
            "delivered" => "已签收",
            "cancelled" => "已取消",
            _ => status,
        };
    }

    /// <summary>
    /// 工单处理 Agent
    /// 处理工单的创建、查询和跟进
    /// </summary>
    public class TicketAgent : MafBusinessAgentBase
    {
        private readonly ITicketService _ticketService;

        public override string AgentId => "cs:ticket-agent:001";
        public override string Name => "TicketAgent";
        public override string Description => "工单处理 Agent，支持工单创建、查询和状态跟进";
        public override IReadOnlyList<string> Capabilities =>
            ["ticket-create", "ticket-query", "ticket-update", "escalation"];

        public TicketAgent(
            ITicketService ticketService,
            IMafAiAgentRegistry llmRegistry,
            ILogger<TicketAgent> logger)
            : base(llmRegistry, logger)
        {
            _ticketService = ticketService ?? throw new ArgumentNullException(nameof(ticketService));
        }

        public override async Task<MafTaskResponse> ExecuteBusinessLogicAsync(
            MafTaskRequest request,
            CancellationToken ct = default)
        {
            var userInput = request.UserInput;

            // 工单查询
            if (userInput.Contains("查询工单") || userInput.Contains("我的工单") || userInput.Contains("处理进度"))
            {
                var userId = request.Parameters.TryGetValue("userId", out var uid)
                    ? uid?.ToString() ?? "anonymous"
                    : "anonymous";

                var tickets = await _ticketService.GetUserTicketsAsync(userId, ct);
                if (tickets.Count == 0)
                {
                    return new MafTaskResponse
                    {
                        TaskId = request.TaskId,
                        Success = true,
                        Result = "您目前没有待处理的工单。",
                    };
                }

                var lines = new List<string> { "您的工单列表：" };
                foreach (var t in tickets.Take(5))
                    lines.Add($"• [{t.Status}] {t.TicketId}：{t.Title}（{t.CreatedAt:MM-dd}）");

                return new MafTaskResponse
                {
                    TaskId = request.TaskId,
                    Success = true,
                    Result = string.Join("\n", lines),
                    Data = tickets,
                };
            }

            // 创建工单
            var title = ExtractTitle(userInput);
            var category = DetectCategory(userInput);
            var userId2 = request.Parameters.TryGetValue("userId", out var uid2)
                ? uid2?.ToString() ?? "anonymous"
                : "anonymous";

            var ticketId = await _ticketService.CreateTicketAsync(new TicketCreateRequest
            {
                UserId = userId2,
                Title = title,
                Description = userInput,
                Category = category,
                Priority = "normal",
            }, ct);

            return new MafTaskResponse
            {
                TaskId = request.TaskId,
                Success = true,
                Result = $"✅ 工单已创建成功\n• 工单编号：{ticketId}\n• 类别：{category}\n• 我们会在24小时内处理您的工单，请耐心等待。",
                Data = new { TicketId = ticketId },
            };
        }

        private static string ExtractTitle(string input)
        {
            return input.Length > 50 ? input[..50] + "..." : input;
        }

        private static string DetectCategory(string input) =>
            input.Contains("订单") || input.Contains("付款") ? "order"
            : input.Contains("快递") || input.Contains("物流") ? "shipping"
            : input.Contains("退款") || input.Contains("退货") ? "refund"
            : input.Contains("产品") || input.Contains("质量") ? "product"
            : "other";
    }
}
