using CKY.MultiAgentFramework.Demos.CustomerService.Data;
using CKY.MultiAgentFramework.Demos.CustomerService.Entities;
using CKY.MultiAgentFramework.Demos.CustomerService.Services;
using CKY.MultiAgentFramework.Demos.CustomerService.Services.Implementations;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace CKY.MAF.Tests.CustomerService;

/// <summary>
/// 持久化服务单元测试 - 使用 EF Core InMemory 提供程序
/// </summary>
public class PersistentServicesTests : IDisposable
{
    private readonly CustomerServiceDbContext _db;

    public PersistentServicesTests()
    {
        var options = new DbContextOptionsBuilder<CustomerServiceDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _db = new CustomerServiceDbContext(options);
    }

    public void Dispose()
    {
        _db.Dispose();
    }

    // ===================================
    // PersistentTicketService 测试
    // ===================================

    [Fact]
    public async Task TicketService_CreateTicket_ShouldPersistAndReturnTicketId()
    {
        // Arrange
        SeedCustomer("user-001", "张三");
        var svc = CreateTicketService();
        var request = new TicketCreateRequest
        {
            UserId = "user-001",
            Title = "订单问题",
            Description = "我的订单迟迟没发货",
            Category = "order",
            Priority = "high"
        };

        // Act
        var ticketId = await svc.CreateTicketAsync(request);

        // Assert
        ticketId.Should().StartWith("TKT-");
        var saved = await _db.Tickets.FirstOrDefaultAsync(t => t.TicketId == ticketId);
        saved.Should().NotBeNull();
        saved!.Title.Should().Be("订单问题");
        saved.Priority.Should().Be("high");
        saved.Status.Should().Be("open");
    }

    [Fact]
    public async Task TicketService_CreateTicket_ForNewUser_ShouldAutoCreateCustomer()
    {
        var svc = CreateTicketService();
        var request = new TicketCreateRequest
        {
            UserId = "new-user-999",
            Title = "首次咨询",
            Description = "如何注册会员",
            Category = "other"
        };

        var ticketId = await svc.CreateTicketAsync(request);

        ticketId.Should().NotBeNullOrEmpty();
        var customer = await _db.Customers.FirstOrDefaultAsync(c => c.CustomerId == "new-user-999");
        customer.Should().NotBeNull();
    }

    [Fact]
    public async Task TicketService_GetTicket_ShouldIncludeComments()
    {
        SeedCustomer("user-001", "张三");
        var svc = CreateTicketService();
        var ticketId = await svc.CreateTicketAsync(new TicketCreateRequest
        {
            UserId = "user-001",
            Title = "测试工单",
            Description = "测试描述"
        });

        await svc.UpdateTicketAsync(ticketId, new TicketUpdateRequest
        {
            Comment = "第一条评论"
        });

        var ticket = await svc.GetTicketAsync(ticketId);

        ticket.Should().NotBeNull();
        ticket!.Comments.Should().HaveCount(1);
        ticket.Comments[0].Content.Should().Be("第一条评论");
    }

    [Fact]
    public async Task TicketService_UpdateStatus_ShouldSetResolvedAt()
    {
        SeedCustomer("user-001", "张三");
        var svc = CreateTicketService();
        var ticketId = await svc.CreateTicketAsync(new TicketCreateRequest
        {
            UserId = "user-001",
            Title = "已解决工单",
            Description = "测试"
        });

        await svc.UpdateTicketAsync(ticketId, new TicketUpdateRequest
        {
            Status = "resolved"
        });

        var ticket = await svc.GetTicketAsync(ticketId);
        ticket!.Status.Should().Be("resolved");
        ticket.ResolvedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task TicketService_GetUserTickets_ShouldReturnOnlyUserTickets()
    {
        SeedCustomer("user-001", "张三");
        SeedCustomer("user-002", "李四");
        var svc = CreateTicketService();

        await svc.CreateTicketAsync(new TicketCreateRequest { UserId = "user-001", Title = "工单A", Description = "A" });
        await svc.CreateTicketAsync(new TicketCreateRequest { UserId = "user-002", Title = "工单B", Description = "B" });
        await svc.CreateTicketAsync(new TicketCreateRequest { UserId = "user-001", Title = "工单C", Description = "C" });

        var user1Tickets = await svc.GetUserTicketsAsync("user-001");
        user1Tickets.Should().HaveCount(2);
    }

    // ===================================
    // PersistentKnowledgeBaseService 测试
    // ===================================

    [Fact]
    public async Task KnowledgeBase_Search_ShouldMatchByKeywords()
    {
        SeedFaq("如何退款？", "申请退款步骤...", "refund", ["退款", "退货", "退钱"]);
        var svc = CreateKnowledgeBaseService();

        var result = await svc.SearchAsync("我想申请退款");

        result.RelevantFaqs.Should().NotBeEmpty();
        result.RelevantFaqs[0].Question.Should().Be("如何退款？");
    }

    [Fact]
    public async Task KnowledgeBase_Search_ShouldReturnEmptyForNoMatch()
    {
        SeedFaq("如何退款？", "...", "refund", ["退款"]);
        var svc = CreateKnowledgeBaseService();

        var result = await svc.SearchAsync("今天天气如何");

        result.RelevantFaqs.Should().BeEmpty();
        result.Confidence.Should().Be(0);
    }

    [Fact]
    public async Task KnowledgeBase_UpsertFaq_ShouldAddNewEntry()
    {
        var svc = CreateKnowledgeBaseService();

        await svc.UpsertFaqAsync(new FaqEntry
        {
            Question = "配送范围",
            Answer = "全国配送",
            Category = "shipping",
            Keywords = ["配送", "送到"]
        });

        var entries = await _db.FaqEntries.ToListAsync();
        entries.Should().HaveCount(1);
        entries[0].Question.Should().Be("配送范围");
    }

    [Fact]
    public async Task KnowledgeBase_HasDefinitiveAnswer_ShouldReturnTrueForHighConfidence()
    {
        SeedFaq("如何退款？", "退款步骤如下...", "refund", ["退款", "退货", "退钱"]);
        var svc = CreateKnowledgeBaseService();

        // "退款退货" 匹配两个关键词，得分 0.6，低于 0.7 阈值
        // 但"退款退货退钱" 匹配三个关键词，得分 0.9
        var result = await svc.HasDefinitiveAnswerAsync("退款退货退钱");
        result.Should().BeTrue();
    }

    // ===================================
    // PersistentChatHistoryService 测试
    // ===================================

    [Fact]
    public async Task ChatHistory_CreateSession_ShouldReturnSessionId()
    {
        var svc = CreateChatHistoryService();

        var sessionId = await svc.CreateSessionAsync();

        sessionId.Should().NotBeNullOrEmpty();
        var session = await _db.ChatSessions.FirstOrDefaultAsync(s => s.SessionId == sessionId);
        session.Should().NotBeNull();
        session!.Status.Should().Be("active");
    }

    [Fact]
    public async Task ChatHistory_SaveMessage_ShouldPersistMessage()
    {
        var svc = CreateChatHistoryService();
        var sessionId = await svc.CreateSessionAsync();

        await svc.SaveMessageAsync(sessionId, "user", "你好，我想查订单");
        await svc.SaveMessageAsync(sessionId, "assistant", "好的，请提供订单号");

        var messages = await svc.GetSessionMessagesAsync(sessionId);
        messages.Should().HaveCount(2);
        messages[0].Role.Should().Be("user");
        messages[1].Role.Should().Be("assistant");
    }

    [Fact]
    public async Task ChatHistory_SaveMessage_WithEntities_ShouldPersistJson()
    {
        var svc = CreateChatHistoryService();
        var sessionId = await svc.CreateSessionAsync();

        await svc.SaveMessageAsync(sessionId, "user", "查询订单 ORD-001",
            intent: "order_query",
            entities: new Dictionary<string, string> { ["orderId"] = "ORD-001" });

        var messages = await svc.GetSessionMessagesAsync(sessionId);
        messages[0].Intent.Should().Be("order_query");
        messages[0].EntitiesJson.Should().Contain("ORD-001");
    }

    [Fact]
    public async Task ChatHistory_CloseSession_ShouldUpdateStatus()
    {
        var svc = CreateChatHistoryService();
        var sessionId = await svc.CreateSessionAsync();

        await svc.CloseSessionAsync(sessionId, "用户查询了订单状态");

        var session = await _db.ChatSessions.FirstOrDefaultAsync(s => s.SessionId == sessionId);
        session!.Status.Should().Be("closed");
        session.EndedAt.Should().NotBeNull();
        session.Summary.Should().Be("用户查询了订单状态");
    }

    [Fact]
    public async Task ChatHistory_GetSessionMessages_WithLimit_ShouldRespectLimit()
    {
        var svc = CreateChatHistoryService();
        var sessionId = await svc.CreateSessionAsync();

        for (int i = 0; i < 10; i++)
            await svc.SaveMessageAsync(sessionId, "user", $"消息{i}");

        var limited = await svc.GetSessionMessagesAsync(sessionId, limit: 3);
        limited.Should().HaveCount(3);
    }

    // ===================================
    // PersistentCustomerService 测试
    // ===================================

    [Fact]
    public async Task CustomerService_GetCustomer_ShouldFindByBusinessId()
    {
        SeedCustomer("CUST-001", "张三", "zhangsan@test.com");
        var svc = CreateCustomerService();

        var customer = await svc.GetCustomerAsync("CUST-001");

        customer.Should().NotBeNull();
        customer!.Name.Should().Be("张三");
        customer.Email.Should().Be("zhangsan@test.com");
    }

    [Fact]
    public async Task CustomerService_UpsertCustomer_ShouldCreateNew()
    {
        var svc = CreateCustomerService();

        var customer = await svc.UpsertCustomerAsync("NEW-001", "新用户", "new@test.com");

        customer.Should().NotBeNull();
        customer.CustomerId.Should().Be("NEW-001");
        var saved = await _db.Customers.FirstOrDefaultAsync(c => c.CustomerId == "NEW-001");
        saved.Should().NotBeNull();
    }

    [Fact]
    public async Task CustomerService_UpsertCustomer_ShouldUpdateExisting()
    {
        SeedCustomer("CUST-001", "旧名字");
        var svc = CreateCustomerService();

        var customer = await svc.UpsertCustomerAsync("CUST-001", "新名字", "new@test.com");

        customer.Name.Should().Be("新名字");
        customer.Email.Should().Be("new@test.com");
        var total = await _db.Customers.CountAsync();
        total.Should().Be(1);
    }

    [Fact]
    public async Task CustomerService_UpdateLastActive_ShouldUpdateTimestamp()
    {
        SeedCustomer("CUST-001", "张三");
        var svc = CreateCustomerService();

        await svc.UpdateLastActiveAsync("CUST-001");

        var customer = await _db.Customers.FirstOrDefaultAsync(c => c.CustomerId == "CUST-001");
        customer!.LastActiveAt.Should().NotBeNull();
    }

    // ===================================
    // PersistentUserBehaviorService 测试
    // ===================================

    [Fact]
    public async Task UserBehavior_Record_ShouldPersist()
    {
        var svc = CreateUserBehaviorService();

        await svc.RecordAsync(new UserBehaviorRecord
        {
            UserId = "user-001",
            SessionId = "sess-001",
            Intent = "order_query",
            TaskSucceeded = true,
            ResponseTime = TimeSpan.FromMilliseconds(500),
            Entities = new() { ["orderId"] = "ORD-001" }
        });

        var records = await _db.UserBehaviorRecords.ToListAsync();
        records.Should().HaveCount(1);
        records[0].Intent.Should().Be("order_query");
        records[0].ResponseTimeMs.Should().Be(500);
    }

    [Fact]
    public async Task UserBehavior_GetProfile_ShouldAggregateIntentFrequency()
    {
        var svc = CreateUserBehaviorService();

        for (int i = 0; i < 3; i++)
            await svc.RecordAsync(new UserBehaviorRecord
            {
                UserId = "user-001",
                SessionId = $"sess-{i}",
                Intent = "order_query",
                TaskSucceeded = true,
                ResponseTime = TimeSpan.FromMilliseconds(100)
            });

        await svc.RecordAsync(new UserBehaviorRecord
        {
            UserId = "user-001",
            SessionId = "sess-4",
            Intent = "refund_request",
            TaskSucceeded = true,
            ResponseTime = TimeSpan.FromMilliseconds(200)
        });

        var profile = await svc.GetUserProfileAsync("user-001");

        profile.Should().NotBeNull();
        profile!.TotalInteractions.Should().Be(4);
        profile.IntentFrequency["order_query"].Should().Be(3);
        profile.IntentFrequency["refund_request"].Should().Be(1);
    }

    [Fact]
    public async Task UserBehavior_GetDefaultEntities_ShouldReturnRecentEntities()
    {
        var svc = CreateUserBehaviorService();

        await svc.RecordAsync(new UserBehaviorRecord
        {
            UserId = "user-001",
            SessionId = "sess-1",
            Intent = "order_query",
            TaskSucceeded = true,
            ResponseTime = TimeSpan.FromMilliseconds(100),
            Entities = new() { ["orderId"] = "ORD-001" }
        });

        var entities = await svc.GetDefaultEntitiesAsync("user-001");

        entities.Should().ContainKey("orderId");
        entities["orderId"].Should().Be("ORD-001");
    }

    [Fact]
    public async Task UserBehavior_GetProfile_ForUnknownUser_ShouldReturnNull()
    {
        var svc = CreateUserBehaviorService();

        var profile = await svc.GetUserProfileAsync("nonexistent");

        profile.Should().BeNull();
    }

    // ===================================
    // 辅助方法
    // ===================================

    private void SeedCustomer(string customerId, string name, string? email = null)
    {
        _db.Customers.Add(new CustomerEntity
        {
            CustomerId = customerId,
            Name = name,
            Email = email,
            CreatedAt = DateTime.UtcNow
        });
        _db.SaveChanges();
    }

    private void SeedFaq(string question, string answer, string category, List<string> keywords)
    {
        _db.FaqEntries.Add(new FaqEntryEntity
        {
            Question = question,
            Answer = answer,
            Category = category,
            KeywordsJson = System.Text.Json.JsonSerializer.Serialize(keywords),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
        _db.SaveChanges();
    }

    private PersistentTicketService CreateTicketService() =>
        new(_db, Mock.Of<ILogger<PersistentTicketService>>());

    private PersistentKnowledgeBaseService CreateKnowledgeBaseService() =>
        new(_db, Mock.Of<ILogger<PersistentKnowledgeBaseService>>());

    private PersistentChatHistoryService CreateChatHistoryService() =>
        new(_db, Mock.Of<ILogger<PersistentChatHistoryService>>());

    private PersistentCustomerService CreateCustomerService() =>
        new(_db, Mock.Of<ILogger<PersistentCustomerService>>());

    private PersistentUserBehaviorService CreateUserBehaviorService() =>
        new(_db, Mock.Of<ILogger<PersistentUserBehaviorService>>());
}
