# LLM Agent Factory 架构设计总结

## 设计目标

创建一个灵活、可扩展的工厂模式系统，用于根据场景（Chat、Embed、Intent等）和优先级动态创建 LLM Agent 实例，支持多个提供商的自动选择和故障转移。

## 架构设计

### 1. 分层架构

```
┌─────────────────────────────────────────────────────────────┐
│                     应用层 (Services)                        │
│  ┌──────────────────────────────────────────────────────┐  │
│  │              LlmAgentFactory                         │  │
│  │        (工厂模式 - 核心创建逻辑)                      │  │
│  └──────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                  抽象层 (Abstractions)                       │
│  ┌──────────────────────┐  ┌────────────────────────────┐  │
│  │ ILlmAgentFactory     │  │ ILlmProviderConfigRepository│ │
│  └──────────────────────┘  └────────────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                   核心层 (Core)                              │
│  ┌──────────────────┐  ┌─────────────────────────────────┐ │
│  │  LlmAgent        │  │  LlmProviderConfig              │ │
│  │  (抽象基类)       │  │  (领域模型)                      │ │
│  └──────────────────┘  └─────────────────────────────────┘ │
│  ┌──────────────────────────────────────────────────────┐  │
│  │           FallbackLlmAgent                            │  │
│  │        (装饰器模式 - 自动故障转移)                     │  │
│  └──────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
                              ▼
┌─────────────────────────────────────────────────────────────┐
│              基础设施层 (Infrastructure)                     │
│  ┌──────────────────────────────────────────────────────┐  │
│  │       LlmProviderConfigRepository                     │  │
│  │           (EF Core 实现)                              │  │
│  └──────────────────────────────────────────────────────┘  │
│  ┌──────────────────┐  ┌─────────────────────────────────┐ │
│  │ZhipuAILlmAgent   │  │ TongyiLlmAgent / WenxinLlmAgent  │ │
│  │  (具体实现)       │  │   (其他提供商实现)                │ │
│  └──────────────────┘  └─────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────┘
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                   数据持久化层                               │
│  ┌──────────────────────────────────────────────────────┐  │
│  │            MafDbContext                               │  │
│  │    DbSet<LlmProviderConfigEntity>                    │  │
│  └──────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
```

### 2. 核心组件

#### 2.1 领域模型 (Domain Model)

**LlmProviderConfig**
- **职责**: 定义 LLM 提供商的配置信息
- **关键属性**:
  - `ProviderName`: 提供商唯一标识
  - `SupportedScenarios`: 支持的场景列表
  - `Priority`: 优先级（数字越小越高）
  - `IsEnabled`: 是否启用
- **验证**: 内置 `Validate()` 方法确保配置有效性

**LlmScenario** (枚举)
- 定义 8 种场景：Chat, Embed, Intent, Image, Video, Code, Summarization, Translation

#### 2.2 数据库实体 (Entity)

**LlmProviderConfigEntity**
- **职责**: EF Core 持久化实体
- **特性**:
  - JSON 序列化场景列表 (`SupportedScenariosJson`)
  - 支持与领域模型双向转换
  - 自动时间戳管理

#### 2.3 抽象基类 (Base Agent)

**LlmAgent**
- **职责**: 所有 LLM Agent 的抽象基类
- **继承**: `AIAgent` (Microsoft Agent Framework)
- **核心方法**:
  ```csharp
  public abstract Task<string> ExecuteAsync(
      string modelId,
      string prompt,
      LlmScenario scenario,
      string? systemPrompt,
      CancellationToken ct);
  ```

#### 2.4 工厂模式 (Factory)

**ILlmAgentFactory / LlmAgentFactory**
- **职责**: 根据配置和场景创建 Agent 实例
- **创建策略**:
  - 按提供商名称创建
  - 按场景自动选择最佳 Agent
  - 批量创建支持场景的所有 Agent
  - 创建带 Fallback 能力的 Agent

#### 2.5 装饰器模式 (Decorator)

**FallbackLlmAgent**
- **职责**: 包装主 Agent，提供自动故障转移
- **机制**:
  1. 尝试主 Agent
  2. 失败时按优先级尝试备用 Agent
  3. 记录详细的 Fallback 历史
  4. 提供统计信息（成功率、Fallback率等）

### 3. 数据流

```
用户请求 (Chat, Embed, Intent...)
        │
        ▼
LlmAgentFactory.CreateBestAgentForScenarioAsync(scenario)
        │
        ▼
ILlmProviderConfigRepository.GetAllEnabledAsync()
        │
        ▼
MafDbContext.LlmProviderConfigs
        │
        ▼
过滤: IsEnabled == true && SupportedScenarios.Contains(scenario)
        │
        ▼
排序: OrderBy(Priority)
        │
        ▼
创建 LlmAgent 实例 (ZhipuAILlmAgent, TongyiLlmAgent...)
        │
        ▼
返回 Agent 给调用方
```

### 4. Fallback 流程

```
FallbackLlmAgent.ExecuteAsync(prompt, scenario)
        │
        ├─→ 尝试 Agent1 (Priority=1): 智谱AI
        │       │
        │       ├─→ 成功 → 返回结果 ✓
        │       │
        │       └─→ 失败 → 继续 ▼
        │
        ├─→ 尝试 Agent2 (Priority=2): 通义千问
        │       │
        │       ├─→ 成功 → 返回结果 ✓
        │       │
        │       └─→ 失败 → 继续 ▼
        │
        ├─→ 尝试 Agent3 (Priority=3): 文心一言
        │       │
        │       ├─→ 成功 → 返回结果 ✓
        │       │
        │       └─→ 失败 → 继续 ▼
        │
        └─→ 所有 Agent 失败 → 抛出异常 ✗
```

## 设计模式应用

| 模式 | 应用位置 | 优势 |
|------|---------|------|
| **工厂模式** | `LlmAgentFactory` | 集中创建逻辑，隐藏实例化细节 |
| **策略模式** | 不同提供商的创建方法 | 易于添加新的提供商 |
| **装饰器模式** | `FallbackLlmAgent` | 动态添加 Fallback 能力，不修改原 Agent |
| **仓储模式** | `ILlmProviderConfigRepository` | 解耦数据访问，易于测试 |
| **单一职责** | 每个类职责明确 | 高内聚低耦合 |

## 扩展性设计

### 添加新的 LLM 提供商

只需 3 步：

1. **实现 Agent 类**
```csharp
public class NewProviderLlmAgent : LlmAgent
{
    public NewProviderLlmAgent(LlmProviderConfig config, ILogger logger)
        : base(config, logger) { }

    public override async Task<string> ExecuteAsync(...)
    {
        // 实现具体 API 调用
    }
}
```

2. **在工厂中注册**
```csharp
// LlmAgentFactory.CreateAgentAsync
"newprovider" => await CreateNewProviderAgentAsync(config, ct),

private async Task<LlmAgent> CreateNewProviderAgentAsync(...)
{
    // 创建并返回实例
}
```

3. **保存配置到数据库**
```csharp
var config = new LlmProviderConfig
{
    ProviderName = "newprovider",
    SupportedScenarios = new List<LlmScenario> { LlmScenario.Chat },
    Priority = 5,
    IsEnabled = true
};
await _repository.SaveAsync(config);
```

## 使用示例

### 基本用法
```csharp
// 自动选择最佳 Agent（场景在创建时确定）
var agent = await _factory.CreateBestAgentForScenarioAsync(LlmScenario.Chat);
var response = await agent.ExecuteAsync(modelId, prompt);
```

### Fallback 模式
```csharp
// 创建带 Fallback 的 Agent
var agent = await _factory.CreateAgentWithFallbackAsync(LlmScenario.Chat);
var response = await agent.ExecuteAsync(modelId, prompt);
// 如果主 Agent 失败，自动尝试备用 Agent
```

### 指定提供商
```csharp
var agent = await _factory.CreateAgentByProviderAsync("zhipuai", LlmScenario.Chat);
var response = await agent.ExecuteAsync(modelId, prompt);
```

## 性能考虑

1. **配置缓存**: 可考虑添加内存缓存减少数据库查询
2. **Agent 池化**: 对于高频使用的 Agent，可创建对象池
3. **并行创建**: `CreateAllAgentsForScenarioAsync` 支持并行创建
4. **连接复用**: HttpClient 在 Agent 内部复用

## 安全考虑

1. **API 密钥加密**: 存储在数据库时应加密
2. **脱敏日志**: `GetApiKeyForLogging()` 方法提供脱敏
3. **验证配置**: `Validate()` 方法确保配置有效
4. **取消令牌**: 所有异步方法支持 `CancellationToken`

## 测试策略

1. **单元测试**: Mock Repository，测试工厂逻辑
2. **集成测试**: 使用 Testcontainers 测试数据库交互
3. **E2E 测试**: 测试完整的 Fallback 流程

## 监控和可观测性

1. **日志记录**: 关键操作记录详细日志
2. **Fallback 统计**: 提供成功率和 Fallback 率
3. **性能指标**: 记录每个 Agent 的响应时间
4. **错误追踪**: 记录失败原因和堆栈信息

## 总结

该架构设计实现了：
- ✅ **灵活性**: 支持多个提供商，易于扩展
- ✅ **可靠性**: 自动 Fallback 机制保证高可用
- ✅ **可维护性**: 清晰的分层，职责明确
- ✅ **可测试性**: 接口抽象，易于 Mock
- ✅ **性能优化**: 支持并行、缓存等优化策略
