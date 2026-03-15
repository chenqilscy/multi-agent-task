using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Demos.CustomerService;
using CKY.MultiAgentFramework.Demos.CustomerService.Agents;
using CKY.MultiAgentFramework.Demos.CustomerService.Models;
using CKY.MultiAgentFramework.Demos.CustomerService.Services;
using CKY.MultiAgentFramework.Demos.CustomerService.Services.Implementations;
using CKY.MultiAgentFramework.Infrastructure.DependencyInjection;
using CKY.MultiAgentFramework.Services.NLP;
using CKY.MultiAgentFramework.Demos.CustomerService.Components;
using Microsoft.EntityFrameworkCore;
using CKY.MultiAgentFramework.Infrastructure.Repository.Data;
using CKY.MultiAgentFramework.Infrastructure.Repository.Repositories;

var builder = WebApplication.CreateBuilder(args);

// 配置 EF Core SQLite
builder.Services.AddDbContext<MafDbContext>(options =>
    options.UseSqlite("Data Source=customer_service.db"));

// 自动注册 Infrastructure 服务
builder.Services.AddMafInfrastructureServices(builder.Configuration);

// 注册工作单元和仓储
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IMainTaskRepository, MainTaskRepository>();

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
// Blazor 服务
// ========================================

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found");
app.UseHttpsRedirection();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
