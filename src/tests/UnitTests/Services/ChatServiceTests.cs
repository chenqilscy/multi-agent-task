using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Core.Models.Task;
using CKY.MultiAgentFramework.Core.Resilience;
using CKY.MultiAgentFramework.Demos.CustomerService.Agents;
using CKY.MultiAgentFramework.Demos.CustomerService.Models;
using CKY.MultiAgentFramework.Demos.CustomerService.Services;
using CKY.MultiAgentFramework.Demos.CustomerService.Services.Implementations;
using CKY.MultiAgentFramework.Services.NLP;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace CKY.MAF.Tests.CustomerService;

/// <summary>
/// ChatService 单元测试
/// 验证消息处理流程、边界条件和异常处理
/// </summary>
public class ChatServiceTests
{
    private readonly ChatService _sut;
    private readonly CustomerServiceLeaderAgent _leaderAgent;
    private readonly Mock<IChatHistoryService> _mockChatHistory;
    private readonly Mock<IOrderService> _mockOrderService;
    private readonly Mock<IKnowledgeBaseService> _mockKbService;
    private readonly Mock<ITicketService> _mockTicketService;

    public ChatServiceTests()
    {
        // 构建 LeaderAgent 的最小依赖
        var keywordProvider = new TestCustomerServiceIntentKeywordProvider();
        var recognizer = new RuleBasedIntentRecognizer(
            keywordProvider, Mock.Of<ILogger<RuleBasedIntentRecognizer>>());
        var entityExtractor = new IntentDrivenEntityExtractor(
            recognizer,
            Mock.Of<IIntentProviderMapping>(),
            Mock.Of<IMafAiAgentRegistry>(),
            Mock.Of<IServiceProvider>(),
            Mock.Of<ILogger<IntentDrivenEntityExtractor>>());

        _mockOrderService = new Mock<IOrderService>();
        _mockKbService = new Mock<IKnowledgeBaseService>();
        _mockTicketService = new Mock<ITicketService>();

        // 默认：知识库返回成功回复
        _mockKbService.Setup(x => x.SearchAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new KnowledgeSearchResult
            {
                GeneratedAnswer = "这是知识库的回答",
                Confidence = 0.8,
                RelevantFaqs = [],
                SourceReferences = [],
            });

        var kbAgent = new KnowledgeBaseAgent(
            _mockKbService.Object,
            Mock.Of<IMafAiAgentRegistry>(),
            Mock.Of<ILogger<KnowledgeBaseAgent>>());

        var orderAgent = new OrderAgent(
            _mockOrderService.Object,
            Mock.Of<IMafAiAgentRegistry>(),
            Mock.Of<ILogger<OrderAgent>>());

        var ticketAgent = new TicketAgent(
            _mockTicketService.Object,
            Mock.Of<IMafAiAgentRegistry>(),
            Mock.Of<ILogger<TicketAgent>>());

        var mockDegradation = new Mock<IDegradationManager>();
        mockDegradation.Setup(x => x.IsFeatureEnabled(It.IsAny<string>())).Returns(true);

        var mockRuleEngine = new Mock<IRuleEngine>();
        mockRuleEngine.Setup(x => x.CanHandle(It.IsAny<string>())).Returns(false);

        _leaderAgent = new CustomerServiceLeaderAgent(
            recognizer,
            entityExtractor,
            kbAgent,
            orderAgent,
            ticketAgent,
            Mock.Of<IUserBehaviorService>(),
            mockDegradation.Object,
            mockRuleEngine.Object,
            Mock.Of<IMafAiAgentRegistry>(),
            Mock.Of<ILogger<CustomerServiceLeaderAgent>>());

        _mockChatHistory = new Mock<IChatHistoryService>();

        _sut = new ChatService(
            _leaderAgent,
            Mock.Of<ILogger<ChatService>>(),
            new ConversationManager(),
            _mockChatHistory.Object);
    }

    [Fact]
    public async Task SendMessageAsync_WithValidInput_ReturnsResponse()
    {
        var response = await _sut.SendMessageAsync("user1", "session1", "你们的退货政策是什么？");

        response.Should().NotBeNull();
        response.Content.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task SendMessageAsync_PersistsUserAndAssistantMessages()
    {
        await _sut.SendMessageAsync("user1", "session1", "你好");

        _mockChatHistory.Verify(
            x => x.SaveMessageAsync("session1", "user", "你好", null, null, It.IsAny<CancellationToken>()),
            Times.Once);
        _mockChatHistory.Verify(
            x => x.SaveMessageAsync("session1", "assistant", It.IsAny<string>(), null, null, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SendMessageAsync_TruncatesLongMessages()
    {
        var longMessage = new string('A', 600);

        await _sut.SendMessageAsync("user1", "session1", longMessage);

        // 验证持久化的消息被截断到500字符
        _mockChatHistory.Verify(
            x => x.SaveMessageAsync("session1", "user", It.Is<string>(s => s.Length == 500), null, null, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SendMessageAsync_WithNullOrWhiteSpaceMessage_ThrowsArgumentException(string? message)
    {
        var act = () => _sut.SendMessageAsync("user1", "session1", message!);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SendMessageAsync_WithNullOrWhiteSpaceUserId_ThrowsArgumentException(string? userId)
    {
        var act = () => _sut.SendMessageAsync(userId!, "session1", "test");

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task SendMessageAsync_OrderQueryInput_RoutesToOrderAgent()
    {
        _mockOrderService.Setup(x => x.GetOrderAsync("ORD-2024-001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OrderInfo
            {
                OrderId = "ORD-2024-001",
                Status = "shipped",
                TotalAmount = 299.00m,
                Items = [new OrderItem { ProductName = "测试商品" }],
                CreatedAt = DateTime.UtcNow,
            });

        var response = await _sut.SendMessageAsync("user1", "session1", "查询订单 ORD-2024-001");

        response.Content.Should().Contain("ORD-2024-001");
    }

    [Fact]
    public async Task SendMessageAsync_WhenChatHistoryFails_StillReturnsResponse()
    {
        _mockChatHistory.Setup(x => x.SaveMessageAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string?>(), It.IsAny<Dictionary<string, string>?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("DB connect failure"));

        var response = await _sut.SendMessageAsync("user1", "session1", "你好");

        response.Should().NotBeNull();
        response.Content.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task SendMessageAsync_WithoutChatHistoryService_StillWorks()
    {
        var sutNoPersistence = new ChatService(
            _leaderAgent,
            Mock.Of<ILogger<ChatService>>(),
            chatHistoryService: null);

        var response = await sutNoPersistence.SendMessageAsync("user1", "session1", "你好");

        response.Should().NotBeNull();
        response.Content.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task RequestHumanHandoffAsync_ReturnsSuccessResponse()
    {
        _mockTicketService.Setup(x => x.CreateTicketAsync(It.IsAny<TicketCreateRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("TKT-001");

        var response = await _sut.RequestHumanHandoffAsync("user1", "session1");

        response.Should().NotBeNull();
        response.Content.Should().NotBeNullOrWhiteSpace();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task RequestHumanHandoffAsync_WithInvalidUserId_ThrowsArgumentException(string? userId)
    {
        var act = () => _sut.RequestHumanHandoffAsync(userId!, "session1");

        await act.Should().ThrowAsync<ArgumentException>();
    }

    /// <summary>
    /// 测试用的意图关键词提供器
    /// 复制自 CustomerServiceIntentKeywordProvider 的数据，隔离测试环境
    /// </summary>
    private class TestCustomerServiceIntentKeywordProvider : IIntentKeywordProvider
    {
        private readonly Dictionary<string, string[]> _intentKeywordMap = new(StringComparer.OrdinalIgnoreCase)
        {
            ["QueryOrder"] = ["查询订单", "查看订单", "订单状态", "我的订单"],
            ["CancelOrder"] = ["取消订单", "不要了", "取消"],
            ["TrackShipping"] = ["快递", "物流", "到哪了", "配送"],
            ["RequestRefund"] = ["退款", "退货", "退钱", "申请退款"],
            ["CreateTicket"] = ["投诉", "工单", "反馈", "建议", "人工"],
            ["QueryTicket"] = ["查询工单", "工单进度"],
            ["GeneralFaq"] = ["怎么", "如何", "什么是", "能不能"],
        };

        public string?[]? GetKeywords(string intent)
        {
            _intentKeywordMap.TryGetValue(intent, out var keywords);
            return keywords;
        }

        public IEnumerable<string> GetSupportedIntents() => _intentKeywordMap.Keys;
    }
}
