# CKY.MAF 实现指南

> **文档版本**: v2.0 (最终整合版)
> **更新日期**: 2026-03-16
> **整合来源**: 09-implementation-guide.md + 06-interface-design-spec.md + 14-error-handling-guide.md

---

## 📋 文档说明

本文档整合了 CKY.MAF 的实现细节，包括：
- 项目结构规范
- 核心接口定义
- Agent实现模式
- 依赖注入配置
- 错误处理最佳实践
- 测试策略

---

## 一、项目结构规范

### 1.1 完整目录结构

```
CKY.MAF/
├── src/
│   ├── Core/                                   # Layer 1: 核心抽象层
│   │   ├── Abstractions/
│   │   │   ├── Interfaces/
│   │   │   │   ├── ICacheStore.cs              # 缓存存储抽象
│   │   │   │   ├── IVectorStore.cs             # 向量存储抽象
│   │   │   │   ├── IRelationalDatabase.cs      # 关系数据库抽象
│   │   │   │   ├── IUnitOfWork.cs              # 工作单元模式
│   │   │   │   ├── IMafAiAgentRegistry.cs     # LLM注册表
│   │   │   │   └── ISessionStorage.cs          # 会话存储
│   │   │   └── Models/
│   │   │       ├── MainTask.cs                 # 任务实体
│   │   │       ├── SubTask.cs
│   │   │       ├── LlmProviderConfig.cs        # LLM配置
│   │   │       └── LlmScenario.cs              # LLM场景枚举
│   │   ├── Agents/
│   │   │   ├── MafBusinessAgentBase.cs                 # 纯业务基类
│   │   │   └── AiAgents/
│   │   │       ├── MafAiAgent.cs               # LLM抽象基类 : AIAgent
│   │   │       └── ILlmService.cs              # LLM服务接口
│   │   └── Exceptions/
│   │       ├── MafException.cs
│   │       ├── LlmServiceException.cs
│   │       └── CacheServiceException.cs
│   │
│   ├── Infrastructure/Repository/              # Layer 3: 数据访问层
│   │   ├── Data/
│   │   │   ├── MafDbContext.cs                 # EF Core DbContext
│   │   │   ├── MafDbContextFactory.cs          # 设计时工厂
│   │   │   └── EntityTypeConfigurations/       # EF Core配置
│   │   ├── Repositories/
│   │   │   ├── MainTaskRepository.cs           # Repository实现
│   │   │   ├── SubTaskRepository.cs
│   │   │   └── UnitOfWork.cs
│   │   └── Relational/
│   │       └── EfCoreRelationalDatabase.cs     # EF Core实现
│   │
│   ├── Infrastructure/Caching/                 # 缓存实现
│   │   ├── RedisCacheStore.cs                  # Redis实现
│   │   └── MemoryCacheStore.cs                 # 内存实现（测试）
│   │
│   ├── Infrastructure/Vectorization/           # 向量存储实现
│   │   ├── QdrantVectorStore.cs                # Qdrant实现
│   │   └── MemoryVectorStore.cs                # 内存实现
│   │
│   ├── Services/                               # Layer 4: 业务服务层
│   │   ├── Scheduling/
│   │   │   ├── MafTaskScheduler.cs             # 任务调度器
│   │   │   └── PriorityCalculator.cs
│   │   ├── IntentRecognition/
│   │   │   └── MafIntentRecognizer.cs
│   │   ├── Orchestration/
│   │   │   ├── MafTaskOrchestrator.cs
│   │   │   └── MafAiAgentRegistry.cs           # LLM注册表实现
│   │   ├── Resilience/
│   │   │   ├── DegradationManager.cs           # 降级管理器
│   │   │   └── MafRetryPolicy.cs
│   │   └── Factory/
│   │       └── LlmAgentFactory.cs              # LLM工厂
│   │
│   └── Demos/SmartHome/                        # Layer 5: Demo应用层
│       ├── Pages/                              # Blazor页面
│       ├── Components/
│       └── Program.cs                          # 服务注册
│
└── tests/
    ├── UnitTests/                              # 单元测试
    │   ├── Core/
    │   ├── Services/
    │   └── Repositories/
    └── IntegrationTests/                       # 集成测试
        └── Testcontainers/
```

### 1.2 命名规范

**项目命名**:
- `CKY.MultiAgentFramework.Core` - 核心抽象层
- `CKY.MultiAgentFramework.Infrastructure.Repository` - 数据访问实现
- `CKY.MultiAgentFramework.Infrastructure.Caching` - 缓存实现
- `CKY.MultiAgentFramework.Services` - 业务服务层
- `CKY.MultiAgentFramework.Demos.SmartHome` - Demo应用

**文件命名**:
- 接口：`I`前缀 (如 `ICacheStore`)
- 抽象类：`Base`后缀 (如 `MafBusinessAgentBase`)
- 实现类：具体实现名称 (如 `RedisCacheStore`)

---

## 二、核心接口定义

### 2.1 存储抽象接口

**ICacheStore - 缓存存储接口**:
```csharp
public interface ICacheStore
{
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default);
    Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken ct = default);
    Task DeleteAsync(string key, CancellationToken ct = default);
    Task<bool> ExistsAsync(string key, CancellationToken ct = default);
}
```

**IVectorStore - 向量存储接口**:
```csharp
public interface IVectorStore
{
    Task AddAsync(string collection, VectorData data, CancellationToken ct = default);
    Task<VectorSearchResult[]> SearchAsync(string collection, float[] vector, int topK, CancellationToken ct = default);
    Task DeleteAsync(string collection, string id, CancellationToken ct = default);
}
```

**IRelationalDatabase - 关系数据库接口**:
```csharp
public interface IRelationalDatabase
{
    Task<T> QueryAsync<T>(string sql, object param = null, CancellationToken ct = default);
    Task<int> ExecuteAsync(string sql, object param = null, CancellationToken ct = default);
    Task<T> QueryFirstOrDefaultAsync<T>(string sql, object param = null, CancellationToken ct = default);
}
```

### 2.2 Repository接口

**IUnitOfWork - 工作单元模式**:
```csharp
public interface IUnitOfWork : IDisposable
{
    IMainTaskRepository MainTasks { get; }
    ISubTaskRepository SubTasks { get; }
    ILlmProviderConfigRepository LlmProviderConfigs { get; }
    Task<int> SaveChangesAsync(CancellationToken ct = default);
    Task BeginTransactionAsync(CancellationToken ct = default);
    Task CommitTransactionAsync(CancellationToken ct = default);
    Task RollbackTransactionAsync(CancellationToken ct = default);
}
```

**IMainTaskRepository**:
```csharp
public interface IMainTaskRepository
{
    Task<MainTask?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<List<MainTask>> GetAllAsync(CancellationToken ct = default);
    Task<List<MainTask>> GetByPriorityAsync(int minPriority, CancellationToken ct = default);
    Task AddAsync(MainTask task, CancellationToken ct = default);
    Task UpdateAsync(MainTask task, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}
```

### 2.3 LLM服务接口

**IMafAiAgentRegistry - LLM注册表**:
```csharp
public interface IMafAiAgentRegistry
{
    Task<MafAiAgent> GetBestAgentAsync(LlmScenario scenario, CancellationToken ct = default);
    Task<MafAiAgent> GetAgentByProviderAsync(string providerName, CancellationToken ct = default);
    Task SetAgentEnabledAsync(string providerName, bool enabled, CancellationToken ct = default);
    Task ReloadFromDatabaseAsync(CancellationToken ct = default);
    Task<IEnumerable<LlmProviderConfig>> GetAllConfigsAsync(CancellationToken ct = default);
}
```

**ILlmService - LLM服务接口**:
```csharp
public interface ILlmService
{
    Task<string> CompleteAsync(string prompt, CancellationToken ct = default);
    Task<string> CompleteAsync(string systemPrompt, string userPrompt, CancellationToken ct = default);
    IChatClient GetUnderlyingAgent();
}
```

---

## 三、Agent实现模式

### 3.1 业务Agent基类

**MafBusinessAgentBase - 纯业务基类（不继承AIAgent）**:
```csharp
public abstract class MafBusinessAgentBase
{
    protected readonly IMafAiAgentRegistry LlmRegistry;
    protected readonly ICacheStore Cache;
    protected readonly ILogger Logger;

    protected MafBusinessAgentBase(
        IMafAiAgentRegistry llmRegistry,
        ICacheStore cache,
        ILogger logger)
    {
        LlmRegistry = llmRegistry;
        Cache = cache;
        Logger = logger;
    }

    // 辅助方法：调用LLM
    protected async Task<string> CallLlmAsync(
        string systemPrompt,
        string userPrompt,
        LlmScenario scenario = LlmScenario.Chat,
        CancellationToken ct = default)
    {
        var llmAgent = await LlmRegistry.GetBestAgentAsync(scenario, ct);
        return await llmAgent.ExecuteAsync(systemPrompt, userPrompt);
    }

    // 抽象方法：业务逻辑
    public abstract Task<MafTaskResponse> ExecuteBusinessLogicAsync(
        MafTaskRequest request,
        CancellationToken ct = default);
}
```

### 3.2 LLM Agent基类

**MafAiAgent - 继承MS AF的AIAgent**:
```csharp
public abstract class MafAiAgent : AIAgent
{
    protected readonly LlmProviderConfig Config;
    protected readonly ILogger Logger;
    protected readonly IHttpClientFactory HttpClientFactory;

    protected MafAiAgent(
        LlmProviderConfig config,
        IHttpClientFactory httpClientFactory,
        ILogger logger)
    {
        Config = config;
        HttpClientFactory = httpClientFactory;
        Logger = logger;
    }

    // MS AF必需实现
    protected override async Task<string> RunAsync(ITurnContext turnContext, CancellationToken cancellationToken)
    {
        var prompt = turnContext.Activity?.Text ?? string.Empty;
        return await ExecuteAsync(Config.ModelId, prompt);
    }

    protected override async Task<string> RunStreamingAsync(ITurnContext turnContext, CancellationToken cancellationToken)
    {
        var prompt = turnContext.Activity?.Text ?? string.Empty;
        return await ExecuteAsync(Config.ModelId, prompt);
    }

    // LLM调用抽象方法
    public abstract Task<string> ExecuteAsync(string systemPrompt, string userPrompt, CancellationToken ct = default);
}
```

### 3.3 具体LLM实现示例

**ZhipuAIMafAiAgent - 智谱AI实现**:
```csharp
public class ZhipuAIMafAiAgent : MafAiAgent
{
    public ZhipuAIMafAiAgent(
        LlmProviderConfig config,
        IHttpClientFactory httpClientFactory,
        ILogger<ZhipuAIMafAiAgent> logger)
        : base(config, httpClientFactory, logger)
    {
    }

    public override async Task<string> ExecuteAsync(string systemPrompt, string userPrompt, CancellationToken ct = default)
    {
        var httpClient = HttpClientFactory.CreateClient();

        var request = new
        {
            model = Config.ModelId,
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt }
            },
            temperature = Config.Temperature,
            max_tokens = Config.MaxTokens
        };

        var response = await httpClient.PostAsJsonAsync(
            $"{Config.ApiBaseUrl}/chat/completions",
            request,
            ct);

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ZhipuAIResponse>(ct);
        return result.choices[0].message.content;
    }
}
```

### 3.4 业务Agent示例

**LightingAgent - 灯光控制Agent**:
```csharp
public class LightingAgent : MafBusinessAgentBase
{
    public LightingAgent(
        IMafAiAgentRegistry llmRegistry,
        ICacheStore cache,
        ILogger<LightingAgent> logger)
        : base(llmRegistry, cache, logger)
    {
    }

    public override async Task<MafTaskResponse> ExecuteBusinessLogicAsync(
        MafTaskRequest request,
        CancellationToken ct = default)
    {
        try
        {
            // 1. 意图识别
            var intent = await RecognizeIntentAsync(request.Text, ct);

            // 2. 实体提取
            var entities = await ExtractEntitiesAsync(request.Text, ct);

            // 3. 执行业务逻辑
            var result = await ControlLightAsync(entities, ct);

            return new MafTaskResponse
            {
                Success = true,
                Result = result,
                Message = $"灯光控制成功：{result}"
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "灯光控制失败");
            return new MafTaskResponse
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    private async Task<string> RecognizeIntentAsync(string text, CancellationToken ct)
    {
        var systemPrompt = "你是一个智能家居意图识别助手...";
        var intent = await CallLlmAsync(systemPrompt, text, LlmScenario.Intent, ct);
        return intent;
    }

    private async Task<Dictionary<string, string>> ExtractEntitiesAsync(string text, CancellationToken ct)
    {
        // 使用LLM进行实体提取
        var systemPrompt = "提取灯光控制相关的实体...";
        var json = await CallLlmAsync(systemPrompt, text, LlmScenario.Intent, ct);
        return JsonSerializer.Deserialize<Dictionary<string, string>>(json);
    }

    private async Task<string> ControlLightAsync(Dictionary<string, string> entities, CancellationToken ct)
    {
        var location = entities.GetValueOrDefault("location", "客厅");
        var action = entities.GetValueOrDefault("action", "打开");
        var brightness = entities.GetValueOrDefault("brightness", "100");

        // 模拟控制灯光
        await Task.Delay(100, ct);
        return $"{location}的灯已{action}，亮度{brightness}%";
    }
}
```

---

## 四、依赖注入配置

### 4.1 核心服务注册

**AddMafBuiltinServices扩展方法**:
```csharp
public static class MafServiceCollectionExtensions
{
    public static IServiceCollection AddMafBuiltinServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // 1. 存储抽象接口注册
        services.AddScoped<ICacheStore, RedisCacheStore>();
        services.AddScoped<IVectorStore, QdrantVectorStore>();
        services.AddScoped<IRelationalDatabase, EfCoreRelationalDatabase>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // 2. Repository注册
        services.AddScoped<IMainTaskRepository, MainTaskRepository>();
        services.AddScoped<ISubTaskRepository, SubTaskRepository>();
        services.AddScoped<ILlmProviderConfigRepository, LlmProviderConfigRepository>();

        // 3. DbContext注册
        services.AddDbContext<MafDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("PostgreSQL")));

        // 4. LLM服务注册
        services.AddScoped<IMafAiAgentRegistry, MafAiAgentRegistry>();
        services.AddScoped<MafAiAgent, ZhipuAIMafAiAgent>();
        services.AddScoped<MafAiAgent, TongyiMafAiAgent>();
        services.AddScoped<MafAiAgent, FallbackLlmAgent>();

        // 5. 业务服务注册
        services.AddScoped<MafTaskScheduler>();
        services.AddScoped<MafIntentRecognizer>();
        services.AddScoped<DegradationManager>();

        // 6. HttpClient注册
        services.AddHttpClient();

        // 7. 配置选项
        services.Configure<RedisOptions>(configuration.GetSection("Redis"));
        services.Configure<QdrantOptions>(configuration.GetSection("Qdrant"));

        return services;
    }
}
```

### 4.2 Demo应用注册

**Program.cs (SmartHome Demo)**:
```csharp
var builder = WebApplication.CreateBuilder(args);

// 1. 添加CKY.MAF内置服务
builder.Services.AddMafBuiltinServices(builder.Configuration);

// 2. 注册Demo Agents
builder.Services.AddScoped<LightingAgent>();
builder.Services.AddScoped<ClimateAgent>();
builder.Services.AddScoped<MusicAgent>();

// 3. 添加Blazor服务
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
```

---

## 五、错误处理最佳实践

### 5.1 错误分类

**异常层次结构**:
```csharp
MafException (基础异常)
├─ LlmServiceException (LLM服务异常)
│  └─ IsRateLimited (是否限流)
├─ CacheServiceException (缓存服务异常)
├─ DatabaseException (数据库异常)
│  └─ IsTransient (是否瞬时错误)
└─ VectorStoreException (向量存储异常)
```

### 5.2 重试策略

**MafRetryPolicy**:
```csharp
public class MafRetryPolicy
{
    public static async Task<T> ExecuteAsync<T>(
        Func<Task<T>> action,
        CancellationToken ct = default)
    {
        var maxRetries = 3;
        var initialBackoff = TimeSpan.FromMilliseconds(1000);

        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            try
            {
                return await action();
            }
            catch (MafException ex) when (ex.IsRetryable && attempt < maxRetries - 1)
            {
                var backoff = CalculateBackoff(initialBackoff, attempt);
                await Task.Delay(backoff, ct);
            }
        }

        throw new MafException(MafErrorCode.MaxRetriesExceeded, "最大重试次数已达到");
    }

    private static TimeSpan CalculateBackoff(TimeSpan initialBackoff, int attempt)
    {
        // 指数退避 + 抖动
        var exponential = initialBackoff * Math.Pow(2, attempt);
        var jitter = TimeSpan.FromMilliseconds(Random.Shared.Next(0, 200));
        return exponential + jitter;
    }
}
```

### 5.3 降级策略

**DegradationManager**:
```csharp
public class DegradationManager
{
    private int _currentLevel = 0;
    private readonly ILogger<DegradationManager> _logger;

    public async Task CheckAndDegradeAsync(CancellationToken ct)
    {
        var redisFailureRate = await GetRedisFailureRateAsync(ct);
        var llmErrorRate = await GetLlmErrorRateAsync(ct);
        var dbPoolExhausted = await IsDbPoolExhaustedAsync(ct);

        if (redisFailureRate > 0.2)
        {
            SetLevel(1, "Redis失败率过高");
        }
        else if (llmErrorRate > 0.3)
        {
            SetLevel(2, "LLM错误率过高");
        }
        else if (dbPoolExhausted)
        {
            SetLevel(3, "数据库连接池耗尽");
        }
    }

    private void SetLevel(int level, string reason)
    {
        if (_currentLevel != level)
        {
            _logger.LogWarning("降级级别变更为 {Level}: {Reason}", level, reason);
            _currentLevel = level;
            OnDegradationLevelChanged?.Invoke(level);
        }
    }

    public event Action<int> OnDegradationLevelChanged;
}
```

### 5.4 Agent中的错误处理

**推荐模式**:
```csharp
public override async Task<MafTaskResponse> ExecuteAsync(
    MafTaskRequest request,
    CancellationToken ct = default)
{
    try
    {
        // 业务逻辑
        var result = await MafRetryPolicy.ExecuteAsync(async () =>
        {
            return await DoWorkAsync(request, ct);
        }, ct);

        return new MafTaskResponse { Success = true, Result = result };
    }
    catch (LlmServiceException ex) when (ex.IsRateLimited)
    {
        // LLM限流 - 使用降级策略
        return await FallbackToRuleEngineAsync(request, ct);
    }
    catch (CacheServiceException)
    {
        // 缓存失败 - 降级到直接计算
        return await FallbackToDirectComputeAsync(request, ct);
    }
    catch (DatabaseException ex) when (ex.IsTransient)
    {
        // 数据库瞬时错误 - 重试
        return await RetryOperationAsync(request, ct);
    }
    catch (Exception ex)
    {
        Logger.LogError(ex, "任务执行失败");
        return new MafTaskResponse
        {
            Success = false,
            ErrorMessage = "服务暂时不可用，请稍后重试"
        };
    }
}
```

---

## 六、测试策略

### 6.1 单元测试

**测试结构**:
```
tests/UnitTests/
├── Core/
│   ├── Agents/
│   │   └── MafBusinessAgentBaseTests.cs
│   └── Models/
│       └── MainTaskTests.cs
├── Services/
│   ├── Scheduling/
│   │   └── MafTaskSchedulerTests.cs
│   └── Orchestration/
│       └── MafAiAgentRegistryTests.cs
└── Repositories/
    └── MainTaskRepositoryTests.cs
```

**单元测试示例**:
```csharp
public class MafTaskSchedulerTests
{
    private readonly Mock<ICacheStore> _mockCache;
    private readonly Mock<IMainTaskRepository> _mockRepo;
    private readonly MafTaskScheduler _scheduler;

    public MafTaskSchedulerTests()
    {
        _mockCache = new Mock<ICacheStore>();
        _mockRepo = new Mock<IMainTaskRepository>();
        _scheduler = new MafTaskScheduler(_mockCache.Object, _mockRepo.Object);
    }

    [Fact]
    public async Task ScheduleAsync_ShouldSaveToCacheAndDatabase()
    {
        // Arrange
        var task = new MainTask { Title = "测试任务" };
        _mockCache.Setup(x => x.SetAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _mockRepo.Setup(x => x.AddAsync(It.IsAny<MainTask>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _scheduler.ScheduleAsync(task);

        // Assert
        _mockCache.Verify(x => x.SetAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockRepo.Verify(x => x.AddAsync(task, It.IsAny<CancellationToken>()), Times.Once);
    }
}
```

### 6.2 集成测试

**Testcontainers示例**:
```csharp
public class RedisCacheStoreTests : IClassFixture<RedisFixture>
{
    private readonly RedisFixture _fixture;

    public RedisCacheStoreTests(RedisFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task SetAndGetAsync_ShouldReturnValue()
    {
        // Arrange
        var store = new RedisCacheStore(_fixture.Connection);
        var key = "test-key";
        var value = "test-value";

        // Act
        await store.SetAsync(key, value);
        var result = await store.GetAsync<string>(key);

        // Assert
        Assert.Equal(value, result);
    }
}
```

---

## 七、配置管理

### 7.1 配置文件结构

**appsettings.json**:
```json
{
  "ConnectionStrings": {
    "Redis": "localhost:6379",
    "PostgreSQL": "Host=localhost;Database=mafdb;Username=maf;Password=***"
  },
  "MafStorage": {
    "UseBuiltinImplementations": true,
    "CacheStore": "Redis",
    "VectorStore": "Qdrant",
    "RelationalDatabase": {
      "Provider": "PostgreSQL",
      "EnableSensitiveDataLogging": false
    }
  },
  "Qdrant": {
    "Host": "http://localhost:6333",
    "ApiKey": ""
  },
  "Monitoring": {
    "EnablePrometheus": true,
    "EnableOpenTelemetry": true
  }
}
```

### 7.2 环境变量支持

**配置优先级**: 环境变量 > appsettings.{Environment}.json > appsettings.json

```bash
# Docker环境变量
export Redis__ConnectionString="redis:6379"
export PostgreSQL__ConnectionString="Host=postgres;Database=mafdb"
export MafStorage__CacheStore="Redis"
```

---

## 八、部署检查清单

### 8.1 开发环境

- [ ] .NET 10 SDK已安装
- [ ] Redis已启动（或使用Docker）
- [ ] PostgreSQL已启动（或使用SQLite）
- [ ] 运行数据库迁移
- [ ] 配置appsettings.json
- [ ] 运行单元测试

### 8.2 生产环境

- [ ] 配置强密码和API密钥
- [ ] 启用HTTPS
- [ ] 配置Prometheus监控
- [ ] 配置日志聚合
- [ ] 设置资源限制
- [ ] 配置健康检查
- [ ] 准备回滚计划

---

**相关文档**:
- [核心架构文档](./00-CORE-ARCHITECTURE.md) - 架构设计
- [操作指南](../guides/) - 快速上手
- [错误处理指南](./14-error-handling-guide.md) - 详细错误处理策略

---

**最后更新**: 2026-03-16
**维护者**: CKY.MAF架构团队
