# CKY.MAF 核心架构文档

> **文档版本**: v2.0 (最终整合版)
> **更新日期**: 2026-03-16
> **整合来源**: 01-architecture-overview.md + 12-layered-architecture.md + LLM架构决策

---

## 📋 文档说明

本文档整合了 CKY.MAF 框架的核心架构设计，包括：
- 基于Microsoft Agent Framework的定位
- 5层分层依赖架构（DIP原则）
- LLM服务架构设计
- 核心设计原则与技术栈

> **重要**: CKY.MAF 不是独立框架，而是 Microsoft Agent Framework 的企业级增强层。

---

## 一、框架定位与核心概念

### 1.1 框架定位

**CKY.MAF = 基于Microsoft Agent Framework的企业级增强层**

```
┌─────────────────────────────────────────┐
│  应用层 (Application)                    │  智能家居、设备控制、客服  │
├─────────────────────────────────────────┤
│  CKY.MAF增强层 (Enhancement)             │  调度、存储、监控、优先级  │
├─────────────────────────────────────────┤
│  MS Agent Framework (Core)               │  AIAgent、A2A、LLM集成   │
├─────────────────────────────────────────┤
│  基础设施层 (Infrastructure)             │  LLM、数据库、消息队列    │
└─────────────────────────────────────────┘
```

**核心原则**：
- ✅ 所有Agent继承自MS AF的`AIAgent`
- ✅ Agent间通信使用MS AF的A2A机制
- ✅ CKY.MAF提供MS AF缺失的企业级特性
- ✅ 不重复造轮子，只增强不替代

### 1.2 技术栈

**核心框架**:
- Microsoft Agent Framework (Preview) - 硬性依赖
- .NET 10
- 原生ASP.NET Core（无ABP依赖）

**CKY.MAF增强**:
- 任务调度：优先级系统、依赖管理、弹性调度
- 三层存储：L1内存、L2 Redis、L3 PostgreSQL (EF Core)
- 监控告警：Prometheus、分布式追踪、Grafana

**LLM提供商**（支持7大厂商）:
- 首选：智谱AI (GLM-4/GLM-4-Plus)
- 备选：通义千问、文心一言、讯飞星火、百川、MiniMax
- 自动降级：FallbackLlmAgent

---

## 二、5层分层依赖架构

### 2.1 架构设计原则

**依赖倒置原则（DIP）**: 高层模块不应依赖低层模块，两者都应依赖抽象。

**关键好处**：
- ✅ Core层零外部依赖（仅MS AF）
- ✅ 所有存储实现可替换
- ✅ 单元测试无需外部服务
- ✅ 支持多种实现方案

### 2.2 分层架构图

```
┌─────────────────────────────────────────────────────────────┐
│  Layer 5: Demo应用层                                         │
│  CKY.MultiAgentFramework.Demos.SmartHome (Blazor Server)                    │
│  职责：提供用户界面和演示场景                                │
│  依赖：Services, Core                                        │
└─────────────────────────────────────────────────────────────┘
                           ↓
┌─────────────────────────────────────────────────────────────┐
│  Layer 4: 业务服务层                                         │
│  CKY.MultiAgentFramework.Services                                           │
│  职责：任务调度、意图识别、任务编排、结果聚合                │
│  主要组件：                                                  │
│  - MafTaskScheduler (任务调度器) ✅ 已实现                   │
│  - MafIntentRecognizer (意图识别器)                         │
│  - MafTaskOrchestrator (任务编排器)                          │
│  - DegradationManager (降级管理器) ✅ 已实现                 │
│  依赖：Core, Repository (抽象接口)                           │
└─────────────────────────────────────────────────────────────┘
                           ↓
┌─────────────────────────────────────────────────────────────┐
│  Layer 3: 数据访问层 (Infrastructure/Repository)            │
│  CKY.MultiAgentFramework.Repository                                         │
│  职责：实现 Core 定义的存储抽象接口                         │
│  子模块：                                                    │
│  ├─ Caching/        (缓存实现) ✅ 已完成                     │
│  │  ├─ RedisCacheStore : ICacheStore                        │
│  │  └─ MemoryCacheStore : ICacheStore                       │
│  ├─ Relational/     (关系数据库实现) ✅ 已完成               │
│  │  ├─ EfCoreRelationalDatabase : IRelationalDatabase       │
│  │  └─ UnitOfWork : IUnitOfWork                             │
│  └─ Vectorization/  (向量存储实现) ⏳ 部分完成               │
│     ├─ QdrantVectorStore : IVectorStore (API调整中)         │
│     └─ MemoryVectorStore : IVectorStore ✅                   │
│  外部依赖：Redis、EF Core、Qdrant                            │
│  依赖：Core (实现抽象接口)                                   │
└─────────────────────────────────────────────────────────────┘
                           ↓ 实现
┌─────────────────────────────────────────────────────────────┐
│  Layer 1: 核心抽象层 (Core)                                  │
│  CKY.MultiAgentFramework.Core                                               │
│  职责：定义抽象接口和领域实体                                │
│                                                             │
│  ┌─ Abstractions/Interfaces/ (存储抽象接口)                 │
│  │  - ICacheStore (缓存接口)                                │
│  │  - IVectorStore (向量存储接口)                           │
│  │  - IRelationalDatabase (关系数据库接口)                  │
│  │  - IUnitOfWork (工作单元模式)                            │
│  │                                                          │
│  ├─ Models/ (领域模型)                                      │
│  │  - MainTask, SubTask (任务模型)                         │
│  │  - LlmProviderConfig (LLM配置)                           │
│  │                                                          │
│  └─ Agents/ (Agent 基类)                                    │
│     - MafBusinessAgentBase (纯业务基类，不继承AIAgent)               │
│       └─ Demo Agents: LightingAgent, ClimateAgent          │
│                                                             │
│  依赖：零外部依赖（仅 MS AF + Microsoft.Extensions.* 抽象） │
└─────────────────────────────────────────────────────────────┘
                           ↓ 组合
┌─────────────────────────────────────────────────────────────┐
│  Layer 1: LLM抽象层 (Core - AI Agents)                       │
│                                                             │
│  ┌─ Agents/AiAgents/                                        │
│  │  - MafAiAgent : AIAgent (抽象基类, 继承MS AF)            │
│  │  - IMafAiAgentRegistry (LLM注册表)                       │
│  │                                                          │
│  └─ Services/LLM/                                           │
│     - LlmAgentFactory (工厂模式, 7大提供商)                  │
│     - MafAiAgentRegistry (注册表实现)                       │
│                                                             │
│  依赖：Microsoft.Agents.AI (唯一硬性依赖)                   │
└─────────────────────────────────────────────────────────────┘
```

### 2.3 关键依赖规则

**Core层（Layer 1）依赖规则**：
- ✅ 可以依赖：MS Agent Framework、Microsoft.Extensions.* 抽象包
- ❌ 禁止依赖：具体实现包（Redis、EF Core、Qdrant）
- ❌ 禁止依赖：第三方LLM SDK

**Services层（Layer 4）依赖规则**：
- ✅ 可以依赖：Core抽象接口、Microsoft.Extensions.* 抽象包
- ❌ 禁止依赖：具体实现（直接使用RedisCacheStore、EF Core等）
- ✅ 通过依赖注入获取抽象接口的实现

---

## 三、LLM服务架构设计

### 3.1 架构决策（2026-03-13重构）

**问题**：业务Agent与LLM服务职责混乱

**解决方案**：
- **业务Agent**（MafBusinessAgentBase）专注于业务逻辑，不直接依赖MS AF
- **LLM Agent**（MafAiAgent）继承MS AF的AIAgent，封装不同LLM厂商
- **配置驱动**：LLM提供商配置存储在数据库，支持动态切换

### 3.2 LLM架构图

```
业务层 (Layer 5: Demos → Layer 4: Services)
┌────────────────────────────────────────┐
│  MafBusinessAgentBase (纯业务基类)              │
│  - 依赖 IMafAiAgentRegistry               │
│  - 提供 CallLlmAsync() 辅助方法         │
│  - 不继承 AIAgent                       │
└────────────┬───────────────────────────┘
             │ 使用
             ▼
┌────────────────────────────────────────┐
│  IMafAiAgentRegistry (LLM 注册表)         │
│  - 管理多个 MafAiAgent 实例               │
│  - 根据场景和优先级动态选择              │
│  - 从数据库加载配置                     │
└────────────┬───────────────────────────┘
             │ 管理
             ▼
LLM 抽象层 (Layer 3: Infrastructure)
┌────────────────────────────────────────┐
│  MafAiAgent : AIAgent (抽象基类)          │
│  - 继承 MS AF 的 AIAgent                │
│  - 实现 ExecuteAsync(model, prompt)     │
│  - 支持多种场景 (chat/embed/intent)     │
└────────────┬───────────────────────────┘
             │ 继承
             ▼
┌────────────────────────────────────────┐
│  ZhipuAIMafAiAgent : MafAiAgent            │
│  TongyiMafAiAgent : MafAiAgent             │
│  QwenMafAiAgent : MafAiAgent               │
│  BaichuanMafAiAgent : MafAiAgent           │
│  MiniMaxMafAiAgent : MafAiAgent            │
│  FallbackLlmAgent (自动降级)              │
└────────────────────────────────────────┘
```

### 3.3 LLM提供商配置

**数据库配置** (LlmProviderConfig表):
- ProviderName: 提供商名称
- ApiBaseUrl: API地址
- ApiKey: 密钥
- ModelId: 模型ID
- SupportedScenarios: 支持的场景 (Chat, Embed, Intent等)
- MaxTokens: 最大Token数
- Temperature: 温度参数
- Priority: 优先级 (0-100)
- CostPer1kTokens: 每1K Token成本
- IsEnabled: 是否启用

**使用示例**:
```csharp
// 业务 Agent 中使用 LLM
public class LightingAgent : MafBusinessAgentBase
{
    private readonly IMafAiAgentRegistry _llmRegistry;

    public LightingAgent(IMafAiAgentRegistry llmRegistry)
    {
        _llmRegistry = llmRegistry;
    }

    protected override async Task<MafTaskResponse> ExecuteBusinessLogicAsync(...)
    {
        // 根据场景自动选择最佳 LLM
        var llmAgent = await _llmRegistry.GetBestAgentAsync(LlmScenario.Chat);
        var response = await llmAgent.ExecuteAsync("控制灯光", userPrompt);
        return new MafTaskResponse { Success = true, Result = response };
    }
}
```

---

## 四、核心设计模式

### 4.1 依赖注入模式

**Service Layer注册示例**:
```csharp
// Program.cs
services.AddScoped<ICacheStore, RedisCacheStore>();
services.AddScoped<IVectorStore, QdrantVectorStore>();
services.AddScoped<IRelationalDatabase, EfCoreRelationalDatabase>();
services.AddScoped<IUnitOfWork, UnitOfWork>();

// LLM Agent 注册
services.AddScoped<IMafAiAgentRegistry, MafAiAgentRegistry>();
services.AddScoped<MafAiAgent, ZhipuAIMafAiAgent>();
services.AddScoped<MafAiAgent, TongyiMafAiAgent>();
```

**Core Layer不注册具体实现**，只定义抽象接口。

### 4.2 Repository模式 + UnitOfWork

**Repository抽象** (Core层定义):
```csharp
public interface IMainTaskRepository
{
    Task<MainTask> GetByIdAsync(Guid id);
    Task<IEnumerable<MainTask>> GetAllAsync();
    Task AddAsync(MainTask task);
    Task UpdateAsync(MainTask task);
    Task DeleteAsync(Guid id);
}

public interface IUnitOfWork : IDisposable
{
    IMainTaskRepository MainTasks { get; }
    ISubTaskRepository SubTasks { get; }
    Task<int> SaveChangesAsync();
}
```

**Repository实现** (Infrastructure/Repository层):
```csharp
public class MainTaskRepository : IMainTaskRepository
{
    private readonly MafDbContext _context;

    public MainTaskRepository(MafDbContext context)
    {
        _context = context;
    }

    public async Task<MainTask> GetByIdAsync(Guid id)
    {
        return await _context.MainTasks.FindAsync(id);
    }

    // ... 其他实现
}
```

### 4.3 Factory模式（LLM Agent）

```csharp
// LlmAgentFactory - 根据配置动态创建LLM Agent
public class LlmAgentFactory
{
    public static MafAiAgent CreateAgent(LlmProviderConfig config)
    {
        return config.ProviderName switch
        {
            "ZhipuAI" => new ZhipuAIMafAiAgent(config),
            "Tongyi" => new TongyiMafAiAgent(config),
            "Qwen" => new QwenMafAiAgent(config),
            _ => throw new NotSupportedException($"Provider {config.ProviderName} not supported")
        };
    }
}
```

---

## 五、Main-Agent + Sub-Agent模式

### 5.1 模式说明

```
用户输入
    ↓
MainAgent (主控代理)
    ├─ 意图识别 (IntentRecognizer)
    ├─ 任务分解 (TaskDecomposer)
    ├─ Agent匹配 (AgentMatcher)
    ├─ 任务编排 (TaskOrchestrator)
    └─ 结果聚合 (ResultAggregator)
    ↓
SubAgents (子代理)
    ├─ LightingAgent (灯光控制)
    ├─ ClimateAgent (空调控制)
    ├─ MusicAgent (音乐播放)
    └─ ...
    ↓
智能设备
```

### 5.2 任务优先级系统

**优先级计算** (0-100分):
- 用户显式指定：90-100
- 紧急程度：+20
- 任务依赖数：-5 * 依赖数
- 等待时长：+0.1 * 分钟数

**调度算法**:
1. 优先级队列排序
2. 依赖关系检查
3. 资源可用性检查
4. 弹性调度（可抢占）

---

## 六、弹性与容错

### 6.1 5级服务降级

| 级别 | 触发条件 | 降级措施 |
|------|---------|---------|
| Level 1 | Redis失败率>20% | 禁用非核心功能（推荐系统） |
| Level 2 | LLM错误率>30% | 禁用向量搜索（使用关键词） |
| Level 3 | PostgreSQL连接池耗尽 | 禁用L2缓存（仅用L1内存） |
| Level 4 | LLM完全不可用 | 使用简化模型（GLM-4-Air） |
| Level 5 | 所有LLM不可用 | 使用规则引擎兜底 |

### 6.2 重试策略

**指数退避 + 抖动**:
- 初始退避：1000ms
- 退避倍数：2
- 最大重试：3次
- 抖动范围：±20%

**熔断器参数**:
- LLM API: 10次失败/60秒 → 熔断120秒
- Redis: 20次失败/30秒 → 熔断60秒
- PostgreSQL: 5次失败/60秒 → 熔断180秒

---

## 七、监控与可观测性

### 7.1 Prometheus指标

**业务指标**:
- `maf_task_total{status="success|failure"}` - 任务总数
- `maf_llm_tokens_total{provider}` - LLM Token消耗
- `maf_cache_hit_rate{tier="l1|l2"}` - 缓存命中率

**基础设施指标**:
- `maf_redis_connection_pool_active` - Redis连接池
- `maf_db_connection_pool_active` - DB连接池
- `maf_llm_latency_seconds{provider}` - LLM延迟

### 7.2 分布式追踪

**OpenTelemetry集成**:
- 自动追踪：所有Agent调用
- 手动追踪：业务关键路径
- 上下文传递：A2A通信、任务分解

---

## 八、快速开始

### 8.1 最小Demo

```csharp
// 1. 定义业务 Agent
public class LightingAgent : MafBusinessAgentBase
{
    public LightingAgent(IMafAiAgentRegistry llmRegistry,
                        ICacheStore cache)
        : base(llmRegistry, cache)
    {
    }

    protected override async Task<MafTaskResponse> ExecuteBusinessLogicAsync(
        MafTaskRequest request,
        CancellationToken ct)
    {
        var intent = await RecognizeIntentAsync(request.Text);
        // ... 业务逻辑
    }
}

// 2. 注册服务
builder.Services.AddMafBuiltinServices(builder.Configuration);
builder.Services.AddScoped<LightingAgent>();

// 3. 使用 Agent
var agent = serviceProvider.GetRequiredService<LightingAgent>();
var response = await agent.ExecuteAsync(new MafTaskRequest
{
    Text = "打开客厅灯"
});
```

### 8.2 配置文件

**appsettings.json**:
```json
{
  "ConnectionStrings": {
    "Redis": "localhost:6379",
    "PostgreSQL": "Host=localhost;Database=mafdb"
  },
  "MafStorage": {
    "UseBuiltinImplementations": true,
    "RelationalDatabase": {
      "Provider": "PostgreSQL"
    }
  }
}
```

---

## 九、文档索引

**相关文档**：
- [实现指南](./01-IMPLEMENTATION-GUIDE.md) - 代码实现细节
- [错误处理指南](./specs/14-error-handling-guide.md) - 完整错误处理策略
- [接口设计规范](./specs/06-interface-design-spec.md) - 所有接口定义
- [操作指南](../guides/) - 快速上手指南

---

**最后更新**: 2026-03-16
**维护者**: CKY.MAF架构团队
