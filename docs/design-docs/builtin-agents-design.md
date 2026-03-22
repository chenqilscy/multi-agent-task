# 内置通用 Agent 设计讨论

> **日期**: 2026-03-23  
> **来源**: Phase 2 Round 3 遗留讨论项  
> **状态**: 设计讨论稿

---

## 1. 现状分析

### Agent 类层次

```
AIAgent (MS Agent Framework)
  └── MafAiAgent (abstract, LLM层)
        ├── QwenAIAgent / ZhipuAIAgent / TongyiLlmAgent / ...  (7个 LLM 提供商)
        └── FallbackLlmAgent (兜底)

MafBusinessAgentBase (业务层基类, 不继承 AIAgent, 组合调用 LLM)
  ├── MafLeaderAgent          ← 已内置: 通用编排流水线
  ├── DialogueAgent           ← 已内置: 多轮对话
  ├── IntentRecognitionAgent  ← 已内置: 意图识别
  ├── EmbeddingAgent          ← 已内置: 文本嵌入
  ├── SummarizationAgent      ← 已内置: 文本摘要
  ├── TranslationAgent        ← 已内置: 翻译
  ├── CodeAgent               ← 已内置: 代码生成
  ├── ImageAgent              ← 已内置: 图像处理
  └── VideoAgent              ← 已内置: 视频处理
```

### Demo 层业务 Agent

| SmartHome | CustomerService |
|-----------|----------------|
| SmartHomeLeaderAgent | CustomerServiceLeaderAgent |
| LightingAgent | OrderStatusAgent |
| ClimateAgent | ComplaintAgent |
| MusicAgent | RefundAgent |
| SecurityAgent | KnowledgeBaseAgent |
| WeatherAgent | |
| TemperatureHistoryAgent | |
| KnowledgeBaseAgent | |

---

## 2. 已解决的需求

### MafLeaderAgent（已实现 ✅）

通用主控编排 Agent 已内置于 `Core/Agents/Specialized/MafLeaderAgent.cs`，提供完整的：

```
意图识别 → 实体提取 → 前置钩子 → 任务分解 → Agent匹配 → 任务执行 → 后置钩子 → 结果聚合
```

- 所有阶段均为 `virtual` 方法，允许子类选择性覆盖
- SmartHomeLeaderAgent / CustomerServiceLeaderAgent 均可改为继承此类并覆盖领域钩子

---

## 3. 仍存在的差距

### 3.1 通用 RAG KnowledgeAgent（优先级: 高）

**问题**: SmartHome.KnowledgeBaseAgent 和 CustomerService.KnowledgeBaseAgent 有 80%+ 相同代码（RAG检索 → LLM增强 → 返回结果）。

**建议方案**: 提取到 `Core/Agents/Specialized/RagKnowledgeAgent.cs`

```csharp
public class RagKnowledgeAgent : MafBusinessAgentBase
{
    // 可配置: collection 名称、topK、scoreThreshold
    public virtual string CollectionName { get; init; }
    public virtual int TopK { get; init; } = 3;
    public virtual float ScoreThreshold { get; init; } = 0.3f;

    // 可覆盖: 自定义 prompt 模板
    protected virtual string FormatRagPrompt(string query, List<RagChunk> chunks) { ... }
}
```

**影响**: Demo 层 KnowledgeBaseAgent 简化为 3 行配置类。

### 3.2 DI 注册便捷方法（优先级: 高）

**问题**: 每个 Demo 须手动注册所有基础设施 + Agent，缺少一键注册。

**建议方案**: 在 `Services/Extensions/` 添加:

```csharp
// 注册所有内置 Specialized Agent
services.AddMafBuiltinAgents();

// 注册 Demo 特定 Agent
services.AddMafSmartHomeAgents();
```

### 3.3 RouterAgent（优先级: 中）

**问题**: Agent 路由逻辑目前散落在 LeaderAgent.MatchBatch 中，无法独立使用。

**建议方案**: 提取 `RouterAgent`，支持基于 capability 的自动路由 + 手动路由表。

```csharp
public class RouterAgent : MafBusinessAgentBase
{
    // 基于 IAgentMatcher 自动路由
    // 支持 fallback 链
    // 支持优先级权重
}
```

### 3.4 GuardrailAgent（优先级: 低）

**问题**: 缺少输入/输出安全校验的标准 Agent。

**建议方案**: 装饰器模式包装任意 Agent，添加内容安全过滤。当前优先级低，因：
- 各 LLM 提供商已有内置内容审查
- 可通过 MafLeaderAgent 的前置/后置钩子实现

### 3.5 MemoryAgent（优先级: 低）

**问题**: 长期记忆管理（跨会话用户偏好等）目前分散在各 Agent 中。

**建议方案**: 结合 `IMafMemoryManager` 和 `IMemoryClassifier` 提供标准记忆 Agent。当前优先级低，因接口已定义但使用场景有限。

---

## 4. 推荐实施路径

| 阶段 | 内容 | 预计工作量 |
|------|------|-----------|
| P0 | `AddMafBuiltinAgents()` DI 扩展方法 | 小 | ✅ 已实现 |
| P0 | `RagKnowledgeAgent` 通用内置 Agent | 中 | ✅ 已实现 |
| P1 | Demo LeaderAgent 改为继承 MafLeaderAgent | 大 | ⚠️ 暂缓（见下方分析） |
| P2 | RouterAgent 独立化 | 中 | 待定 |
| P3 | GuardrailAgent / MemoryAgent | 大（低优先级）| 待定 |

### P1 暂缓分析

SmartHomeLeaderAgent 和 CustomerServiceLeaderAgent 与 MafLeaderAgent 的差异较大：

- **SmartHome**: 额外集成了 `IDialogStateManager`(对话状态)、`IMemoryClassifier`(记忆分类)、`IContextCompressor`(上下文压缩)、槽位自动回填等功能
- **CustomerService**: 使用直接路由模式(非 IAgentMatcher)，集成了情绪检测、投诉协作、降级管理等领域特定逻辑

虽然 MafLeaderAgent 的 virtual 钩子可容纳这些扩展，但重构约涉及 300+ 行代码改动，且有破坏现有测试的风险。建议在 Phase 3 中专项处理。

---

## 5. 结论

框架已具备 **9 个内置 Specialized Agent**，其中 `MafLeaderAgent` 已解决核心编排需求。最高价值的改进是：

1. **抽取 RagKnowledgeAgent** — 消除 Demo 层重复代码
2. **添加 `AddMafBuiltinAgents()` 扩展** — 简化 DI 注册

其余（Router、Guardrail、Memory）可视实际需求在后续迭代中按需实现。
