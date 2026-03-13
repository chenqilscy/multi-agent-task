using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Demos.SmartHome.Providers;
using CKY.MultiAgentFramework.Repository.Data;
using CKY.MultiAgentFramework.Services.NLP;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// 配置 EF Core SQLite
builder.Services.AddDbContext<MafDbContext>(options =>
    options.UseSqlite("Data Source=smart_home.db"));

// 注册工作单元和仓储
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IMainTaskRepository, MainTaskRepository>();
builder.Services.AddScoped<ISubTaskRepository, SubTaskRepository>();

// ========================================
// NLP 服务注册
// ========================================

// 1. 注册 LLM Service（可选 - 需要 API Key 时启用）
// builder.Services.AddSingleton<LlmAgent>();
// builder.Services.AddSingleton<ILlmService, LlmServiceAdapter>();

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

// 5. 注册 Entity Extractor（如果未注册 ILlmService，将使用关键字匹配模式）
builder.Services.AddSingleton<IEntityExtractor, IntentDrivenEntityExtractor>();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
