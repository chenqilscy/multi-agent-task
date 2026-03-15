# 长对话场景上下文优化设计方案

> **创建日期**: 2026-03-15
> **设计师**: Claude (Sonnet 4.6)
> **项目**: CKY.MAF - Multi-Agent Framework
> **状态**: 设计评审中

---

## 📋 目录

1. [问题陈述](#问题陈述)
2. [设计目标](#设计目标)
3. [核心概念](#核心概念)
4. [架构设计](#架构设计)
5. [组件设计](#组件设计)
6. [数据流程](#数据流程)
7. [错误处理](#错误处理)
8. [测试策略](#测试策略)
9. [实施计划](#实施计划)

---

## 问题陈述

### 当前挑战

在长对话场景中，CKY.MAF面临以下问题：

1. **上下文膨胀**：随着对话轮次增加，传递给LLM的上下文长度线性增长
   - Token消耗过高
   - 响应速度下降
   - 成本增加

2. **信息管理混乱**：
   - 缺乏智能的记忆分级（短期/长期）
   - 用户偏好无法持久化
   - 对话历史缺乏压缩机制

3. **槽位管理复杂**：
   - 长对话中槽位值可能演化
   - 多实例任务（如"客厅空调"和"卧室空调"）难以管理
   - SubAgent槽位缺失缺乏统一处理机制

4. **架构集成不足**：
   - DialogStateManager未与现有任务编排系统集成
   - 缺乏与ITaskOrchestrator、ITaskDecomposer的协同

---

## 设计目标

### 主要目标

1. **智能记忆管理**：自动区分短期/长期记忆，实现信息的自动分级和遗忘
2. **上下文压缩**：在保持关键信息的前提下，压缩对话历史以降低Token消耗
3. **槽位分层管理**：支持全局/会话/意图三层槽位设计
4. **无缝集成**：与CKY.MAF现有的任务编排系统完全集成

### 成功标准

- ✅ 长对话场景下Token使用量降低40%以上
- ✅ 用户偏好准确识别率达到90%+
- ✅ SubAgent槽位缺失自动处理率80%+
- ✅ 对话轮次超过50轮仍保持稳定性能

---

## 核心概念

### 三层槽位架构

```
┌─────────────────────────────────────────────────────────┐
│ Layer 1: 全局槽位 (GlobalSlots)                         │
│ • 跨会话的用户偏好                                       │
│ • 存储: IMafMemoryManager (L3 + Vector)                │
│ • TTL: 永久或30天未访问后降级                            │
│ • 例: control_device.Location = "客厅"                   │
├─────────────────────────────────────────────────────────┤
│ Layer 2: 会话槽位 (SessionSlots)                        │
│ • 当前对话的上下文                                       │
│ • 存储: DialogContext (L1 + L2)                         │
│ • TTL: 会话结束 (24小时)                                 │
│ • 例: ActiveTasks = [客厅空调, 卧室空调]                  │
├─────────────────────────────────────────────────────────┤
│ Layer 3: 意图槽位 (IntentSlots)                          │
│ • 单个意图的槽位值                                       │
│ • 存储: SlotDetectionResult (临时)                      │
│ • TTL: 意图完成或超时                                    │
│ • 例: control_device.Device = "空调"                     │
└─────────────────────────────────────────────────────────┘
```

### 智能记忆分级

**长期记忆触发条件**：
- 规则1.1: 同一槽位值出现≥3次 → 用户偏好
- 规则1.2: 任务成功确认 → 重要决策
- 规则1.3: 包含关键词("记住"、"默认"、"总是")
- 规则1.4: LLM重要性评分 ≥ 0.6

**短期记忆触发条件**：
- 规则2.1: 未完成的澄清问题
- 规则2.2: 低置信度槽位 (< 0.7)
- 规则2.3: 临时性信息("这次"、"这次对话")
- 规则2.4: 最近3轮的完整对话

### SubAgent槽位缺失处理流程

```
SubAgent检测槽位缺失
  ↓
返回 SlotMissingResult
  ↓
MainAgent接收处理
  ↓
尝试从HistoricalSlots填充
  ↓
尝试使用DefaultValue
  ↓
仍有缺失？
  ├─ YES → 生成澄清问题 → 记录PendingTask
  └─ NO  → 重新调用SubAgent
```

---

## 架构设计

### 整体架构图

```
┌─────────────────────────────────────────────────────────────────┐
│                    Layer 5: Demo应用层                           │
│  ┌────────────────────────────────────────────────────────────┐ │
│  │              SmartHomeMainAgent : MafBusinessAgentBase     │ │
│  │   • 继承MafBusinessAgentBase (不继承AIAgent)              │ │
│  │   • 实现ExecuteBusinessLogicAsync(MafTaskRequest)         │ │
│  │   • 职责: 对话编排、任务协调、状态管理                     │ │
│  └────────────────────────────────────────────────────────────┘ │
└────────────────────────────┬────────────────────────────────────┘
                             │ 依赖并组合
┌────────────────────────────▼────────────────────────────────────┐
│                    Layer 4: 业务服务层                            │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │          现有组件 (CKY.MAF任务编排核心)                   │  │
│  │  • IIntentRecognizer - 意图识别                          │  │
│  │  • IEntityExtractor - 实体提取                           │  │
│  │  • ITaskDecomposer - 任务分解                            │  │
│  │  • IAgentMatcher - Agent匹配                             │  │
│  │  • ITaskOrchestrator - 任务编排                          │  │
│  │  • IResultAggregator - 结果聚合                          │  │
│  └──────────────────────────────────────────────────────────┘  │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │          新增组件 (长对话上下文优化)                       │  │
│  │  • IDialogStateManager - 对话状态管理                    │  │
│  │  • ISlotManager - 槽位管理 (部分实现)                   │  │
│  │  • IClarificationManager - 澄清管理 (部分实现)          │  │
│  │  • IContextCompressor - 上下文压缩 (新增)               │  │
│  │  • IMemoryClassifier - 记忆分类 (新增)                  │  │
│  └──────────────────────────────────────────────────────────┘  │
└────────────────────────────┬────────────────────────────────────┘
                             │ 依赖抽象接口
┌────────────────────────────▼────────────────────────────────────┐
│                 Layer 2: 存储抽象层                               │
│  • IMafMemoryManager - 记忆管理 (已实现)                     │
│  • IMafSessionStorage - 会话存储 (已实现)                    │
│  • ICacheStore - 缓存存储 (已实现)                           │
│  • IVectorStore - 向量存储 (已实现)                          │
└────────────────────────────┬────────────────────────────────────┘
                             │ 实现
┌────────────────────────────▼────────────────────────────────────┐
│                  Layer 3: 基础设施层                              │
│  • MafMemoryManager - 向量+DB双存储                          │
│  • MafTieredSessionStorage - L1+L2+L3三层存储                │
│  • RedisCacheStore / PostgreSqlDatabase / QdrantVectorStore  │
└─────────────────────────────────────────────────────────────────┘
```

### MainAgent在任务编排中的集成点

```
用户输入 (MafTaskRequest)
  ↓
┌─────────────────────────────────────────────────────────┐
│ 0. 加载对话上下文 (新增)                                  │
│    IDialogStateManager.LoadOrCreateAsync()              │
└────────────────┬────────────────────────────────────────┘
                 ↓
┌─────────────────────────────────────────────────────────┐
│ 1. 意图识别 (现有)                                       │
│    IIntentRecognizer.RecognizeAsync()                    │
└────────────────┬────────────────────────────────────────┘
                 ↓
┌─────────────────────────────────────────────────────────┐
│ 2. 实体提取 (现有)                                       │
│    IEntityExtractor.ExtractAsync()                       │
└────────────────┬────────────────────────────────────────┘
                 ↓
┌─────────────────────────────────────────────────────────┐
│ 3. 槽位检测与填充 (增强)                                  │
│    ISlotManager.DetectMissingSlotsAsync(context)         │
│    ✅ 传入DialogContext用于智能填充                       │
└────────────────┬────────────────────────────────────────┘
                 ↓
┌─────────────────────────────────────────────────────────┐
│ 4. 槽位缺失处理 (新增)                                    │
│    IClarificationManager.GenerateClarificationAsync()     │
│    ✅ 记录PendingClarification                            │
└────────────────┬────────────────────────────────────────┘
                 ↓
┌─────────────────────────────────────────────────────────┐
│ 5. 任务分解 (现有)                                       │
│    ITaskDecomposer.DecomposeAsync()                      │
│    ✅ 返回TaskDecomposition → List<DecomposedTask>        │
└────────────────┬────────────────────────────────────────┘
                 ↓
┌─────────────────────────────────────────────────────────┐
│ 6. 注入对话上下文到子任务 (新增)                          │
│    foreach subTask in decomposition.SubTasks:           │
│      • 注入GlobalSlots                                   │
│      • 注入SessionId, UserId, TurnCount                   │
│      • 注入PreviousIntent                                 │
└────────────────┬────────────────────────────────────────┘
                 ↓
┌─────────────────────────────────────────────────────────┐
│ 7. Agent匹配 (现有)                                      │
│    IAgentMatcher.MatchBatchAsync()                       │
└────────────────┬────────────────────────────────────────┘
                 ↓
┌─────────────────────────────────────────────────────────┐
│ 8. 任务编排 (现有)                                       │
│    ITaskOrchestrator.CreatePlanAsync()                   │
│    ITaskOrchestrator.ExecutePlanAsync()                  │
└────────────────┬────────────────────────────────────────┘
                 ↓
┌─────────────────────────────────────────────────────────┐
│ 9. SubAgent槽位缺失处理 (新增)                           │
│    检查TaskExecutionResult.ErrorCode == "SLOTS_MISSING"  │
│    • 从HistoricalSlots填充                                │
│    • 重新执行计划                                          │
│    • 或生成澄清问题                                        │
└────────────────┬────────────────────────────────────────┘
                 ↓
┌─────────────────────────────────────────────────────────┐
│ 10. 结果聚合 (现有)                                      │
│     IResultAggregator.AggregateAsync()                    │
└────────────────┬────────────────────────────────────────┘
                 ↓
┌─────────────────────────────────────────────────────────┐
│ 11. 更新对话状态 (新增)                                   │
│     IDialogStateManager.UpdateAsync()                     │
└────────────────┬────────────────────────────────────────┘
                 ↓
┌─────────────────────────────────────────────────────────┐
│ 12. 触发上下文压缩 (新增, 每5轮)                          │
│     IContextCompressor.CompressAndStoreAsync()           │
└────────────────┬────────────────────────────────────────┘
                 ↓
┌─────────────────────────────────────────────────────────┐
│ 13. 记忆分类与存储 (新增, 每轮)                           │
│     IMemoryClassifier.ClassifyAndStoreAsync()            │
└────────────────┬────────────────────────────────────────┘
                 ↓
           返回 MafTaskResponse
```

---

## 组件设计

### 1. DialogStateManager (新增)

**接口定义**：

```csharp
// src/Core/Abstractions/IDialogStateManager.cs
namespace CKY.MultiAgentFramework.Core.Abstractions
{
    public interface IDialogStateManager
    {
        /// <summary>
        /// 加载或创建对话上下文
        /// </summary>
        Task<DialogContext> LoadOrCreateAsync(
            string conversationId,
            string userId,
            CancellationToken ct = default);

        /// <summary>
        /// 更新对话状态
        /// </summary>
        Task UpdateAsync(
            DialogContext context,
            string intent,
            Dictionary<string, object> slots,
            List<TaskExecutionResult> executionResults,
            CancellationToken ct = default);

        /// <summary>
        /// 记录待处理的澄清
        /// </summary>
        Task RecordPendingClarificationAsync(
            DialogContext context,
            string intent,
            Dictionary<string, object> detectedSlots,
            List<SlotDefinition> missingSlots,
            CancellationToken ct = default);

        /// <summary>
        /// 记录待处理的任务（SubAgent槽位缺失时）
        /// </summary>
        Task RecordPendingTasksAsync(
            DialogContext context,
            ExecutionPlan plan,
            Dictionary<string, object> filledSlots,
            CancellationToken ct = default);

        /// <summary>
        /// 处理用户响应（针对澄清问题）
        /// </summary>
        Task<MafTaskResponse> HandleClarificationResponseAsync(
            string conversationId,
            string userResponse,
            CancellationToken ct = default);
    }
}
```

**核心职责**：
- 管理DialogContext生命周期
- 追踪TurnCount、PreviousIntent、HistoricalSlots
- 协调PendingClarification和PendingTasks
- 与IMafSessionStorage集成存储

---

### 2. ContextCompressor (新增)

**接口定义**：

```csharp
// src/Core/Abstractions/IContextCompressor.cs
namespace CKY.MultiAgentFramework.Core.Abstractions
{
    public interface IContextCompressor
    {
        /// <summary>
        /// 压缩并存储对话历史
        /// </summary>
        Task<ContextCompressionResult> CompressAndStoreAsync(
            DialogContext context,
            CancellationToken ct = default);

        /// <summary>
        /// 生成对话摘要（使用LLM）
        /// </summary>
        Task<string> GenerateSummaryAsync(
            List<MessageContext> messages,
            CancellationToken ct = default);

        /// <summary>
        /// 提取关键信息（使用LLM）
        /// </summary>
        Task<List<KeyInformation>> ExtractKeyInformationAsync(
            List<MessageContext> messages,
            CancellationToken ct = default);
    }

    public class ContextCompressionResult
    {
        public string Summary { get; set; } = string.Empty;
        public List<KeyInformation> KeyInfos { get; set; } = new();
        public int OriginalMessageCount { get; set; }
        public int CompressedMessageCount { get; set; }
        public double CompressionRatio { get; set; }
    }

    public class KeyInformation
    {
        public string Type { get; set; } = string.Empty;  // "Preference", "Decision", "Fact"
        public string Content { get; set; } = string.Empty;
        public double Importance { get; set; }
        public List<string> Tags { get; set; } = new();
    }
}
```

**压缩策略**：

```
触发条件: TurnCount % 5 == 0

压缩规则:
├─ 最近5轮 → 保留完整对话 (L1 Memory)
├─ 5-10轮 → 压缩为摘要 (L2 Redis, 24h TTL)
├─ 10-50轮 → 只保留关键信息 (L3 PostgreSQL)
└─ 50轮+ → 向量化语义检索 (L3 + Vector)
```

---

### 3. MemoryClassifier (新增)

**接口定义**：

```csharp
// src/Core/Abstractions/IMemoryClassifier.cs
namespace CKY.MultiAgentFramework.Core.Abstractions
{
    public interface IMemoryClassifier
    {
        /// <summary>
        /// 分类并存储记忆
        /// </summary>
        Task<MemoryClassificationResult> ClassifyAndStoreAsync(
            string intent,
            Dictionary<string, object> slots,
            DialogContext context,
            AggregatedResult executionResult,
            CancellationToken ct = default);

        /// <summary>
        /// 评估记忆是否应该遗忘
        /// </summary>
        ForgettingDecision EvaluateForgetting(
            SemanticMemory memory,
            DateTime lastAccessed,
            int accessCount);
    }

    public class MemoryClassificationResult
    {
        public List<ShortTermMemory> ShortTermMemories { get; set; } = new();
        public List<LongTermMemory> LongTermMemories { get; set; } = new();
        public List<ForgettingCandidate> ForgettingCandidates { get; set; } = new();
    }

    public enum ForgettingDecision
    {
        Keep,              // 保留
        Downgrade,         // 降级（从热存储移到冷存储）
        MarkForCleanup,    // 标记待清理
        Delete             // 删除
    }
}
```

**智能分级规则**：

```
┌─────────────────────────────────────────────────────────┐
│ 长期记忆触发规则:                                         │
│  ✅ 规则1.1: 同一槽位值出现≥3次 → 用户偏好               │
│  ✅ 规则1.2: 任务成功确认 → 重要决策                     │
│  ✅ 规则1.3: 包含关键词("记住"、"默认"、"总是")          │
│  ✅ 规则1.4: LLM评分 ≥ 0.6                              │
├─────────────────────────────────────────────────────────┤
│ 短期记忆触发规则:                                         │
│  ⚠️ 规则2.1: 未完成的澄清问题                            │
│  ⚠️ 规则2.2: 低置信度槽位 (< 0.7)                        │
│  ⚠️ 规则2.3: 临时性信息("这次")                          │
│  ⚠️ 规则2.4: 最近3轮的完整对话                           │
└─────────────────────────────────────────────────────────┘
```

---

### 4. SlotManager (增强现有)

**现有实现检查**：
- ✅ 已有接口: `src/Core/Abstractions/ISlotManager.cs`
- ✅ 已有实现: `src/Services/Dialog/SlotManager.cs`
- ✅ 已有测试: `src/tests/UnitTests/Services/Dialog/SlotManagerTests.cs`

**需要增强的方法**：

```csharp
// 现有方法签名
Task<SlotDetectionResult> DetectMissingSlotsAsync(
    string userInput,
    IntentRecognitionResult intent,
    EntityExtractionResult entities,
    CancellationToken ct = default);

// 增强后的方法签名
Task<SlotDetectionResult> DetectMissingSlotsAsync(
    string userInput,
    IntentRecognitionResult intent,
    EntityExtractionResult entities,
    DialogContext context,  // ✅ 新增：传入对话上下文
    CancellationToken ct = default);
```

**增强逻辑**：
- 检查`context.HistoricalSlots`进行智能填充
- 支持`context.PreviousIntent`的指代消解
- 从`GlobalSlots`加载默认值

---

### 5. ClarificationManager (增强现有)

**现有实现检查**：
- ✅ 已有接口: `src/Core/Abstractions/IClarificationManager.cs`
- ✅ 已有部分实现
- ⚠️ 需要增强：支持SubAgent槽位缺失场景

**新增方法**：

```csharp
Task<string> GenerateClarificationAsync(
    List<SlotDefinition> missingSlots,
    string intent,
    Dictionary<string, object> context,  // ✅ 新增：上下文信息
    CancellationToken ct = default);
```

---

## 数据流程

### 完整的长对话处理流程

```
┌─────────────────────────────────────────────────────────┐
│ 场景: 用户多轮对话配置智能家居                            │
└─────────────────────────────────────────────────────────┘

Turn 1: "打开空调"
├─ IDialogStateManager.LoadOrCreateAsync()
│  └─ 创建 DialogContext (TurnCount=1)
├─ IIntentRecognizer → intent: "control_device"
├─ ISlotManager.DetectMissingSlotsAsync()
│  ├─ 检测到 Device="空调", Action="打开"
│  └─ 缺失 Location
├─ IClarificationManager → "请问要打开哪个位置的空调？"
└─ 返回 NeedsClarification=true

Turn 2: "客厅的"
├─ IDialogStateManager.LoadOrCreateAsync()
│  └─ 加载 DialogContext (TurnCount=2)
├─ 检测到 PendingClarification
├─ ISlotManager 填充 Location="客厅"
├─ ITaskDecomposer → DecomposedTask: control_device
├─ 注入上下文到 DecomposedTask
├─ ITaskOrchestrator.ExecutePlanAsync()
│  └─ 调用 SmartHomeDeviceController
├─ IResultAggregator → "已打开客厅的空调"
├─ IDialogStateManager.UpdateAsync()
│  └─ 更新 HistoricalSlots: {control_device.Location: 客厅}
└─ IMemoryClassifier → 分类为短期记忆

Turn 3-5: ... (正常对话)

Turn 6: "打开空调" (第二次)
├─ ISlotManager.DetectMissingSlotsAsync()
│  ├─ 检测到 Device="空调", Action="打开"
│  ├─ 缺失 Location
│  └─ ✅ 从 HistoricalSlots 填充: Location="客厅"
├─ ITaskOrchestrator.ExecutePlanAsync()
│  └─ 直接执行，无需澄清
└─ IMemoryClassifier → Location出现2次，仍未达长期记忆阈值

Turn 10: "打开空调" (第三次)
├─ ISlotManager.DetectMissingSlotsAsync()
│  └─ ✅ 自动填充 Location="客厅"
├─ ITaskOrchestrator.ExecutePlanAsync()
└─ IMemoryClassifier → Location出现3次，提升为长期记忆！
   └─ SaveSemanticMemoryAsync("control_device.Location", "客厅", tags: ["用户偏好"])

Turn 15: 触发上下文压缩
├─ TurnCount % 5 == 0 → IContextCompressor
├─ 生成对话摘要
├─ 提取关键信息
└─ 压缩 MessageHistory (保留最近5轮 + 摘要)

Turn 30: 用户切换到新话题 "查询天气"
├─ IntentDriftDetector 检测到意图飘移
├─ IContextCompressor 压缩旧话题
└─ 开始新的对话流程
```

---

## 错误处理

### SubAgent槽位缺失错误处理

```csharp
// TaskExecutionResult错误码定义
public enum TaskErrorCode
{
    None = 0,
    SlotsMissing = 1,      // 槽位缺失
    AgentNotFound = 2,     // Agent未找到
    ExecutionFailed = 3,   // 执行失败
    Timeout = 4            // 超时
}

// MainAgent中的错误处理
if (executionResult.ErrorCode == TaskErrorCode.SlotsMissing)
{
    // 1. 尝试从HistoricalSlots填充
    var filledSlots = await TryFillFromHistoricalSlotsAsync(
        executionResult.Data as SlotMissingData,
        context,
        ct);

    // 2. 仍有缺失？
    if (filledSlots.StillMissing.Any())
    {
        // 3. 记录PendingTask
        await _stateManager.RecordPendingTasksAsync(
            context, plan, filledSlots.Filled, ct);

        // 4. 生成澄清问题
        return new MafTaskResponse
        {
            NeedsClarification = true,
            ClarificationQuestion = GenerateClarification(filledSlots.StillMissing)
        };
    }

    // 5. 重新执行计划
    return await RetryExecutionPlanAsync(plan, filledSlots.Filled, ct);
}
```

### 降级策略

```
┌─────────────────────────────────────────────────────────┐
│ 降级级别 (基于 LLM API 状态)                             │
├─────────────────────────────────────────────────────────┤
│ Level 1: 禁用非核心功能                                  │
│   • ContextCompressor 使用模板而非LLM                    │
│   • MemoryClassifier 仅使用规则引擎                      │
├─────────────────────────────────────────────────────────┤
│ Level 2: 禁用向量搜索                                    │
│   • IMemoryManager.GetRelevantMemoryAsync() 降级         │
│   • 使用关键词搜索替代                                    │
├─────────────────────────────────────────────────────────┤
│ Level 3: 禁用L2缓存                                      │
│   • 仅使用L1内存存储                                     │
│   • 会话结束后自动清理                                    │
├─────────────────────────────────────────────────────────┤
│ Level 4: 使用简化LLM模型                                  │
│   • 切换到 GLM-4-Air (更快速、更便宜)                   │
├─────────────────────────────────────────────────────────┤
│ Level 5: 禁用LLM entirely                                 │
│   • 使用规则引擎替代                                      │
│   • 仅支持预定义的对话模板                                │
└─────────────────────────────────────────────────────────┘
```

---

## 测试策略

### 单元测试 (70%)

**DialogStateManager测试**：
```
✅ LoadOrCreateAsync - 新建会话
✅ LoadOrCreateAsync - 加载已有会话
✅ UpdateAsync - 更新TurnCount
✅ UpdateAsync - 更新HistoricalSlots
✅ RecordPendingClarificationAsync - 记录澄清
✅ HandleClarificationResponseAsync - 处理用户响应
```

**ContextCompressor测试**：
```
✅ CompressAndStoreAsync - 压缩5轮对话
✅ GenerateSummaryAsync - LLM摘要生成
✅ ExtractKeyInformationAsync - 关键信息提取
✅ 压缩率验证 (目标: > 60%)
```

**MemoryClassifier测试**：
```
✅ 频次规则测试 - 3次触发长期记忆
✅ 关键词规则测试 - "记住"触发长期记忆
✅ LLM评分测试 - 0.6分界
✅ 遗忘策略测试 - 30天未访问降级
```

### 集成测试 (25%)

**端到端场景测试**：
```
场景1: 简单对话 (3-5轮)
  - 用户: "打开空调"
  - 系统: "哪个位置？"
  - 用户: "客厅的"
  - 系统: "已打开客厅的空调"

场景2: 复杂对话 (10-20轮)
  - 涉及多个设备控制
  - 验证HistoricalSlots自动填充
  - 验证记忆自动提升为长期

场景3: 意图飘移 (20+轮)
  - 从"控制设备"切换到"查询天气"
  - 验证上下文压缩
  - 验证旧话题清理
```

### 性能测试 (5%)

```
指标:
• 简单任务响应时间: P95 < 1s
• 复杂任务响应时间: P95 < 5s
• 长对话 (50轮) 内存使用: < 100MB
• Token使用量降低: > 40%
```

---

## 实施计划

### Phase 1: 核心组件实现 (3天)

**Task 1.1: DialogStateManager实现**
- 创建接口和实现类
- 集成IMafSessionStorage
- 单元测试
- 验收: 能正确加载和更新DialogContext

**Task 1.2: 增强SlotManager**
- 添加DialogContext参数
- 实现HistoricalSlots智能填充
- 更新单元测试
- 验收: 能从历史槽位自动填充

**Task 1.3: 增强ClarificationManager**
- 支持上下文信息
- 优化澄清问题生成
- 验收: 生成更自然的澄清问题

### Phase 2: 智能记忆管理 (2天)

**Task 2.1: MemoryClassifier实现**
- 规则引擎
- LLM评分集成
- 单元测试
- 验收: 准确区分短期/长期记忆

**Task 2.2: 自动遗忘策略**
- 实现30天降级规则
- 实现低价值清理
- 验收: 自动清理过期记忆

### Phase 3: 上下文压缩 (2天)

**Task 3.1: ContextCompressor实现**
- LLM摘要生成
- 关键信息提取
- 单元测试
- 验收: 压缩率 > 60%

**Task 3.2: 集成压缩触发**
- 在MainAgent中集成
- 每5轮触发压缩
- 验收: 自动压缩，不影响用户体验

### Phase 4: MainAgent集成 (2天)

**Task 4.1: 增强SmartHomeMainAgent**
- 集成DialogStateManager
- 集成MemoryClassifier
- 集成ContextCompressor
- 验收: 完整的长对话流程

**Task 4.2: SubAgent槽位缺失处理**
- 实现SlotMissingResult处理
- 实现自动填充和重试
- 验收: 80%的槽位缺失自动处理

### Phase 5: 测试与优化 (1天)

**Task 5.1: 端到端测试**
- 3个完整场景测试
- 性能测试
- 验收: 所有场景通过

**Task 5.2: 文档与示例**
- API文档
- 使用示例
- README更新
- 验收: 文档完整

---

## 现有实现检查清单

### ✅ 已实现

**Core层**:
- ✅ `DialogContext` - `src/Core/Models/Dialog/DialogContext.cs`
- ✅ `ISlotManager` - `src/Core/Abstractions/ISlotManager.cs`
- ✅ `IMafMemoryManager` - `src/Core/Abstractions/IMafMemoryManager.cs`
- ✅ `IMafSessionStorage` - `src/Core/Abstractions/IMafSessionStorage.cs`

**Services层**:
- ✅ `SlotManager` - `src/Services/Dialog/SlotManager.cs`
- ✅ `MafMemoryManager` - `src/Services/Storage/MafMemoryManager.cs`
- ✅ `MafTieredSessionStorage` - `src/Services/Storage/MafTieredSessionStorage.cs`
- ✅ `SmartHomeMainAgent` - `src/Demos/SmartHome/SmartHomeMainAgent.cs`

**Demo层**:
- ✅ `WeatherAgent` - `src/Demos/SmartHome/Agents/WeatherAgent.cs`

### ❌ 待实现

**新增接口**:
- ❌ `IDialogStateManager` - 对话状态管理
- ❌ `IContextCompressor` - 上下文压缩
- ❌ `IMemoryClassifier` - 记忆分类

**新增实现**:
- ❌ `DialogStateManager` - Services/Dialog/
- ❌ `ContextCompressor` - Services/Dialog/
- ❌ `MemoryClassifier` - Services/Dialog/

**需要增强**:
- ⚠️ `SlotManager` - 添加DialogContext参数
- ⚠️ `ClarificationManager` - 支持SubAgent槽位缺失
- ⚠️ `SmartHomeMainAgent` - 集成新增组件

---

## 附录

### A. 参考文档

- [CKY.MAF架构概览](../specs/01-architecture-overview.md)
- [5层DIP架构](../specs/12-layered-architecture.md)
- [任务调度设计](../specs/03-task-scheduling-design.md)
- [接口设计规范](../specs/06-interface-design-spec.md)
- [Demo对话框架实施计划](./2026-03-15-demo-conversation-framework-implementation.md)

### B. 依赖项

- .NET 10
- Microsoft Agent Framework (Preview)
- CKY.MAF Core Services
- xUnit, Moq, FluentAssertions

### C. 环境准备

```bash
# 还原NuGet包
dotnet restore

# 构建解决方案
dotnet build

# 运行所有测试
dotnet test
```

---

**文档维护**: CKY.MAF架构团队
**创建日期**: 2026-03-15
**预计完成**: 2026-03-22 (7天)
**审核状态**: 待审核
