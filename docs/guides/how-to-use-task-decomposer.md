# 如何使用任务分解器（Task Decomposer）

## 概述

`ITaskDecomposer` 是 CKY.MAF 框架的任务分解组件，负责将用户的复杂输入分解为多个可独立执行的 `DecomposedTask`。分解结果直接作为 `ITaskOrchestrator` 和 `ITaskScheduler` 的输入。

| 实现 | 分解策略 |
|------|----------|
| `MafTaskDecomposer` | 规则匹配 + LLM 辅助（降级到规则） |

## 架构位置

```
L4 Services
  └─ Orchestration/
       └─ MafTaskDecomposer.cs        # 分解器实现
L1 Core
  └─ Abstractions/
       └─ ITaskDecomposer.cs          # 接口
  └─ Models/Task/
       ├─ TaskDecomposition.cs         # 分解结果
       └─ DecomposedTask.cs            # 子任务模型
```

## 快速开始

### 1. 基本用法

```csharp
var decomposer = serviceProvider.GetRequiredService<ITaskDecomposer>();

var intent = new IntentRecognitionResult
{
    PrimaryIntent = "LightControl",
    Confidence = 0.95,
    OriginalInput = "打开客厅的灯"
};

var decomposition = await decomposer.DecomposeAsync("打开客厅的灯", intent);
// decomposition.SubTasks → 包含一个 DecomposedTask
```

### 2. 复杂任务分解

对包含连接词（"并且"、"然后"、"接着"）的任务自动分割：

```csharp
var decomposition = await decomposer.DecomposeAsync(
    "打开灯 并且 调节温度到26度 然后 播放音乐",
    intent);
// decomposition.SubTasks → 3 个子任务
```

### 3. LLM 辅助分解

```csharp
if (decomposer is MafTaskDecomposer mafDecomposer)
{
    var decomposition = await mafDecomposer.DecomposeTaskWithLlmAsync(
        "帮我创建一个电商网站，需要用户注册、商品管理和订单系统");
    // LLM 分解失败时自动降级到规则分解
}
```

## 接口定义

```csharp
public interface ITaskDecomposer
{
    Task<TaskDecomposition> DecomposeAsync(
        string userInput,
        IntentRecognitionResult intent,
        CancellationToken ct = default);
}
```

## 分解结果模型

```csharp
public class TaskDecomposition
{
    public string DecompositionId { get; set; }       // 唯一标识
    public string OriginalUserInput { get; set; }     // 原始输入
    public object? Intent { get; set; }               // 意图识别结果
    public List<DecomposedTask> SubTasks { get; set; } // 子任务列表
    public DecompositionMetadata Metadata { get; set; } // 元数据（策略等）
}
```

## 分解策略

### 意图匹配

根据 `IntentRecognitionResult.PrimaryIntent` 通过 `IIntentCapabilityProvider` 查找对应能力：

- 找到能力 → 创建主任务（`PriorityScore = 50`）
- 未找到 → 创建通用任务（`PriorityScore = 30`, `RequiredCapability = "general"`）

### 规则分解

检测连接词（"并且"、"然后"、"接着"），按连接词分割为多个子任务，自动推断每个子任务的意图。

### LLM 辅助分解

通过 `DecomposeTaskWithLlmAsync` 调用 LLM 进行智能分解，失败时降级到规则分解。

## 与编排器的完整流程

```csharp
// 1. 意图识别
var intent = await intentRecognizer.RecognizeAsync(userInput);

// 2. 任务分解
var decomposition = await decomposer.DecomposeAsync(userInput, intent);

// 3. 创建编排计划
var plan = await orchestrator.CreatePlanAsync(decomposition.SubTasks);

// 4. 执行
var results = await orchestrator.ExecutePlanAsync(plan, agentCallback);
```

## 相关文件

| 文件 | 说明 |
|------|------|
| `src/Core/Abstractions/ITaskDecomposer.cs` | 分解器接口 |
| `src/Services/Orchestration/MafTaskDecomposer.cs` | 分解器实现 |
| `src/Core/Models/Task/TaskDecomposition.cs` | 分解结果模型 |
| `src/Core/Models/Task/DecomposedTask.cs` | 子任务模型 |
| `tests/UnitTests/Services/Orchestration/MafTaskDecomposerTests.cs` | 单元测试 |
