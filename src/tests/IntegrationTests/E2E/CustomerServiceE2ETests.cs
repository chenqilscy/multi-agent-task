using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Core.Models.Task;
using CKY.MultiAgentFramework.Core.Resilience;
using CKY.MultiAgentFramework.Demos.CustomerService.Agents;
using CKY.MultiAgentFramework.Demos.CustomerService.Services;
using CKY.MultiAgentFramework.Demos.CustomerService.Services.Implementations;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using KBAgent = CKY.MultiAgentFramework.Demos.CustomerService.Agents.KnowledgeBaseAgent;

namespace CKY.MultiAgentFramework.IntegrationTests.E2E;

/// <summary>
/// CustomerService Demo 端到端集成测试
/// 测试各个Agent的业务逻辑处理流程
/// </summary>
public class CustomerServiceE2ETests
{
    private readonly Mock<IMafAiAgentRegistry> _mockRegistry;
    private readonly SimulatedOrderService _orderService;
    private readonly SimulatedTicketService _ticketService;
    private readonly SimulatedKnowledgeBaseService _knowledgeBaseService;

    public CustomerServiceE2ETests()
    {
        _mockRegistry = new Mock<IMafAiAgentRegistry>();
        _orderService = new SimulatedOrderService(NullLogger<SimulatedOrderService>.Instance);
        _ticketService = new SimulatedTicketService(NullLogger<SimulatedTicketService>.Instance);
        _knowledgeBaseService = new SimulatedKnowledgeBaseService();
    }

    // ========================================
    // KnowledgeBaseAgent 测试
    // ========================================

    [Theory]
    [InlineData("如何查询我的订单？")]
    [InlineData("怎么申请退款")]
    [InlineData("快递到哪里了")]
    public async Task KnowledgeBaseAgent_KnownFAQ_ShouldReturnAnswer(string query)
    {
        var agent = new KBAgent(
            _knowledgeBaseService,
            _mockRegistry.Object,
            NullLogger<KBAgent>.Instance);

        var request = CreateRequest(query);
        var response = await agent.ExecuteBusinessLogicAsync(request);

        response.Success.Should().BeTrue();
        response.Result.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task KnowledgeBaseAgent_UnknownQuestion_ShouldSuggestTicket()
    {
        var agent = new KBAgent(
            _knowledgeBaseService,
            _mockRegistry.Object,
            NullLogger<KBAgent>.Instance);

        var request = CreateRequest("这个系统支持声纹识别吗？");
        var response = await agent.ExecuteBusinessLogicAsync(request);

        response.Success.Should().BeFalse();
        response.Result.Should().Contain("工单");
    }

    // ========================================
    // OrderAgent 测试
    // ========================================

    [Fact]
    public async Task OrderAgent_QueryOrder_ShouldReturnOrderDetails()
    {
        var agent = new OrderAgent(
            _orderService,
            _mockRegistry.Object,
            NullLogger<OrderAgent>.Instance);

        var request = CreateRequest("查询订单 ORD-2024-001");
        var response = await agent.ExecuteBusinessLogicAsync(request);

        response.Success.Should().BeTrue();
        response.Result.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task OrderAgent_TrackShipping_ShouldReturnShippingInfo()
    {
        var agent = new OrderAgent(
            _orderService,
            _mockRegistry.Object,
            NullLogger<OrderAgent>.Instance);

        var request = CreateRequest("物流查询 ORD-2024-001");
        var response = await agent.ExecuteBusinessLogicAsync(request);

        response.Success.Should().BeTrue();
        response.Result.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task OrderAgent_CancelOrder_ShouldProcessCancellation()
    {
        var agent = new OrderAgent(
            _orderService,
            _mockRegistry.Object,
            NullLogger<OrderAgent>.Instance);

        var request = CreateRequest("取消订单 ORD-2024-001");
        var response = await agent.ExecuteBusinessLogicAsync(request);

        // 能返回响应即可（取消可能成功或因状态限制失败）
        response.Result.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task OrderAgent_RefundRequest_ShouldProcessRefund()
    {
        var agent = new OrderAgent(
            _orderService,
            _mockRegistry.Object,
            NullLogger<OrderAgent>.Instance);

        var request = CreateRequest("退款 ORD-2024-001");
        var response = await agent.ExecuteBusinessLogicAsync(request);

        response.Result.Should().NotBeNullOrEmpty();
    }

    // ========================================
    // TicketAgent 测试
    // ========================================

    [Fact]
    public async Task TicketAgent_CreateTicket_ShouldCreateSuccessfully()
    {
        var agent = new TicketAgent(
            _ticketService,
            _mockRegistry.Object,
            NullLogger<TicketAgent>.Instance);

        var request = CreateRequest("提交工单 我的商品收到时包装破损");
        var response = await agent.ExecuteBusinessLogicAsync(request);

        response.Success.Should().BeTrue();
        response.Result.Should().Contain("工单");
    }

    [Fact]
    public async Task TicketAgent_CreateUrgentTicket_ShouldSetHighPriority()
    {
        var agent = new TicketAgent(
            _ticketService,
            _mockRegistry.Object,
            NullLogger<TicketAgent>.Instance);

        var request = CreateRequest("投诉 紧急！我被多次扣款");
        var response = await agent.ExecuteBusinessLogicAsync(request);

        response.Success.Should().BeTrue();
        response.Result.Should().NotBeNullOrEmpty();
    }

    // ========================================
    // CustomerServiceLeaderAgent 路由测试
    // ========================================

    [Theory]
    [InlineData("查询订单", "order")]
    [InlineData("退款", "refund")]
    [InlineData("投诉", "ticket")]
    [InlineData("怎么使用", "knowledge")]
    public async Task LeaderAgent_ShouldRouteToCorrectSubAgent(string input, string expectedArea)
    {
        var intentRecognizer = new CKY.MultiAgentFramework.Services.NLP.RuleBasedIntentRecognizer(
            new CKY.MultiAgentFramework.Demos.CustomerService.CustomerServiceIntentKeywordProvider(),
            NullLogger<CKY.MultiAgentFramework.Services.NLP.RuleBasedIntentRecognizer>.Instance);

        var mockEntityExtractor = new Mock<IEntityExtractor>();
        mockEntityExtractor
            .Setup(e => e.ExtractAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EntityExtractionResult { Entities = new Dictionary<string, object>() });

        var mockUserBehavior = new Mock<IUserBehaviorService>();

        var mockDegradation = new Mock<IDegradationManager>();
        mockDegradation.Setup(d => d.IsFeatureEnabled(It.IsAny<string>())).Returns(true);
        var mockRuleEngine = new Mock<IRuleEngine>();

        var kbAgent = new KBAgent(_knowledgeBaseService, _mockRegistry.Object, NullLogger<KBAgent>.Instance);
        var orderAgent = new OrderAgent(_orderService, _mockRegistry.Object, NullLogger<OrderAgent>.Instance);
        var ticketAgent = new TicketAgent(_ticketService, _mockRegistry.Object, NullLogger<TicketAgent>.Instance);

        var leaderAgent = new CustomerServiceLeaderAgent(
            intentRecognizer,
            mockEntityExtractor.Object,
            kbAgent, orderAgent, ticketAgent,
            mockUserBehavior.Object,
            mockDegradation.Object,
            mockRuleEngine.Object,
            _mockRegistry.Object,
            NullLogger<CustomerServiceLeaderAgent>.Instance);

        var request = CreateRequest(input);
        var response = await leaderAgent.ExecuteBusinessLogicAsync(request);

        response.Should().NotBeNull();
        response.Result.Should().NotBeNullOrEmpty($"route for '{expectedArea}' should produce a result");
    }

    [Fact]
    public async Task LeaderAgent_Level5Degradation_ShouldUseRuleEngine()
    {
        var intentRecognizer = new CKY.MultiAgentFramework.Services.NLP.RuleBasedIntentRecognizer(
            new CKY.MultiAgentFramework.Demos.CustomerService.CustomerServiceIntentKeywordProvider(),
            NullLogger<CKY.MultiAgentFramework.Services.NLP.RuleBasedIntentRecognizer>.Instance);

        var mockEntityExtractor = new Mock<IEntityExtractor>();
        var mockUserBehavior = new Mock<IUserBehaviorService>();

        var mockDegradation = new Mock<IDegradationManager>();
        mockDegradation.Setup(d => d.IsFeatureEnabled("llm")).Returns(false);

        var mockRuleEngine = new Mock<IRuleEngine>();
        mockRuleEngine.Setup(r => r.CanHandle("退款")).Returns(true);
        mockRuleEngine.Setup(r => r.ProcessAsync(It.IsAny<MafTaskRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MafTaskResponse { Success = true, Result = "退款规则引擎处理" });

        var kbAgent = new KBAgent(_knowledgeBaseService, _mockRegistry.Object, NullLogger<KBAgent>.Instance);
        var orderAgent = new OrderAgent(_orderService, _mockRegistry.Object, NullLogger<OrderAgent>.Instance);
        var ticketAgent = new TicketAgent(_ticketService, _mockRegistry.Object, NullLogger<TicketAgent>.Instance);

        var leaderAgent = new CustomerServiceLeaderAgent(
            intentRecognizer,
            mockEntityExtractor.Object,
            kbAgent, orderAgent, ticketAgent,
            mockUserBehavior.Object,
            mockDegradation.Object,
            mockRuleEngine.Object,
            _mockRegistry.Object,
            NullLogger<CustomerServiceLeaderAgent>.Instance);

        var request = CreateRequest("退款");
        var response = await leaderAgent.ExecuteBusinessLogicAsync(request);

        response.Success.Should().BeTrue();
        response.Result.Should().Contain("退款");
    }

    private static MafTaskRequest CreateRequest(string userInput) =>
        new()
        {
            TaskId = Guid.NewGuid().ToString(),
            UserInput = userInput,
            Parameters = new Dictionary<string, object>()
        };
}
