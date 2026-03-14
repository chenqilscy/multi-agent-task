# LLM 架构重构总结

> **更新日期**: 2026-03-13
> **重构内容**: 分离 LLM 服务与业务 Agent，实现基于数据库配置的动态 LLM 提供商选择

---

## 重构概述

本次重构修正了 LLM 服务与业务 Agent 之间的架构关系，确保：
- **业务 Agent**（MafAgentBase）专注于业务逻辑，不直接依赖 MS AF
- **LLM Agent**（LlmAgent）继承 MS AF 的 AIAgent，封装不同 LLM 厂商的实现
- **配置驱动**：LLM 提供商配置存储在数据库，支持动态切换和场景选择

## 架构变更

### 之前的架构问题

```
❌ 错误架构：
MafAgentBase : AIAgent              // 业务基类继承 MS AF
    ├── ZhipuAILlmAgent : MafAgentBase  // LLM 实现继承业务基类
    └── LightingAgent : MafAgentBase    // 业务 Agent 继承业务基类

问题：
1. 业务 Agent 被强制实现 MS AF 的抽象方法（RunAsync, RunStreamingAsync 等）
2. LLM 实现混入业务逻辑，职责混乱
3. 无法动态选择 LLM 提供商
```

### 重构后的正确架构

```
✅ 正确架构：

业务层 (Layer 5: Demos → Layer 4: Services)
┌────────────────────────────────────────┐
│  MafAgentBase (纯业务基类)              │
│  - 依赖 ILlmAgentRegistry               │
│  - 提供 CallLlmAsync() 辅助方法         │
│  - 不继承 AIAgent                       │
└────────────┬───────────────────────────┘
             │ 使用
             ▼
┌────────────────────────────────────────┐
│  ILlmAgentRegistry (LLM 注册表)         │
│  - 管理多个 LlmAgent 实例               │
│  - 根据场景和优先级动态选择              │
│  - 从数据库加载配置                     │
└────────────┬───────────────────────────┘
             │ 管理
             ▼
LLM 抽象层 (Layer 3: Infrastructure)
┌────────────────────────────────────────┐
│  LlmAgent : AIAgent (抽象基类)          │
│  - 继承 MS AF 的 AIAgent                │
│  - 实现 ExecuteAsync(model, prompt)     │
│  - 支持多种场景 (chat/embed/intent)     │
└────────────┬───────────────────────────┘
             │ 继承
             ▼
┌────────────────────────────────────────┐
│  ZhipuAILlmAgent : LlmAgent            │
│  TongyiLlmAgent : LlmAgent             │
│  QwenLlmAgent : LlmAgent               │
└────────────────────────────────────────┘
```

## 新增文件

### Core Layer (Layer 1)

1. **[LlmAgent.cs](../src/Core/Agents/LlmAgent.cs)**
   - 抽象基类，继承 `AIAgent`
   - 提供统一的 LLM 调用接口
   - 实现 MS AF 必需的抽象方法（RunAsync, RunStreamingAsync 等）
   - 支持多种场景（chat, embed, intent, image, video）

2. **[LlmScenario.cs](../src/Core/Models/LLM/LlmScenario.cs)**
   - 枚举：定义 LLM 使用场景
   - 值：Chat, Embed, Intent, Image, Video, Code, Summarization, Translation

3. **[LlmProviderConfig.cs](../src/Core/Models/LLM/LlmProviderConfig.cs)**
   - LLM 提供商配置模型（对应数据库表结构）
   - 包含：ProviderName, ApiBaseUrl, ApiKey, ModelId, SupportedScenarios, MaxTokens, Temperature, Priority, CostPer1kTokens

### Abstractions Layer (Layer 2)

4. **[ILlmAgentRegistry.cs](../src/Core/Abstractions/ILlmAgentRegistry.cs)**
   - 接口：管理多个 LlmAgent 实例
   - 方法：GetBestAgentAsync(scenario), GetAgentByProviderAsync, SetAgentEnabledAsync, ReloadFromDatabaseAsync

### Services Layer (Layer 4)

5. **[LlmAgentRegistry.cs](../src/Services/Orchestration/LlmAgentRegistry.cs)**
   - `ILlmAgentRegistry` 的实现
   - 支持按场景和优先级选择最佳 LLM Agent
   - 支持动态启用/禁用 Agent

## 重构文件

### MafAgentBase.cs

**变更前**:
```csharp
public abstract class MafAgentBase : AIAgent  // ❌ 继承 MS AF
{
    // 业务逻辑 + MS AF 抽象方法混在一起
}
```

**变更后**:
```csharp
public abstract class MafAgentBase  // ✅ 纯业务基类
{
    protected readonly ILlmAgentRegistry LlmRegistry;  // ✅ 依赖 LLM 注册表

    protected async Task<string> CallLlmAsync(
        string prompt,
        LlmScenario scenario = LlmScenario.Chat,
        string? systemPrompt = null,
        CancellationToken ct = default)
    {
        var llmAgent = await LlmRegistry.GetBestAgentAsync(scenario, ct);
        return await llmAgent.ExecuteAsync(
            llmAgent.GetCurrentModelId(),
            prompt,
            scenario,
            systemPrompt,
            ct);
    }
}
```

### ZhipuAILlmAgent.cs

**变更前**:
```csharp
public class ZhipuAILlmAgent : MafAgentBase, ILlmService  // ❌ 继承业务基类
{
    private readonly ZhipuAIConfig _config;

    // 实现 ILlmService
    public async Task<string> CompleteAsync(string systemPrompt, string userPrompt)
    {
        var request = new MafTaskRequest { ... };
        var response = await ExecuteAsync(request);  // 调用 MafAgentBase 的方法
        return response.Result;
    }

    // 实现 MafAgentBase 抽象方法
    protected override async Task<MafTaskResponse> ExecuteBusinessLogicAsync(...)
    {
        // HTTP 调用智谱AI API
    }
}
```

**变更后**:
```csharp
public class ZhipuAILlmAgent : LlmAgent  // ✅ 继承 LlmAgent
{
    public ZhipuAILlmAgent(LlmProviderConfig config, ILogger<ZhipuAILlmAgent> logger)
        : base(config, logger)  // ✅ 使用统一的配置模型
    {
    }

    // 实现 LlmAgent 抽象方法
    public override async Task<string> ExecuteAsync(
        string modelId,
        string prompt,
        LlmScenario scenario = LlmScenario.Chat,
        string? systemPrompt = null,
        CancellationToken ct = default)
    {
        // 直接调用智谱AI API
        return scenario switch
        {
            LlmScenario.Chat => await CallChatCompletionAsync(modelId, prompt, systemPrompt, ct),
            LlmScenario.Embed => await CallEmbeddingAsync(modelId, prompt, ct),
            _ => throw new NotSupportedException()
        };
    }
}
```

## 数据库设计

### 表结构

```sql
-- LLM 提供商配置表
CREATE TABLE llm_providers (
    id SERIAL PRIMARY KEY,
    provider_name VARCHAR(50) NOT NULL,        -- 'zhipuai', 'tongyi', 'qwen'
    provider_display_name VARCHAR(100) NOT NULL, -- '智谱AI', '通义千问'
    api_base_url VARCHAR(255) NOT NULL,        -- 'https://open.bigmodel.cn/api/'
    api_key VARCHAR(255) NOT NULL,
    is_enabled BOOLEAN DEFAULT true,
    priority INT DEFAULT 0,                    -- 优先级（数字越小优先级越高）
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW()
);

-- LLM 模型配置表
CREATE TABLE llm_models (
    id SERIAL PRIMARY KEY,
    provider_id INT REFERENCES llm_providers(id),
    model_id VARCHAR(100) NOT NULL,           -- 'glm-4', 'glm-4-plus', 'qwen-max'
    model_display_name VARCHAR(100),
    scenario VARCHAR(50) NOT NULL,             -- 'chat', 'embed', 'intent', 'image', 'video'
    max_tokens INT DEFAULT 2000,
    temperature DECIMAL(3,2) DEFAULT 0.7,
    cost_per_1k_tokens DECIMAL(10,4) DEFAULT 0,
    is_available BOOLEAN DEFAULT true,
    created_at TIMESTAMP DEFAULT NOW()
);

-- 创建索引
CREATE INDEX idx_llm_providers_enabled ON llm_providers(is_enabled, priority);
CREATE INDEX idx_llm_models_provider_scenario ON llm_models(provider_id, scenario, is_available);
```

### 配置示例

```sql
-- 插入智谱AI配置
INSERT INTO llm_providers (provider_name, provider_display_name, api_base_url, api_key, priority)
VALUES ('zhipuai', '智谱AI', 'https://open.bigmodel.cn/api/', 'your-api-key', 0);

-- 插入智谱AI模型
INSERT INTO llm_models (provider_id, model_id, model_display_name, scenario, max_tokens, temperature)
SELECT
    id,
    'glm-4',
    'GLM-4',
    'chat',
    2000,
    0.7
FROM llm_providers WHERE provider_name = 'zhipuai';

INSERT INTO llm_models (provider_id, model_id, model_display_name, scenario, max_tokens)
SELECT
    id,
    'embedding-2',
    'Embedding-2',
    'embed',
    1024,
    NULL
FROM llm_providers WHERE provider_name = 'zhipuai';
```

## 使用示例

### 业务 Agent 使用 LLM

```csharp
public class LightingAgent : MafAgentBase
{
    public LightingAgent(
        IMafSessionStorage sessionStorage,
        IPriorityCalculator priorityCalculator,
        IMetricsCollector metricsCollector,
        ILlmAgentRegistry llmRegistry,
        ILogger<LightingAgent> logger)
        : base(sessionStorage, priorityCalculator, metricsCollector, llmRegistry, logger)
    {
    }

    protected override async Task<MafTaskResponse> ExecuteBusinessLogicAsync(
        MafTaskRequest request,
        IAgentSession session,
        CancellationToken ct)
    {
        // 使用辅助方法调用 LLM
        var intent = await CallLlmAsync(
            "用户想要控制客厅的灯光，请识别意图",
            LlmScenario.Intent,
            "你是智能家居意图识别专家",
            ct);

        // 处理意图...
    }
}
```

### 动态选择 LLM 提供商

```csharp
// 自动选择最佳 LLM（基于场景和优先级）
var chatAgent = await llmRegistry.GetBestAgentAsync(LlmScenario.Chat);
var embedAgent = await llmRegistry.GetBestAgentAsync(LlmScenario.Embed);

// 获取特定提供商
var zhipuAgent = await llmRegistry.GetAgentByProviderAsync("zhipuai");

// 动态启用/禁用
await llmRegistry.SetAgentEnabledAsync("zhipuai", false);  // 禁用智谱AI
await llmRegistry.SetAgentEnabledAsync("tongyi", true);    // 启用通义千问
```

### 依赖注入配置

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// 1. 从数据库加载 LLM 配置
var llmConfigs = await dbContext.LlmProviders
    .Where(p => p.IsEnabled)
    .Include(p => p.Models)
    .ToListAsync();

// 2. 创建 LlmAgent 实例
var llmAgents = new List<LlmAgent>();
foreach (var config in llmConfigs)
{
    var agent = config.ProviderName.ToLowerInvariant() switch
    {
        "zhipuai" => new ZhipuAILlmAgent(config, logger),
        "tongyi" => new TongyiLlmAgent(config, logger),
        "qwen" => new QwenLlmAgent(config, logger),
        _ => throw new InvalidOperationException($"Unknown provider: {config.ProviderName}")
    };
    llmAgents.Add(agent);
}

// 3. 注册 LLM Agent Registry
builder.Services.AddSingleton<ILlmAgentRegistry>(sp =>
    new LlmAgentRegistry(llmAgents, sp.GetRequiredService<ILogger<LlmAgentRegistry>>()));

// 4. 注册业务 Agent（自动注入 ILlmAgentRegistry）
builder.Services.AddSingleton<LightingAgent>();
builder.Services.AddSingleton<ClimateAgent>();

var app = builder.Build();
```

## 关键设计原则

### 1. 分层清晰

| 层级 | 职责 | 继承关系 |
|------|------|---------|
| **LlmAgent** | LLM 服务抽象 | `: AIAgent` |
| **MafAgentBase** | 业务 Agent 基类 | 无（纯POCO） |
| **ZhipuAILlmAgent** | 具体厂商实现 | `: LlmAgent` |
| **LightingAgent** | 具体业务 Agent | `: MafAgentBase` |

### 2. 依赖倒置

```
业务 Agent → ILlmAgentRegistry (抽象) → LlmAgent (抽象) → ZhipuAILlmAgent (具体)
```

### 3. 配置驱动

- LLM 配置存储在数据库
- 支持运行时动态切换
- 无需重新编译或部署

### 4. 场景支持

每个 LLM Agent 可以支持多种场景：
- **Chat**: 通用对话
- **Embed**: 文本向量化
- **Intent**: 意图识别
- **Image**: 图像生成
- **Video**: 视频生成

## 迁移指南

### 从旧架构迁移

如果你有代码使用旧的 `ILlmService` 接口：

**旧代码**:
```csharp
private readonly ILlmService _llmService;

var response = await _llmService.CompleteAsync(systemPrompt, userPrompt);
```

**新代码**:
```csharp
// 方式1：使用 MafAgentBase 的辅助方法（推荐）
var response = await CallLlmAsync(userPrompt, LlmScenario.Chat, systemPrompt);

// 方式2：直接使用 LlmAgentRegistry
var llmAgent = await _llmRegistry.GetBestAgentAsync(LlmScenario.Chat);
var response = await llmAgent.ExecuteAsync(
    llmAgent.GetCurrentModelId(),
    userPrompt,
    LlmScenario.Chat,
    systemPrompt);
```

## 测试策略

### 单元测试

```csharp
public class LlmAgentRegistryTests
{
    [Fact]
    public async Task GetBestAgentAsync_ShouldReturnHighestPriorityAgent()
    {
        // Arrange
        var config1 = new LlmProviderConfig { ProviderName = "zhipuai", Priority = 1, ... };
        var config2 = new LlmProviderConfig { ProviderName = "tongyi", Priority = 0, ... };

        var agent1 = new MockZhipuAILlmAgent(config1);
        var agent2 = new MockTongyiLlmAgent(config2);

        var registry = new LlmAgentRegistry(new[] { agent1, agent2 });

        // Act
        var bestAgent = await registry.GetBestAgentAsync(LlmScenario.Chat);

        // Assert
        bestAgent.Config.ProviderName.Should().Be("tongyi"); // Priority 0 > 1
    }
}
```

### 集成测试

使用 Testcontainers 测试实际的数据库配置加载。

## 性能考虑

1. **LLM Agent 实例复用**: 所有 Agent 注册为单例
2. **按场景缓存**: `LlmAgentRegistry` 按场景索引，快速查找
3. **优先级排序**: 优先级在注册时预排序，运行时 O(1) 选择

## 未来增强

1. **自动降级**: 主 LLM 失败时自动切换到备用提供商
2. **成本优化**: 根据场景自动选择最经济的模型
3. **A/B 测试**: 对同一场景使用多个 LLM 提供商进行对比
4. **监控集成**: 记录每个 LLM 调用的性能、成本、成功率

## 相关文档

- **接口设计**: [06-interface-design-spec.md](./06-interface-design-spec.md)
- **架构概览**: [01-architecture-overview.md](./01-architecture-overview.md)
- **分层架构**: [12-layered-architecture.md](./12-layered-architecture.md)
- **实现指南**: [09-implementation-guide.md](./09-implementation-guide.md)

## 总结

本次重构实现了以下核心目标：

✅ **职责分离**: 业务 Agent 和 LLM 服务各司其职
✅ **依赖倒置**: 业务层依赖抽象，不依赖具体实现
✅ **配置驱动**: LLM 配置存储在数据库，支持动态切换
✅ **场景支持**: 支持多种 LLM 使用场景
✅ **可扩展性**: 轻松添加新的 LLM 提供商

这种架构确保 LLM 服务完全融入系统架构，同时保持业务 Agent 的纯粹性。
