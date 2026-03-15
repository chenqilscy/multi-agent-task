namespace CKY.MultiAgentFramework.Demos.CustomerService.Services
{
    // ============================
    // 订单相关模型
    // ============================

    /// <summary>订单信息</summary>
    public class OrderInfo
    {
        public string OrderId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty; // pending/paid/shipped/delivered/cancelled
        public decimal TotalAmount { get; set; }
        public List<OrderItem> Items { get; set; } = new();
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? TrackingNumber { get; set; }
    }

    /// <summary>订单商品</summary>
    public class OrderItem
    {
        public string ProductId { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }

    /// <summary>物流追踪信息</summary>
    public class TrackingInfo
    {
        public string TrackingNumber { get; set; } = string.Empty;
        public string CurrentLocation { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public List<TrackingEvent> Events { get; set; } = new();
        public DateTime? EstimatedDelivery { get; set; }
    }

    /// <summary>物流事件</summary>
    public class TrackingEvent
    {
        public DateTime Timestamp { get; set; }
        public string Location { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    /// <summary>退款请求</summary>
    public class RefundRequest
    {
        public string Reason { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string? Description { get; set; }
    }

    /// <summary>退款结果</summary>
    public class RefundResult
    {
        public bool Success { get; set; }
        public string? RefundId { get; set; }
        public string Message { get; set; } = string.Empty;
        public decimal RefundAmount { get; set; }
        public int EstimatedDays { get; set; } // 预计到账天数
    }

    /// <summary>
    /// 订单服务接口
    /// 对接三方订单系统（适配器模式）
    /// </summary>
    public interface IOrderService
    {
        /// <summary>根据订单ID查询订单</summary>
        Task<OrderInfo?> GetOrderAsync(string orderId, CancellationToken ct = default);

        /// <summary>查询用户的订单列表</summary>
        Task<List<OrderInfo>> GetUserOrdersAsync(string userId, int pageSize = 10, CancellationToken ct = default);

        /// <summary>取消订单</summary>
        Task<bool> CancelOrderAsync(string orderId, string reason, CancellationToken ct = default);

        /// <summary>查询物流信息</summary>
        Task<TrackingInfo?> GetShippingStatusAsync(string orderId, CancellationToken ct = default);

        /// <summary>申请退款</summary>
        Task<RefundResult> RequestRefundAsync(string orderId, RefundRequest request, CancellationToken ct = default);
    }

    // ============================
    // 工单相关模型
    // ============================

    /// <summary>工单信息</summary>
    public class TicketInfo
    {
        public string TicketId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty; // order/product/payment/shipping/other
        public string Priority { get; set; } = "normal"; // low/normal/high/urgent
        public string Status { get; set; } = "open"; // open/in_progress/resolved/closed
        public List<TicketComment> Comments { get; set; } = new();
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? ResolvedAt { get; set; }
    }

    /// <summary>工单评论</summary>
    public class TicketComment
    {
        public string Author { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public bool IsStaff { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>创建工单请求</summary>
    public class TicketCreateRequest
    {
        public string UserId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = "other";
        public string Priority { get; set; } = "normal";
        public string? RelatedOrderId { get; set; }
    }

    /// <summary>更新工单请求</summary>
    public class TicketUpdateRequest
    {
        public string? Status { get; set; }
        public string? Comment { get; set; }
    }

    /// <summary>
    /// 工单服务接口
    /// 对接三方工单系统（适配器模式）
    /// </summary>
    public interface ITicketService
    {
        /// <summary>创建工单，返回工单ID</summary>
        Task<string> CreateTicketAsync(TicketCreateRequest request, CancellationToken ct = default);

        /// <summary>查询工单详情</summary>
        Task<TicketInfo?> GetTicketAsync(string ticketId, CancellationToken ct = default);

        /// <summary>更新工单</summary>
        Task<bool> UpdateTicketAsync(string ticketId, TicketUpdateRequest update, CancellationToken ct = default);

        /// <summary>查询用户的工单列表</summary>
        Task<List<TicketInfo>> GetUserTicketsAsync(string userId, CancellationToken ct = default);
    }

    // ============================
    // 知识库相关模型
    // ============================

    /// <summary>FAQ 条目</summary>
    public class FaqEntry
    {
        public string Id { get; set; } = string.Empty;
        public string Question { get; set; } = string.Empty;
        public string Answer { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public List<string> Keywords { get; set; } = new();
        public double Relevance { get; set; }
    }

    /// <summary>知识库检索结果</summary>
    public class KnowledgeSearchResult
    {
        public List<FaqEntry> RelevantFaqs { get; set; } = new();
        public string? GeneratedAnswer { get; set; }
        public List<string> SourceReferences { get; set; } = new();
        public double Confidence { get; set; }
    }

    /// <summary>
    /// 知识库服务接口
    /// 支持 FAQ 精确匹配和 RAG 语义检索
    /// </summary>
    public interface IKnowledgeBaseService
    {
        /// <summary>
        /// 检索相关知识（精确匹配 + 向量语义检索）
        /// </summary>
        Task<KnowledgeSearchResult> SearchAsync(string query, int topK = 5, CancellationToken ct = default);

        /// <summary>
        /// 判断问题是否有确定答案（置信度 > 阈值）
        /// </summary>
        Task<bool> HasDefinitiveAnswerAsync(string query, CancellationToken ct = default);

        /// <summary>
        /// 添加/更新 FAQ 条目（管理功能）
        /// </summary>
        Task UpsertFaqAsync(FaqEntry entry, CancellationToken ct = default);
    }

    // ============================
    // 用户行为相关模型
    // ============================

    /// <summary>用户行为记录</summary>
    public class UserBehaviorRecord
    {
        public string UserId { get; set; } = string.Empty;
        public string SessionId { get; set; } = string.Empty;
        public string Intent { get; set; } = string.Empty;
        public bool TaskSucceeded { get; set; }
        public int ClarificationRoundsNeeded { get; set; }
        public TimeSpan ResponseTime { get; set; }
        public Dictionary<string, string> Entities { get; set; } = new();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    /// <summary>用户画像</summary>
    public class UserProfile
    {
        public string UserId { get; set; } = string.Empty;
        public Dictionary<string, int> IntentFrequency { get; set; } = new();
        public Dictionary<string, string> DefaultEntities { get; set; } = new();
        public List<string> FrequentCategories { get; set; } = new();
        public string PreferredLanguage { get; set; } = "zh-CN";
        public DateTime LastActiveTime { get; set; }
        public int TotalInteractions { get; set; }
    }

    /// <summary>
    /// 用户行为服务接口
    /// 记录并分析用户行为，构建用户画像
    /// </summary>
    public interface IUserBehaviorService
    {
        /// <summary>记录用户行为</summary>
        Task RecordAsync(UserBehaviorRecord record, CancellationToken ct = default);

        /// <summary>获取用户画像</summary>
        Task<UserProfile?> GetUserProfileAsync(string userId, CancellationToken ct = default);

        /// <summary>获取用户常用的默认实体值（用于自动填充）</summary>
        Task<Dictionary<string, string>> GetDefaultEntitiesAsync(string userId, CancellationToken ct = default);
    }

    // ============================
    // 主动服务事件驱动模型和接口
    // ============================

    /// <summary>主动服务事件类型</summary>
    public enum ProactiveEventType
    {
        /// <summary>发货延迟通知</summary>
        ShippingDelay,
        /// <summary>促销活动推荐</summary>
        PromotionRecommendation,
        /// <summary>会员权益到期提醒</summary>
        MembershipExpiring,
        /// <summary>生日祝福和优惠券</summary>
        BirthdayGreeting,
        /// <summary>工单处理完成通知</summary>
        TicketResolved,
    }

    /// <summary>主动服务事件</summary>
    public class ProactiveEvent
    {
        public string EventId { get; set; } = Guid.NewGuid().ToString("N")[..8];
        public ProactiveEventType EventType { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public Dictionary<string, object> Data { get; set; } = new();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsHandled { get; set; }
    }

    /// <summary>
    /// 主动服务事件处理器接口
    /// </summary>
    public interface IProactiveEventHandler
    {
        /// <summary>该处理器支持的事件类型</summary>
        ProactiveEventType EventType { get; }

        /// <summary>处理事件并生成通知消息</summary>
        Task<string> HandleEventAsync(ProactiveEvent proactiveEvent, CancellationToken ct = default);
    }

    /// <summary>
    /// 主动服务事件总线接口
    /// 负责事件的发布和订阅
    /// </summary>
    public interface IProactiveEventBus
    {
        /// <summary>发布事件</summary>
        Task PublishAsync(ProactiveEvent proactiveEvent, CancellationToken ct = default);

        /// <summary>注册事件处理器</summary>
        void RegisterHandler(IProactiveEventHandler handler);

        /// <summary>获取用户的未处理事件</summary>
        Task<List<ProactiveEvent>> GetPendingEventsAsync(string userId, CancellationToken ct = default);
    }
}
