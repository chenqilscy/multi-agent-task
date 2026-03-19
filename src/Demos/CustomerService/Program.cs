using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Core.Diagnostics;
using CKY.MultiAgentFramework.Core.Resilience;
using CKY.MultiAgentFramework.Demos.CustomerService;
using CKY.MultiAgentFramework.Demos.CustomerService.Agents;
using CKY.MultiAgentFramework.Demos.CustomerService.Models;
using CKY.MultiAgentFramework.Demos.CustomerService.Services;
using CKY.MultiAgentFramework.Demos.CustomerService.Services.Implementations;
using CKY.MultiAgentFramework.Infrastructure.DependencyInjection;
using CKY.MultiAgentFramework.Services.NLP;
using CKY.MultiAgentFramework.Services.Resilience;
using CKY.MultiAgentFramework.Demos.CustomerService.Components;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

// 使用 MAF Dapper 服务（高性能 + 业务层完全解耦）
builder.Services.AddMafDapperServices(builder.Configuration);

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
// 业务服务注册（模拟实现，生产替换真实适配器）
// ========================================

builder.Services.AddSingleton<IOrderService, SimulatedOrderService>();
builder.Services.AddSingleton<ITicketService, SimulatedTicketService>();
builder.Services.AddSingleton<IKnowledgeBaseService, SimulatedKnowledgeBaseService>();
builder.Services.AddSingleton<IUserBehaviorService, SimulatedUserBehaviorService>();

// 主动服务事件驱动
builder.Services.AddSingleton<IProactiveEventBus, InMemoryProactiveEventBus>();
builder.Services.AddSingleton<IProactiveEventHandler, ShippingDelayEventHandler>();
builder.Services.AddSingleton<IProactiveEventHandler, MembershipExpiringEventHandler>();
builder.Services.AddSingleton<IProactiveEventHandler, BirthdayGreetingEventHandler>();

// 会话管理（多轮对话）
builder.Services.AddSingleton<ConversationManager>();

// ========================================
// 专业 Agent 注册
// ========================================

builder.Services.AddSingleton<KnowledgeBaseAgent>();
builder.Services.AddSingleton<OrderAgent>();
builder.Services.AddSingleton<TicketAgent>();
builder.Services.AddSingleton<CustomerServiceMainAgent>();

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

app.Run();
