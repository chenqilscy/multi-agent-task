using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Core.Agents;
using CKY.MultiAgentFramework.Core.Agents.Specialized;
using CKY.MAF.Demos.SmartHome.Components;
using CKY.MAF.Demos.SmartHome.Hubs;
using CKY.MultiAgentFramework.Demos.SmartHome.Providers;
using CKY.MultiAgentFramework.Demos.SmartHome.Services;
using CKY.MultiAgentFramework.Repository.Data;
using CKY.MultiAgentFramework.Repository.Repositories;
using CKY.MultiAgentFramework.Services.Monitoring;
using CKY.MultiAgentFramework.Services.NLP;
using CKY.MultiAgentFramework.Services.Registry;
using CKY.MultiAgentFramework.Services.RealTime;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Metrics;

var builder = WebApplication.CreateBuilder(args);

// 配置 EF Core SQLite
builder.Services.AddDbContext<MafDbContext>(options =>
    options.UseSqlite("Data Source=smart_home.db"));

// 注册工作单元和仓储
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IMainTaskRepository, MainTaskRepository>();
builder.Services.AddScoped<ISubTaskRepository, SubTaskRepository>();

// ========================================
// Prometheus 监控服务注册
// ========================================

// 注册 Metrics Collector
builder.Services.AddSingleton<IPrometheusMetricsCollector, PrometheusMetricsCollector>();

// 注册系统指标收集器
builder.Services.AddSingleton<SystemMetricsCollector>();

// 配置 OpenTelemetry Metrics 导出到 Prometheus
builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics =>
    {
        metrics
            .AddAspNetCoreInstrumentation()
            .AddMeter("CKY.MAF")
            .AddPrometheusExporter(options =>
            {
                options.ScrapeResponseCacheDurationMilliseconds = 1000;
            });
    });

// ========================================
// LLM Agent 注册
// ========================================

// 1. 注册 LLM Agent Registry
builder.Services.AddSingleton<IMafAiAgentRegistry, LlmAgentRegistry>();

// 2. 注册 LLM Agent（可选 - 需要配置 API Key 时启用）
// builder.Services.AddSingleton<MafAiAgent>(sp =>
// {
//     var config = new LlmProviderConfig
//     {
//         ProviderName = "ZhipuAI",
//         ModelId = "glm-4",
//         ApiKey = builder.Configuration["LLM:ZhipuAI:ApiKey"]!,
//         BaseUrl = "https://open.bigmodel.cn/api/paas/v4/",
//         Priority = 1,
//         IsEnabled = true
//     };
//     return new ZhipuAIAgent(config, sp.GetRequiredService<ILogger<ZhipuAIAgent>>());
// });

// 3. 注册 LLM Agent 到 Registry（启动时）
builder.Services.AddSingleton<MafAgentStartupService>();

// ========================================
// NLP 服务注册
// ========================================

// 1. 注册 Intent Keyword Provider
builder.Services.AddSingleton<IIntentKeywordProvider, SmartHomeIntentKeywordProvider>();

// 2. 注册 Intent Recognizer
builder.Services.AddSingleton<IIntentRecognizer, RuleBasedIntentRecognizer>();

// 3. 注册所有 Entity Pattern Provider
builder.Services.AddSingleton<LightControlEntityPatternProvider>();
builder.Services.AddSingleton<ACControlEntityPatternProvider>();
builder.Services.AddSingleton<CurtainControlEntityPatternProvider>();

// 4. 注册 Intent → Provider 映射
builder.Services.AddSingleton<IIntentProviderMapping>(sp =>
{
    var mapping = new IntentProviderMapping();

    // 控灯场景
    mapping.Register("ControlLight", typeof(LightControlEntityPatternProvider));
    mapping.Register("DimLight", typeof(LightControlEntityPatternProvider));
    mapping.Register("BrightenLight", typeof(LightControlEntityPatternProvider));

    // 控空调场景
    mapping.Register("ControlAC", typeof(ACControlEntityPatternProvider));
    mapping.Register("SetTemperature", typeof(ACControlEntityPatternProvider));
    mapping.Register("SetACMode", typeof(ACControlEntityPatternProvider));

    // 控窗帘场景
    mapping.Register("ControlCurtain", typeof(CurtainControlEntityPatternProvider));
    mapping.Register("OpenCurtain", typeof(CurtainControlEntityPatternProvider));
    mapping.Register("CloseCurtain", typeof(CurtainControlEntityPatternProvider));

    return mapping;
});

// 5. 注册 Entity Extractor
builder.Services.AddSingleton<IEntityExtractor, IntentDrivenEntityExtractor>();

// ========================================
// 专业 Agent 注册
// ========================================

// 注册内置专业 Agent（可根据需要启用）
builder.Services.AddSingleton<IntentRecognitionAgent>();
builder.Services.AddSingleton<DialogueAgent>();
builder.Services.AddSingleton<EmbeddingAgent>();
builder.Services.AddSingleton<ImageAgent>();
builder.Services.AddSingleton<VideoAgent>();
builder.Services.AddSingleton<SummarizationAgent>();
builder.Services.AddSingleton<TranslationAgent>();
builder.Services.AddSingleton<CodeAgent>();

// ========================================
// SignalR 实时通信服务注册
// ========================================

// 添加 SignalR 服务
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
});

// 注册实时通知服务
builder.Services.AddSingleton<IRealTimeNotificationService, RealTimeNotificationService>();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors = true);
    // The default HSTS value is 30 days. You may want to see https://aka.ms/aspnetcore/hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

// 映射 SignalR Hub
app.MapHub<MafHub>("/hub/maf");

// 映射 Prometheus 指标端点
app.MapPrometheusScrapingEndpoint();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
