# 长对话优化功能使用指南
# Long Dialog Optimization Feature Usage Guide

本文档提供 CKY.MAF 框架中长对话优化功能的详细使用示例和最佳实践。

This document provides detailed usage examples and best practices for the long dialog optimization features in CKY.MAF framework.

## 目录 / Table of Contents

1. [概述 / Overview](#概述--overview)
2. [核心组件使用 / Core Components Usage](#核心组件使用--core-components-usage)
3. [集成到自定义Agent / Integration into Custom Agents](#集成到自定义agent--integration-into-custom-agents)
4. [常见场景示例 / Common Scenarios](#常见场景示例--common-scenarios)
5. [最佳实践 / Best Practices](#最佳实践--best-practices)
6. [性能优化建议 / Performance Optimization Recommendations](#性能优化建议--performance-optimization-recommendations)

---

## 概述 / Overview

长对话优化功能包含以下核心组件：

The long dialog optimization feature includes the following core components:

- **DialogStateManager**: 管理对话状态和历史槽位
- **ContextCompressor**: 自动压缩对话历史以减少Token使用
- **MemoryClassifier**: 智能分类短期/长期记忆并实现自动遗忘策略

---

## 核心组件使用 / Core Components Usage

### 1. DialogStateManager 使用示例

#### 1.1 基本使用 / Basic Usage

```csharp
using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Services.Dialog;

public class DialogStateManagerExample
{
    private readonly IDialogStateManager _stateManager;

    public DialogStateManagerExample(IDialogStateManager stateManager)
    {
        _stateManager = stateManager;
    }

    /// <summary>
    /// 加载或创建对话上下文
    /// </summary>
    public async Task<DialogContext> LoadOrCreateDialogAsync(
        string conversationId,
        string userId)
    {
        var context = await _stateManager.LoadOrCreateAsync(
            conversationId,
            userId,
            CancellationToken.None);

        Console.WriteLine($"TurnCount: {context.TurnCount}");
        Console.WriteLine($"PreviousIntent: {context.PreviousIntent}");

        return context;
    }

    /// <summary>
    /// 更新对话状态
    /// </summary>
    public async Task UpdateDialogStateAsync(
        DialogContext context,
        string intent,
        Dictionary<string, object> slots,
        List<SubTaskResult> results)
    {
        await _stateManager.UpdateAsync(
            context,
            intent,
            slots,
            results,
            CancellationToken.None);

        Console.WriteLine($"Updated turn count: {context.TurnCount}");
    }

    /// <summary>
    /// 记录待澄清问题
    /// </summary>
    public async Task RecordClarificationAsync(
        DialogContext context,
        string question,
        List<string> options)
    {
        await _stateManager.RecordPendingClarificationAsync(
            context,
            question,
            options,
            CancellationToken.None);

        Console.WriteLine($"Pending clarification: {context.PendingClarification?.Question}");
    }

    /// <summary>
    /// 处理用户澄清响应
    /// </summary>
    public async Task<Dictionary<string, object>> HandleClarificationResponseAsync(
        DialogContext context,
        string userResponse)
    {
        var resolvedSlots = await _stateManager.HandleClarificationResponseAsync(
            context,
            userResponse,
            CancellationToken.None);

        Console.WriteLine($"Resolved {resolvedSlots.Count} slots from clarification");

        return resolvedSlots;
    }
}
```

#### 1.2 读取历史槽位 / Reading Historical Slots

```csharp
public async Task<object?> GetHistoricalSlotValueAsync(
    DialogContext context,
    string intent,
    string slotName)
{
    var key = $"{intent}.{slotName}";

    if (context.HistoricalSlots.TryGetValue(key, out var value))
    {
        Console.WriteLine($"Found historical slot: {key} = {value}");

        // 检查频次
        if (context.SlotCounts.TryGetValue(key, out var count))
        {
            Console.WriteLine($"Slot usage count: {count}");
        }

        return value;
    }

    Console.WriteLine($"Historical slot not found: {key}");
    return null;
}
```

#### 1.3 话题切换处理 / Topic Switch Handling

```csharp
public async Task HandleTopicSwitchAsync(
    DialogContext context,
    string newIntent)
{
    // 保存当前话题状态
    await _stateManager.RecordPendingTasksAsync(
        context,
        new List<PendingTaskInfo>(),
        CancellationToken.None);

    // 重新初始化IntentSlots
    context.IntentSlots.Clear();

    Console.WriteLine($"Topic switched from {context.PreviousIntent} to {newIntent}");
}
```

---

### 2. ContextCompressor 使用示例

#### 2.1 基本压缩 / Basic Compression

```csharp
using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Services.Dialog;

public class ContextCompressorExample
{
    private readonly IContextCompressor _compressor;

    public ContextCompressorExample(IContextCompressor compressor)
    {
        _compressor = compressor;
    }

    /// <summary>
    /// 在第5轮对话时触发压缩
    /// </summary>
    public async Task<bool> ShouldCompressAsync(DialogContext context)
    {
        // 每5轮触发一次压缩
        return context.TurnCount > 0 && context.TurnCount % 5 == 0;
    }

    /// <summary>
    /// 执行上下文压缩
    /// </summary>
    public async Task<ContextCompressionResult> CompressDialogAsync(
        DialogContext context)
    {
        var result = await _compressor.CompressAndStoreAsync(
            context,
            CancellationToken.None);

        Console.WriteLine($"Compression ratio: {result.CompressionRatio:P2}");
        Console.WriteLine($"Summary: {result.Summary}");

        foreach (var keyInfo in result.KeyInfos)
        {
            Console.WriteLine($"- [{keyInfo.Type}] {keyInfo.Content} " +
                            $"(Importance: {keyInfo.Importance})");
        }

        return result;
    }
}
```

#### 2.2 生成对话摘要 / Generating Dialog Summary

```csharp
public async Task<string> GenerateDialogSummaryAsync(
    DialogContext context)
{
    var summary = await _compressor.GenerateSummaryAsync(
        context.DialogHistory,
        CancellationToken.None);

    Console.WriteLine($"Generated summary: {summary}");
    return summary;
}
```

#### 2.3 提取关键信息 / Extracting Key Information

```csharp
public async Task<List<KeyInformation>> ExtractKeyInfoAsync(
    DialogContext context)
{
    var keyInfos = await _compressor.ExtractKeyInformationAsync(
        context.DialogHistory,
        CancellationToken.None);

    // 按类型分组
    var grouped = keyInfos.GroupBy(k => k.Type);
    foreach (var group in grouped)
    {
        Console.WriteLine($"\n{group.Key}:");
        foreach (var info in group)
        {
            Console.WriteLine($"  - {info.Content}");
        }
    }

    return keyInfos;
}
```

---

### 3. MemoryClassifier 使用示例

#### 3.1 分类并存储记忆 / Classify and Store Memories

```csharp
using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Services.Dialog;

public class MemoryClassifierExample
{
    private readonly IMemoryClassifier _classifier;

    public MemoryClassifierExample(IMemoryClassifier classifier)
    {
        _classifier = classifier;
    }

    /// <summary>
    /// 分类记忆
    /// </summary>
    public async Task<MemoryClassificationResult> ClassifyMemoriesAsync(
        string intent,
        Dictionary<string, object> slots,
        DialogContext context)
    {
        var result = await _classifier.ClassifyAndStoreAsync(
            intent,
            slots,
            context,
            CancellationToken.None);

        Console.WriteLine($"Long-term memories: {result.LongTermMemories.Count}");
        Console.WriteLine($"Short-term memories: {result.ShortTermMemories.Count}");

        // 显示长期记忆
        foreach (var ltMemory in result.LongTermMemories)
        {
            Console.WriteLine($"[Long-term] {ltMemory.Key}: {ltMemory.Value}");
            Console.WriteLine($"  Reason: {ltMemory.Reason}");
            Console.WriteLine($"  Importance: {ltMemory.ImportanceScore}");
        }

        // 显示短期记忆
        foreach (var stMemory in result.ShortTermMemories)
        {
            Console.WriteLine($"[Short-term] {stMemory.Key}: {stMemory.Value}");
            Console.WriteLine($"  Expires in: {stMemory.Expiry.TotalHours} hours");
        }

        return result;
    }
}
```

#### 3.2 评估记忆遗忘 / Evaluate Memory Forgetting

```csharp
public ForgettingDecision ShouldForgetStatus(
    SemanticMemory memory,
    DateTime lastAccessed,
    int accessCount)
{
    var decision = _classifier.EvaluateForgetting(
        memory,
        lastAccessed,
        accessCount);

    Console.WriteLine($"Memory {memory.Id}: {decision}");

    switch (decision)
    {
        case ForgettingDecision.Keep:
            Console.WriteLine("  → Keep the memory");
            break;
        case ForgettingDecision.Downgrade:
            Console.WriteLine("  → Downgrade to lower priority");
            break;
        case ForgettingDecision.MarkForCleanup:
            Console.WriteLine("  → Mark for cleanup");
            break;
        case ForgettingDecision.Delete:
            Console.WriteLine("  → Delete immediately");
            break;
    }

    return decision;
}
```

---

## 集成到自定义Agent / Integration into Custom Agents

### 完整集成示例 / Complete Integration Example

```csharp
using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Core.Models.Dialog;
using CKY.MultiAgentFramework.Core.Models.Task;
using CKY.MultiAgentFramework.Services.Dialog;
using Microsoft.Extensions.Logging;

public class MyCustomAgent : MafBusinessAgentBase
{
    private readonly IDialogStateManager _stateManager;
    private readonly IMemoryClassifier _memoryClassifier;
    private readonly IContextCompressor _contextCompressor;

    // 其他依赖...
    private readonly IIntentRecognizer _intentRecognizer;
    private readonly ITaskDecomposer _taskDecomposer;
    private readonly IAgentMatcher _agentMatcher;
    private readonly ITaskOrchestrator _taskOrchestrator;
    private readonly IResultAggregator _resultAggregator;

    public override string AgentId => "myapp:custom:agent:001";
    public override string Name => "MyCustomAgent";
    public override string Description => "自定义Agent示例";

    public MyCustomAgent(
        IDialogStateManager stateManager,
        IMemoryClassifier memoryClassifier,
        IContextCompressor contextCompressor,
        IIntentRecognizer intentRecognizer,
        ITaskDecomposer taskDecomposer,
        IAgentMatcher agentMatcher,
        ITaskOrchestrator taskOrchestrator,
        IResultAggregator resultAggregator,
        IMafAiAgentRegistry llmRegistry,
        ILogger<MyCustomAgent> logger)
        : base(llmRegistry, logger)
    {
        _stateManager = stateManager;
        _memoryClassifier = memoryClassifier;
        _contextCompressor = contextCompressor;
        _intentRecognizer = intentRecognizer;
        _taskDecomposer = taskDecomposer;
        _agentMatcher = agentMatcher;
        _taskOrchestrator = taskOrchestrator;
        _resultAggregator = resultAggregator;
    }

    public override async Task<MafTaskResponse> ExecuteBusinessLogicAsync(
        MafTaskRequest request,
        CancellationToken ct = default)
    {
        // 1. 加载对话上下文
        var dialogContext = await _stateManager.LoadOrCreateAsync(
            request.ConversationId,
            request.UserId,
            ct);

        Logger.LogInformation("Dialog loaded: Turn={Turn}, Intent={Intent}",
            dialogContext.TurnCount, dialogContext.PreviousIntent);

        // 2. 意图识别
        var intent = await _intentRecognizer.RecognizeAsync(
            request.UserInput,
            ct);

        // 3. 实体提取和槽位填充
        var slots = ExtractSlotsFromRequest(request);
        AutoFillFromHistory(dialogContext, intent.PrimaryIntent, slots);

        // 4. 任务分解和执行
        var decomposition = await _taskDecomposer.DecomposeAsync(
            request.UserInput,
            intent,
            ct);

        var agentMapping = await _agentMatcher.MatchBatchAsync(
            decomposition.SubTasks,
            ct);

        var plan = await _taskOrchestrator.CreatePlanAsync(
            decomposition.SubTasks,
            ct);

        var results = await _taskOrchestrator.ExecutePlanAsync(plan, ct);

        // 5. 处理SubAgent槽位缺失
        await HandleSubAgentSlotMissingAsync(
            dialogContext,
            intent,
            request,
            decomposition,
            results,
            ct);

        // 6. 更新对话状态
        await _stateManager.UpdateAsync(
            dialogContext,
            intent.PrimaryIntent,
            slots,
            results,
            ct);

        // 7. 记忆分类
        var classificationResult = await _memoryClassifier.ClassifyAndStoreAsync(
            intent.PrimaryIntent,
            slots,
            dialogContext,
            ct);

        Logger.LogInformation("Memory classified: {LongTerm} long-term, {ShortTerm} short-term",
            classificationResult.LongTermMemories.Count,
            classificationResult.ShortTermMemories.Count);

        // 8. 触发上下文压缩（每5轮）
        if (dialogContext.TurnCount % 5 == 0)
        {
            var compressionResult = await _contextCompressor.CompressAndStoreAsync(
                dialogContext,
                ct);

            Logger.LogInformation("Context compressed: ratio={Ratio:P2}",
                compressionResult.CompressionRatio);
        }

        // 9. 生成响应
        var aggregated = await _resultAggregator.AggregateAsync(
            results,
            request.UserInput,
            ct);

        return new MafTaskResponse
        {
            TaskId = request.TaskId,
            Success = aggregated.Success,
            Result = await _resultAggregator.GenerateResponseAsync(aggregated, ct),
            Data = aggregated.AggregatedData
        };
    }

    /// <summary>
    /// 从历史记录自动填充槽位
    /// </summary>
    private void AutoFillFromHistory(
        DialogContext context,
        string intent,
        Dictionary<string, object> slots)
    {
        var intentPrefix = $"{intent}.";

        foreach (var kvp in context.HistoricalSlots)
        {
            if (kvp.Key.StartsWith(intentPrefix, StringComparison.OrdinalIgnoreCase))
            {
                var slotName = kvp.Key.Substring(intentPrefix.Length);
                if (!slots.ContainsKey(slotName))
                {
                    slots[slotName] = kvp.Value;
                    Logger.LogDebug("Auto-filled slot {Slot}={Value} from history",
                        slotName, kvp.Value);
                }
            }
        }
    }

    /// <summary>
    /// 处理SubAgent槽位缺失
    /// </summary>
    private async Task HandleSubAgentSlotMissingAsync(
        DialogContext context,
        IntentRecognitionResult intent,
        MafTaskRequest request,
        TaskDecompositionResult decomposition,
        List<SubTaskResult> results,
        CancellationToken ct)
    {
        foreach (var result in results)
        {
            if (!result.Success &&
                result.Error?.Contains("slot", StringComparison.OrdinalIgnoreCase) == true &&
                result.Error?.Contains("missing", StringComparison.OrdinalIgnoreCase) == true)
            {
                Logger.LogWarning("SubAgent {TaskId} reported missing slots", result.TaskId);

                // 从历史记录填充缺失的槽位
                var filledCount = 0;
                foreach (var kvp in context.HistoricalSlots)
                {
                    var parts = kvp.Key.Split('.');
                    if (parts.Length == 2 && parts[0] == intent.PrimaryIntent)
                    {
                        var slotName = parts[1];
                        if (!request.Parameters.ContainsKey(slotName))
                        {
                            request.Parameters[slotName] = kvp.Value;
                            filledCount++;
                        }
                    }
                }

                // 如果填充了槽位，重新执行任务
                if (filledCount > 0)
                {
                    Logger.LogInformation("Retrying task {TaskId} with {Count} auto-filled slots",
                        result.TaskId, filledCount);

                    var newPlan = await _taskOrchestrator.CreatePlanAsync(
                        decomposition.SubTasks,
                        ct);

                    results.Clear();
                    results.AddRange(await _taskOrchestrator.ExecutePlanAsync(newPlan, ct));
                    break;
                }
            }
        }
    }

    private Dictionary<string, object> ExtractSlotsFromRequest(MafTaskRequest request)
    {
        return new Dictionary<string, object>(request.Parameters);
    }
}
```

---

## 常见场景示例 / Common Scenarios

### 场景1: 多轮对话中的上下文保持 / Scenario 1: Context Maintenance in Multi-Turn Dialog

```csharp
public class MultiTurnDialogExample
{
    private readonly IDialogStateManager _stateManager;

    /// <summary>
    /// 模拟多轮对话
    /// </summary>
    public async Task SimulateMultiTurnDialogAsync()
    {
        var conversationId = "conv-001";
        var userId = "user-123";

        // 第1轮: 打开客厅灯
        var turn1Context = await _stateManager.LoadOrCreateAsync(
            conversationId, userId, CancellationToken.None);

        await _stateManager.UpdateAsync(
            turn1Context,
            "ControlDevice",
            new Dictionary<string, object>
            {
                { "Device", "Light" },
                { "Room", "LivingRoom" },
                { "Action", "TurnOn" }
            },
            new List<SubTaskResult>(),
            CancellationToken.None);

        Console.WriteLine($"Turn 1: TurnCount={turn1Context.TurnCount}");

        // 第2轮: 调整亮度（引用上一轮的"客厅"）
        var turn2Context = await _stateManager.LoadOrCreateAsync(
            conversationId, userId, CancellationToken.None);

        // 检查历史槽位
        if (turn2Context.HistoricalSlots.TryGetValue("ControlDevice.Room", out var room))
        {
            Console.WriteLine($"Auto-detected room from history: {room}");
        }

        await _stateManager.UpdateAsync(
            turn2Context,
            "ControlDevice",
            new Dictionary<string, object>
            {
                { "Device", "Light" },
                { "Room", room ?? "LivingRoom" }, // 使用历史值
                { "Action", "SetBrightness" },
                { "Brightness", 50 }
            },
            new List<SubTaskResult>(),
            CancellationToken.None);

        Console.WriteLine($"Turn 2: TurnCount={turn2Context.TurnCount}");
    }
}
```

---

### 场景2: 记忆从短期到长期的转换 / Scenario 2: Memory Transition from Short-term to Long-term

```csharp
public class MemoryTransitionExample
{
    private readonly IMemoryClassifier _classifier;
    private readonly IDialogStateManager _stateManager;

    /// <summary>
    /// 模拟3次重复偏好，触发长期记忆存储
    /// </summary>
    public async Task SimulatePreferenceTransitionAsync()
    {
        var conversationId = "conv-002";
        var userId = "user-456";

        for (int i = 1; i <= 3; i++)
        {
            var context = await _stateManager.LoadOrCreateAsync(
                conversationId, userId, CancellationToken.None);

            var slots = new Dictionary<string, object>
            {
                { "MusicGenre", "Classical" },
                { "TimeOfDay", "Evening" }
            };

            // 第3次时应该转为长期记忆
            var result = await _classifier.ClassifyAndStoreAsync(
                "PlayMusic",
                slots,
                context,
                CancellationToken.None);

            Console.WriteLine($"Iteration {i}:");
            Console.WriteLine($"  Long-term: {result.LongTermMemories.Count}");
            Console.WriteLine($"  Short-term: {result.ShortTermMemories.Count}");

            await _stateManager.UpdateAsync(
                context,
                "PlayMusic",
                slots,
                new List<SubTaskResult>(),
                CancellationToken.None);
        }

        // 第3次后，"MusicGenre"应该被存储为长期记忆
        var finalContext = await _stateManager.LoadOrCreateAsync(
            conversationId, userId, CancellationToken.None);

        Console.WriteLine($"Final slot count for PlayMusic.MusicGenre: " +
            finalContext.SlotCounts.GetValueOrDefault("PlayMusic.MusicGenre", 0));
    }
}
```

---

### 场景3: 上下文压缩触发 / Scenario 3: Context Compression Triggering

```csharp
public class ContextCompressionExample
{
    private readonly IDialogStateManager _stateManager;
    private readonly IContextCompressor _compressor;

    /// <summary>
    /// 模拟5轮对话后触发压缩
    /// </summary>
    public async Task SimulateCompressionTriggerAsync()
    {
        var conversationId = "conv-003";
        var userId = "user-789";

        for (int turn = 1; turn <= 5; turn++)
        {
            var context = await _stateManager.LoadOrCreateAsync(
                conversationId, userId, CancellationToken.None);

            await _stateManager.UpdateAsync(
                context,
                "QueryWeather",
                new Dictionary<string, object> { { "City", "Beijing" } },
                new List<SubTaskResult>(),
                CancellationToken.None);

            Console.WriteLine($"Turn {turn}: TurnCount={context.TurnCount}");

            // 在第5轮触发压缩
            if (turn == 5)
            {
                var result = await _compressor.CompressAndStoreAsync(
                    context,
                    CancellationToken.None);

                Console.WriteLine($"Compression triggered!");
                Console.WriteLine($"  Compression ratio: {result.CompressionRatio:P2}");
                Console.WriteLine($"  Summary: {result.Summary}");
                Console.WriteLine($"  Key information extracted: {result.KeyInfos.Count}");
            }
        }
    }
}
```

---

### 场景4: SubAgent槽位缺失自动恢复 / Scenario 4: SubAgent Slot Missing Auto-Recovery

```csharp
public class SubAgentRecoveryExample
{
    /// <summary>
    /// 模拟SubAgent槽位缺失时的自动恢复
    /// </summary>
    public async Task SimulateSubAgentRecoveryAsync()
    {
        var conversationId = "conv-004";
        var userId = "user-999";

        // 第1轮: 设置房间偏好
        var turn1Context = await _stateManager.LoadOrCreateAsync(
            conversationId, userId, CancellationToken.None);

        await _stateManager.UpdateAsync(
            turn1Context,
            "ControlDevice",
            new Dictionary<string, object> { { "Room", "Bedroom" } },
            new List<SubTaskResult>(),
            CancellationToken.None);

        // 第2轮: 只说"打开灯"，不指定房间
        var turn2Context = await _stateManager.LoadOrCreateAsync(
            conversationId, userId, CancellationToken.None);

        var request = new MafTaskRequest
        {
            UserInput = "打开灯",
            Parameters = new Dictionary<string, object>
            {
                { "Device", "Light" },
                { "Action", "TurnOn" }
                // 注意: 没有提供"Room"参数
            }
        };

        // SubAgent执行时会发现缺少"Room"槽位
        // MainAgent会从HistoricalSlots中自动填充"Bedroom"
        // 并重新执行任务

        Console.WriteLine("SubAgent should auto-fill 'Room' from history: Bedroom");
    }

    private readonly IDialogStateManager _stateManager;
}
```

---

## 最佳实践 / Best Practices

### 1. 对话状态管理 / Dialog State Management

**✅ DO:**
- 在每个请求开始时调用 `LoadOrCreateAsync`
- 在每个请求结束时调用 `UpdateAsync`
- 使用 `HistoricalSlots` 跟踪用户偏好
- 在话题切换时清理 `IntentSlots`

**❌ DON'T:**
- 不要跨多个请求缓存 `DialogContext` 对象
- 不要手动修改 `TurnCount`（由框架自动管理）
- 不要忽略 `PendingClarification` 和 `PendingTask`

---

### 2. 上下文压缩 / Context Compression

**✅ DO:**
- 在第5、10、15...轮时触发压缩
- 压缩前检查对话历史长度
- 将压缩摘要存储到持久化存储
- 监控压缩比率（目标 > 40%）

**❌ DON'T:**
- 不要在每轮都压缩（浪费LLM调用）
- 不要压缩过短的对话（< 3轮）
- 不要丢失关键信息（检查 `KeyInfos`）

---

### 3. 记忆分类 / Memory Classification

**✅ DO:**
- 使用频次规则（≥3次 → 长期记忆）
- 设置合理的过期时间（短期24小时）
- 定期清理过期的记忆
- 记录分类原因用于调试

**❌ DON'T:**
- 不要将临时信息存储为长期记忆
- 不要忽略重要性评分
- 不要过度依赖自动分类（添加人工规则）

---

### 4. SubAgent槽位填充 / SubAgent Slot Filling

**✅ DO:**
- 优先使用 `HistoricalSlots` 中的值
- 验证槽位值的时效性
- 记录自动填充的日志
- 在填充失败时请求用户澄清

**❌ DON'T:**
- 不要使用过期的历史值
- 不要在没有置信度时自动填充
- 不要忽略SubAgent的错误消息

---

## 性能优化建议 / Performance Optimization Recommendations

### 1. 缓存策略 / Caching Strategy

```csharp
// 使用IMafSessionStorage缓存DialogContext
// 缓存TTL设置为1小时
// 在LoadOrCreateAsync时优先从缓存读取
```

### 2. 批量操作 / Batch Operations

```csharp
// 批量更新槽位而不是逐个更新
await _stateManager.UpdateAsync(
    context,
    intent,
    new Dictionary<string, object>
    {
        { "Slot1", value1 },
        { "Slot2", value2 },
        { "Slot3", value3 }
    },
    results,
    ct);
```

### 3. 异步处理 / Async Processing

```csharp
// 将非关键操作（如记忆分类、压缩）放到后台
_ = Task.Run(async () =>
{
    await _memoryClassifier.ClassifyAndStoreAsync(...);
    await _contextCompressor.CompressAndStoreAsync(...);
});
```

### 4. LLM调用优化 / LLM Call Optimization

```csharp
// 只在必要时调用LLM（如压缩、分类）
// 使用缓存避免重复的LLM调用
// 设置合理的超时时间（5-10秒）
```

---

## 监控和诊断 / Monitoring and Diagnostics

### 关键指标 / Key Metrics

```csharp
// 记录以下指标用于监控
public class DialogMetrics
{
    public int AverageTurnCount { get; set; }
    public double AverageCompressionRatio { get; set; }
    public int LongTermMemoryCount { get; set; }
    public int ShortTermMemoryCount { get; set; }
    public double SlotAutoFillRate { get; set; }
    public int SubAgentRetryCount { get; set; }
}
```

### 日志级别 / Log Levels

```csharp
// Debug: 详细的槽位操作、压缩过程
// Information: 关键流程节点（加载、更新、压缩、分类）
// Warning: SubAgent重试、压缩失败、记忆分类失败
// Error: 状态管理失败、持久化失败
```

---

## 故障排查 / Troubleshooting

### 问题1: 对话历史未压缩

**症状**: 对话历史持续增长，Token使用过高

**解决方案**:
```csharp
// 检查TurnCount是否正确递增
Console.WriteLine($"TurnCount: {context.TurnCount}");

// 确认压缩触发条件
if (context.TurnCount % 5 == 0)
{
    await _compressor.CompressAndStoreAsync(context, ct);
}
```

---

### 问题2: 历史槽位未自动填充

**症状**: SubAgent持续报告槽位缺失

**解决方案**:
```csharp
// 检查HistoricalSlots是否正确更新
foreach (var slot in context.HistoricalSlots)
{
    Console.WriteLine($"{slot.Key}: {slot.Value}");
}

// 验证槽位键格式（应该是 "Intent.SlotName"）
var expectedKey = $"{intent}.{slotName}";
```

---

### 问题3: 记忆未转为长期

**症状**: 重复3次后仍未转为长期记忆

**解决方案**:
```csharp
// 检查SlotCounts是否正确递增
if (context.SlotCounts.TryGetValue(key, out var count))
{
    Console.WriteLine($"Slot {key} used {count} times");

    // 确认频次规则
    if (count >= 3)
    {
        // 应该转为长期记忆
    }
}
```

---

## 总结 / Summary

长对话优化功能通过以下方式提升用户体验：

The long dialog optimization feature improves user experience through:

1. **智能上下文管理**: 自动跟踪对话状态和历史槽位
2. **自动Token优化**: 定期压缩对话历史，减少LLM调用成本
3. **智能记忆分类**: 区分短期/长期记忆，实现个性化体验
4. **容错能力**: SubAgent槽位缺失时自动恢复

通过遵循本指南中的最佳实践，您可以构建高效、智能的多轮对话系统。

By following the best practices in this guide, you can build efficient and intelligent multi-turn dialog systems.
