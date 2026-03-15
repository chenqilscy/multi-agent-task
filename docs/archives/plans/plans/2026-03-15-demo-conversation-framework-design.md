# Demo对话框架设计文档

> **文档版本**: v1.0
> **创建日期**: 2026-03-15
> **设计目标**: 为SmartHome和CustomerService Demo提供共通对话能力
> **预计周期**: Phase 1 - 5-7天

---

## 📋 目录

1. [设计目标](#设计目标)
2. [架构概览](#架构概览)
3. [核心组件设计](#核心组件设计)
4. [数据流设计](#数据流设计)
5. [错误处理策略](#错误处理策略)
6. [测试策略](#测试策略)
7. [实施路线图](#实施路线图)

---

## 设计目标

### 主要目标

1. **共通能力复用**：构建可复用于SmartHome和CustomerService Demo的对话能力
2. **生产级质量**：提供可生产使用的参考实现
3. **框架能力展示**：展示CKY.MAF的核心特性（调度、编排、存储、弹性）
4. **多LLM支持**：支持智谱/通义/文心/讯飞等多个提供商

### 核心挑战与解决方案

| 挑战 | 解决方案 |
|------|---------|
| **槽位缺失识别** | 预定义模板 + LLM动态识别混合模式 |
| **用户澄清管理** | 优先级排序 + 历史偏好推断 + 模板/LLM混合 |
| **指代消解** | 上下文实体追踪 + LLM语义解析 |
| **意图飘移检测** | 语义距离计算 + 触发词识别 |
| **多轮对话状态** | 状态机 + 上下文栈管理 |
| **长会话问题** | 三层存储 + 会话压缩 + 状态恢复 |

---

## 架构概览

### 系统分层架构

```
┌─────────────────────────────────────────────────────────────┐
│  Layer 5: Demo 应用层                                        │
│  ┌──────────────────┐      ┌──────────────────┐            │
│  │ SmartHome Demo   │      │ CustomerService  │            │
│  └──────────────────┘      └──────────────────┘            │
├─────────────────────────────────────────────────────────────┤
│  Layer 4: 共通业务服务层  ← 🎯 Phase 1 重点实现             │
│  ┌────────────────────────────────────────────────────┐    │
│  │ ✅ 直接复用 (5个)                                    │    │
│  │   HybridIntentRecognizer  │ MafTaskOrchestrator      │    │
│  │   MafAiSessionManager      │ LlmAgentFactory         │    │
│  │   MafAgentMatcher          │                        │    │
│  ├────────────────────────────────────────────────────┤    │
│  │ 🔧 需要增强 (4个)                                    │    │
│  │   MafEntityExtractor (+LLM) │ DialogueAgent (重构)   │    │
│  │   MafCoreferenceResolver (+LLM) │ MafTaskDecomposer  │    │
│  ├────────────────────────────────────────────────────┤    │
│  │ 🆕 需要新增 (5个)                                    │    │
│  │   SlotManager │ MafClarificationManager             │    │
│  │   IntentDriftDetector │ DialogStateManager          │    │
│  │   槽位模型定义                                       │    │
│  └────────────────────────────────────────────────────┘    │
├─────────────────────────────────────────────────────────────┤
│  Layer 3: 基础设施层                                        │
│  RedisCacheStore │ PostgreSQL │ QdrantVectorStore          │
├─────────────────────────────────────────────────────────────┤
│  Layer 2: 存储抽象层                                        │
│  ICacheStore │ IVectorStore │ IRelationalDatabase          │
├─────────────────────────────────────────────────────────────┤
│  Layer 1: 核心抽象层                                        │
│  MS Agent Framework (AIAgent, A2A, IChatClient)            │
└─────────────────────────────────────────────────────────────┘
```

---

## 核心组件设计

### 1. SlotManager（槽位管理器）

#### 职责

- 检测缺失槽位
- 填充槽位（用户输入+历史+默认值）
- 生成澄清问题

#### 接口定义

```csharp
public interface ISlotManager
{
    /// <summary>
    /// 检测缺失槽位
    /// </summary>
    Task<SlotDetectionResult> DetectMissingSlotsAsync(
        string userInput,
        IntentRecognitionResult intent,
        EntityExtractionResult entities,
        CancellationToken ct);

    /// <summary>
    /// 填充槽位
    /// </summary>
    Task<Dictionary<string, object>> FillSlotsAsync(
        string intent,
        Dictionary<string, object> providedSlots,
        DialogContext context,
        CancellationToken ct);

    /// <summary>
    /// 生成澄清问题
    /// </summary>
    Task<string> GenerateClarificationAsync(
        List<SlotDefinition> missingSlots,
        string intent,
        CancellationToken ct);
}
```

#### 数据模型

```csharp
public class IntentSlotDefinition
{
    public string Intent { get; set; }
    public List<SlotDefinition> RequiredSlots { get; set; }
    public List<SlotDefinition> OptionalSlots { get; set; }
}

public class SlotDefinition
{
    public string SlotName { get; set; }
    public string Description { get; set; }
    public SlotType Type { get; set; }
    public bool HasDefaultValue { get; set; }
    public object? DefaultValue { get; set; }
    public List<string> Synonyms { get; set; }
}
```

#### 实现策略

```
输入：意图 + 实体
  ↓
策略1：预定义意图 → 使用模板检测
  ↓
策略2：未知意图 → 使用LLM动态识别
  ↓
输出：缺失槽位列表 + 置信度
```

---

### 2. MafClarificationManager（澄清管理器）

#### 职责

- 分析是否需要澄清
- 选择澄清策略（模板/智能推断/LLM/混合）
- 生成澄清问题
- 处理用户响应

#### 澄清策略

```csharp
public enum ClarificationStrategy
{
    Template,          // 模板澄清（高频场景）
    SmartInference,    // 智能推断（使用历史偏好）
    LLM,               // LLM生成（复杂场景）
    Hybrid             // 混合模式
}
```

#### 澄清流程

```
检测缺失槽位
  ↓
优先级排序（依赖关系 + 用户习惯）
  ↓
选择澄清策略
  ↓
生成澄清问题
  ↓
用户响应处理
  ↓
更新槽位状态
```

---

### 3. MafCoreferenceResolver（指代消解器）- 增强现有实现

#### 现状

- ✅ 已有接口和基础实现
- ❌ 当前实现简单，只返回原文

#### 增强方案

```csharp
public class MafCoreferenceResolver : ICoreferenceResolver
{
    public async Task<string> ResolveAsync(
        string userInput,
        string conversationId,
        CancellationToken ct)
    {
        // 1. 提取指代词："它"、"这个"、"那个"等
        // 2. 从会话历史获取候选实体
        // 3. 使用LLM进行指代消解
        // 4. 属性推断（如"再低一点" → Temperature -= 2）
        // 5. 返回消解后的文本
    }
}
```

---

### 4. IntentDriftDetector（意图飘移检测器）

#### 职责

- 检测意图飘移
- 分类飘移类型（话题切换/纠正/渐变/放弃）
- 生成处理建议

#### 飘移类型

```csharp
public enum IntentDriftType
{
    None,              // 无飘移
    TopicSwitch,       // 话题切换（合法）
    Correction,        // 用户纠正
    GradualDrift,      // 渐进飘移
    Abandonment,       // 任务放弃
    Continuation       // 任务延续
}
```

#### 检测方法

```
当前意图 vs 历史意图
  ↓
1. 计算语义距离（Embedding/LLM）
2. 识别触发词（"对了"、"顺便"、"不对"）
3. 分类飘移类型
4. 生成处理建议（Accept/Revert/Confirm）
```

---

### 5. DialogStateManager（对话状态管理器）

#### 状态机定义

```csharp
public enum DialogState
{
    Idle,                    // 空闲
    AwaitingClarification,   // 等待澄清
    AwaitingConfirmation,    // 等待确认
    ReadyToExecute,         // 准备执行
    Executing,              // 执行中
    Completed,              // 已完成
    TopicSwitched           // 话题已切换
}
```

#### 上下文管理

```csharp
public class DialogContext
{
    public string SessionId { get; set; }
    public DialogState CurrentState { get; set; }
    public Stack<DialogStateSnapshot> ContextStack { get; set; }
    public Dictionary<string, object> UserPreferences { get; set; }
    public List<ConversationTurn> ConversationHistory { get; set; }
}
```

---

## 数据流设计

### SmartHome案例1：天气查询（含澄清）

```
Turn 1: "今天天气如何？"
  ↓
[1] HybridIntentRecognizer → Intent: query_weather (0.92)
  ↓
[2] MafEntityExtractor → {Date: Today} ❌ 缺失 Location
  ↓
[3] SlotManager.DetectMissingSlots → Missing: [Location]
  ↓
[4] MafClarificationManager → "请问您想查询哪个城市的天气？"
  ↓
🤖 返回用户

Turn 2: "北京"
  ↓
[5] MafAgentSession.LoadAsync → 恢复上下文
  ↓
[6] MafEntityExtractor → 补充 Location: "北京"
  ↓
[7] SlotManager.FillSlots → 完整槽位: {Date: Today, Location: 北京}
  ↓
[8] MafTaskOrchestrator → 执行天气查询
  ↓
[9] MafResultAggregator → "北京今天天气晴，温度15°C"
  ↓
[10] MafAgentSession.SaveAsync → 保存会话
```

### SmartHome案例2：设备控制（指代消解）

```
Turn 1: "打开客厅空调"
  ↓
[1-9] 完整执行流程
  ↓
保存上下文：{Device: 客厅空调, Temperature: 26°C}

Turn 2: "它要再低一点"
  ↓
[1] HybridIntentRecognizer → Intent: control_device
  ↓
[2] MafCoreferenceResolver.Resolve →
    "它" → Referent: "客厅空调"
    "再低一点" → Temperature = 26 - 2 = 24°C
  ↓
[3] MafTaskOrchestrator → 执行温度调整
  ↓
[4] 返回："客厅空调已设置为24度"
```

### SmartHome案例3：意图飘移

```
Turn 1: "打开客厅空调"
  ↓
执行中...

Turn 2: "对了，最近几天的天气怎么样？"
  ↓
[1] HybridIntentRecognizer → Intent: query_weather
  ↓
[2] IntentDriftDetector.Analyze →
    SemanticDistance: 0.78 (高飘移)
    TriggerWords: ["对了"]
    Type: TopicSwitch
    Recommendation: Accept (保存当前上下文到栈)
  ↓
[3] DialogStateManager.HandleTopicSwitch →
    Push(DeviceControlState) 到栈
    切换到 WeatherQueryFlow
  ↓
[4] 继续天气查询流程
```

---

## 错误处理策略

### 分层错误处理

| 错误层级 | 类型 | 处理策略 |
|---------|------|---------|
| **Level 1** | 用户输入错误 | 友好引导 + 建议 |
| **Level 2** | 业务逻辑错误 | 降级方案 + 重试 |
| **Level 3** | 执行错误 | 自动容错 + 切换Agent |
| **Level 4** | 系统错误 | 降级到规则引擎 |
| **Level 5** | 严重错误 | 优雅降级 + 通知 |

### 降级策略矩阵

| 错误场景 | 降级策略 | 用户体验 |
|---------|---------|---------|
| LLM调用失败 | 使用规则引擎 | 能力受限但可用 |
| Redis连接失败 | 降级到L1内存 | 性能下降但可用 |
| 实体提取失败 | 要求用户澄清 | 需要更多交互 |
| Agent执行失败 | 重试1次+切换Agent | 自动容错 |
| 任务编排失败 | 串行执行 | 响应变慢但成功 |

---

## 测试策略

### 测试金字塔

```
     /\
    /E2E\      5% - 端到端测试
   /------\
  /  集成  \    25% - 集成测试
 /----------\
/  单元测试  \  70% - 单元测试
```

### 单元测试重点

- **SlotManager**: 槽位检测、填充、澄清生成
- **MafClarificationManager**: 策略选择、响应处理
- **IntentDriftDetector**: 飘移检测、分类
- **MafCoreferenceResolver**: 指代消解

### 集成测试重点

- 多轮对话流程（澄清、指代消解、意图飘移）
- 任务编排（并行/串行）
- 组件协作（完整端到端流程）

### E2E测试场景

1. **天气查询**（含澄清）
2. **多设备控制**（并行执行）
3. **复杂多轮对话**（澄清+指代消解+意图飘移）

---

## 实施路线图

### Phase 1: 共通能力层（5-7天）

#### Week 1: 核心组件开发（3-4天）

**Day 1-2: 槽位管理**
- [ ] 实现 `ISlotManager` 接口
- [ ] 实现预定义槽位模板（SmartHome意图）
- [ ] 实现 LLM 动态槽位识别
- [ ] 单元测试：SlotManager

**Day 2-3: 澄清管理**
- [ ] 实现 `IClarificationManager` 接口
- [ ] 实现澄清策略选择逻辑
- [ ] 实现模板/LLM混合澄清生成
- [ ] 单元测试：ClarificationManager

**Day 3-4: 增强现有组件**
- [ ] 增强 `MafCoreferenceResolver`（添加LLM消解）
- [ ] 增强 `MafTaskDecomposer`（添加LLM拆解）
- [ ] 重构 `DialogueAgent` 为A2A Agent

#### Week 1: 新增组件开发（2-3天）

**Day 4-5: 飘移检测与状态管理**
- [ ] 实现 `IIntentDriftDetector` 接口
- [ ] 实现语义距离计算
- [ ] 实现触发词识别
- [ ] 实现 `IDialogStateManager` 接口
- [ ] 实现状态机转换逻辑
- [ ] 单元测试：IntentDriftDetector + DialogStateManager

**Day 5-6: 集成与测试**
- [ ] 集成测试：多轮对话流程
- [ ] 集成测试：任务编排
- [ ] E2E测试：3个完整场景
- [ ] 性能测试：响应时间、并发

**Day 6-7: 文档与优化**
- [ ] API文档（XML注释）
- [ ] 使用示例
- [ ] 性能优化
- [ ] Code Review

---

## 验收标准

### 功能验收

- [ ] SmartHome Demo支持3个案例场景
- [ ] 支持槽位缺失检测与澄清
- [ ] 支持指代消解
- [ ] 支持意图飘移检测
- [ ] 支持多轮对话状态管理

### 质量验收

- [ ] 单元测试覆盖率 > 70%
- [ ] 集成测试覆盖核心流程
- [ ] E2E测试通过3个完整场景
- [ ] 所有组件有XML注释

### 性能验收

- [ ] 简单任务响应 < 1s
- [ ] 复杂任务响应 < 5s
- [ ] 并发支持 > 100用户

---

## 附录

### A. 参考文档

- [CKY.MAF架构概览](../specs/01-architecture-overview.md)
- [任务调度系统](../specs/03-task-scheduling-design.md)
- [接口设计规范](../specs/06-interface-design-spec.md)
- [实现指南](../specs/09-implementation-guide.md)

### B. 依赖组件

- Microsoft Agent Framework (Preview)
- CKY.MAF.Core
- CKY.MAF.Services
- CKY.MAF.Infrastructure

### C. 相关Issue

无

---

**文档维护**: CKY.MAF架构团队
**最后更新**: 2026-03-15
**审核状态**: 待审核
