using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Core.Diagnostics;
using CKY.MultiAgentFramework.Core.Resilience;
using CKY.MultiAgentFramework.Demos.CustomerService;
using CKY.MultiAgentFramework.Demos.CustomerService.Agents;
using CKY.MultiAgentFramework.Demos.CustomerService.Data;
using CKY.MultiAgentFramework.Demos.CustomerService.Data.SeedData;
using CKY.MultiAgentFramework.Demos.CustomerService.Models;
using CKY.MultiAgentFramework.Demos.CustomerService.Services;
using CKY.MultiAgentFramework.Demos.CustomerService.Services.Implementations;
using CKY.MultiAgentFramework.Infrastructure.DependencyInjection;
using CKY.MultiAgentFramework.Services.DependencyInjection;
using CKY.MultiAgentFramework.Services.NLP;
using CKY.MultiAgentFramework.Services.Resilience;
using CKY.MultiAgentFramework.Demos.CustomerService.Components;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

// 使用 MAF Dapper 服务（高性能 + 业务层完全解耦）
builder.Services.AddMafDapperServices(builder.Configuration);

// 注册 LLM Agent 工厂及 HttpClient
builder.Services.AddLlmAgentFactory();

// ========================================
// 客服系统业务数据库（独立于 MAF 框架数据库）
// ========================================

var csDbProvider = builder.Configuration["CustomerService:Database:Provider"] ?? "SQLite";
builder.Services.AddDbContext<CustomerServiceDbContext>(options =>
{
    if (csDbProvider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase))
    {
        var connStr = builder.Configuration.GetConnectionString("CustomerService");
        if (string.IsNullOrEmpty(connStr))
            throw new InvalidOperationException("CustomerService PostgreSQL 连接字符串未配置");
        options.UseNpgsql(connStr);
    }
    else
    {
        var connStr = builder.Configuration.GetConnectionString("CustomerService") ?? "Data Source=customer_service.db";
        options.UseSqlite(connStr);
    }
});

// ========================================
// LLM Agent 注册
// ========================================

builder.Services.AddSingleton<IMafAiAgentRegistry, CKY.MultiAgentFramework.Services.Registry.MafAiAgentRegistry>();

// ========================================
// NLP 服务注册
// ========================================

builder.Services.AddSingleton<IIntentKeywordProvider, CustomerServiceIntentKeywordProvider>();
builder.Services.AddSingleton<IIntentRecognizer, RuleBasedIntentRecognizer>();
builder.Services.AddSingleton<IIntentProviderMapping, IntentProviderMapping>();
builder.Services.AddSingleton<IEntityExtractor, IntentDrivenEntityExtractor>();

// ========================================
// 业务服务注册（通过配置切换模拟/持久化实现）
// ========================================

var usePersistentStorage = builder.Configuration.GetValue<bool>("CustomerService:UsePersistentStorage");

if (usePersistentStorage)
{
    // 外部 API 适配器（当前使用本地 Mock 实现）
    builder.Services.AddScoped<CKY.MultiAgentFramework.Demos.CustomerService.ExternalApis.IExternalOrderApi,
        CKY.MultiAgentFramework.Demos.CustomerService.ExternalApis.MockExternalOrderApi>();
    builder.Services.AddScoped<CKY.MultiAgentFramework.Demos.CustomerService.ExternalApis.IExternalLogisticsApi,
        CKY.MultiAgentFramework.Demos.CustomerService.ExternalApis.MockExternalLogisticsApi>();
    builder.Services.AddScoped<CKY.MultiAgentFramework.Demos.CustomerService.ExternalApis.IExternalPaymentApi,
        CKY.MultiAgentFramework.Demos.CustomerService.ExternalApis.MockExternalPaymentApi>();

    // 持久化业务服务（EF Core）
    builder.Services.AddScoped<IOrderService, PersistentOrderService>();
    builder.Services.AddScoped<ITicketService, PersistentTicketService>();
    builder.Services.AddScoped<IKnowledgeBaseService, RagEnhancedKnowledgeBaseService>();
    builder.Services.AddScoped<IUserBehaviorService, PersistentUserBehaviorService>();
    builder.Services.AddScoped<IChatHistoryService, PersistentChatHistoryService>();
    builder.Services.AddScoped<ICustomerService, PersistentCustomerService>();
}
else
{
    // 模拟实现（内存，适合演示和开发）
    builder.Services.AddSingleton<IOrderService, SimulatedOrderService>();
    builder.Services.AddSingleton<ITicketService, SimulatedTicketService>();
    builder.Services.AddSingleton<IKnowledgeBaseService, SimulatedKnowledgeBaseService>();
    builder.Services.AddSingleton<IUserBehaviorService, SimulatedUserBehaviorService>();
}

// 主动服务事件驱动
builder.Services.AddSingleton<IProactiveEventBus, InMemoryProactiveEventBus>();
builder.Services.AddSingleton<IProactiveEventHandler, ShippingDelayEventHandler>();
builder.Services.AddSingleton<IProactiveEventHandler, MembershipExpiringEventHandler>();
builder.Services.AddSingleton<IProactiveEventHandler, BirthdayGreetingEventHandler>();
builder.Services.AddSingleton<IProactiveEventHandler, PromotionRecommendationEventHandler>();
builder.Services.AddSingleton<IProactiveEventHandler, AnomalousTransactionEventHandler>();

// 问题升级服务
builder.Services.AddSingleton<IEscalationService, SimulatedEscalationService>();

// 会话管理（多轮对话）
builder.Services.AddSingleton<ConversationManager>();

// 客服知识库RAG种子数据服务
builder.Services.AddHostedService<CustomerServiceKnowledgeBaseSeedService>();

// ========================================
// 专业 Agent 注册
// ========================================

if (usePersistentStorage)
{
    // 持久化模式下 Agent 使用 Scoped（因依赖 Scoped 的 DbContext）
    builder.Services.AddScoped<KnowledgeBaseAgent>();
    builder.Services.AddScoped<OrderAgent>();
    builder.Services.AddScoped<TicketAgent>();
    builder.Services.AddScoped<CustomerServiceLeaderAgent>();
}
else
{
    builder.Services.AddSingleton<KnowledgeBaseAgent>();
    builder.Services.AddSingleton<OrderAgent>();
    builder.Services.AddSingleton<TicketAgent>();
    builder.Services.AddSingleton<CustomerServiceLeaderAgent>();
}

// ========================================
// 降级策略注册
// ========================================

builder.Services.AddSingleton<IDegradationManager, DegradationManager>();
builder.Services.AddSingleton<IRuleEngine, CustomerServiceRuleEngine>();

// ========================================
// Blazor 服务
// ========================================

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// ========================================
// OpenTelemetry Metrics + Tracing
// ========================================

builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics =>
    {
        metrics
            .AddAspNetCoreInstrumentation()
            .AddMeter("CKY.MAF")
            .AddMeter("CKY.MAF.BusinessAgent")
            .AddMeter("CKY.MultiAgentFramework")
            .AddPrometheusExporter();
    })
    .WithTracing(tracing =>
    {
        tracing
            .AddAspNetCoreInstrumentation()
            .AddSource(MafActivitySource.AgentSourceName)
            .AddSource(MafActivitySource.TaskSourceName)
            .AddSource(MafActivitySource.LlmSourceName);

        var otlpEndpoint = builder.Configuration["OpenTelemetry:OtlpEndpoint"];
        if (!string.IsNullOrEmpty(otlpEndpoint))
        {
            tracing.AddOtlpExporter(options =>
            {
                options.Endpoint = new Uri(otlpEndpoint);
            });
        }

        if (builder.Environment.IsDevelopment())
        {
            tracing.AddConsoleExporter();
        }
    });

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found");
app.UseHttpsRedirection();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Prometheus 指标端点
app.MapPrometheusScrapingEndpoint();

// 初始化客服系统业务数据库和种子数据
await CustomerServiceSeedData.InitializeAsync(app.Services);

app.Run();
