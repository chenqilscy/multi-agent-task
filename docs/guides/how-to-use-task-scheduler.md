# 如何使用任务调度器（Task Scheduler）

## 概述

`ITaskScheduler` 是 CKY.MAF 框架的任务调度组件，负责优先级计算、并发控制和单任务执行。它与 `ITaskOrchestrator` 配合工作：调度器管理优先级和资源，编排器管理依赖关系和执行计划。

| 组件 | 职责 |
|------|------|
| `MafTaskScheduler` | 优先级排序、优先级分组、并发限制、单任务执行 |
| `MafPriorityCalculator` | 多维度优先级评分（0-100） |

## 架构位置

```
L4 Services
  └─ Scheduling/
       ├─ ITaskScheduler.cs           # 接口
       ├─ MafTaskScheduler.cs         # 调度器实现
       └─ MafPriorityCalculator.cs    # 优先级计算器
L1 Core
  └─ Abstractions/
       └─ IPriorityCalculator.cs      # 优先级计算器接口
```

## 快速开始

### 1. 注册服务

```csharp
services.AddSingleton<IPriorityCalculator, MafPriorityCalculator>();
services.AddSingleton<ITaskScheduler, MafTaskScheduler>();
```

或通过一键注册：

```csharp
services.AddPersistentTaskServices();
```

### 2. 调度任务

```csharp
var scheduler = serviceProvider.GetRequiredService<ITaskScheduler>();

var tasks = new List<DecomposedTask>
{
    new() { TaskId = "1", TaskName = "紧急任务", Priority = TaskPriority.Critical },
    new() { TaskId = "2", TaskName = "普通任务", Priority = TaskPriority.Normal },
    new() { TaskId = "3", TaskName = "后台任务", Priority = TaskPriority.Low }
};

var result = await scheduler.ScheduleAsync(tasks);
// result.ExecutionPlan.HighPriorityTasks  → 紧急任务
// result.ExecutionPlan.MediumPriorityTasks → 普通任务
// result.ExecutionPlan.LowPriorityTasks   → 后台任务
```

### 3. 执行单个任务

```csharp
var task = new DecomposedTask { TaskId = "1", TaskName = "Test" };

var result = await scheduler.ExecuteTaskAsync(task, async (t, ct) =>
{
    // 你的 Agent 执行逻辑
    return new TaskExecutionResult
    {
        TaskId = t.TaskId,
        Success = true,
        Message = "完成"
    };
});
```

## 核心接口

```csharp
public interface ITaskScheduler
{
    // 调度多个任务，计算优先级并分组
    Task<ScheduleResult> ScheduleAsync(
        List<DecomposedTask> tasks, CancellationToken ct = default);

    // 执行单个任务（带并发控制）
    Task<TaskExecutionResult> ExecuteTaskAsync(
        DecomposedTask task,
        Func<DecomposedTask, CancellationToken, Task<TaskExecutionResult>> executor,
        CancellationToken ct = default);
}
```

## 优先级评分系统

`MafPriorityCalculator` 使用多维度评分（0-100分）：

| 维度 | 分值范围 | 说明 |
|------|----------|------|
| 基础优先级 | 0-40 | Critical=40, High=30, Normal=20, Low=10, Background=5 |
| 用户交互 | 0-30 | Active=30, Passive=15, Automatic=5 |
| 时间因素 | 0-15 | Urgent, Normal, Flexible 等 |
| 资源惩罚 | 0-10 | 高资源使用扣分 |
| 依赖传播 | 0-5 | 继承依赖任务优先级的 5% |
| 超期奖励 | +15% | 超期任务额外提升 15% |

### 优先级分组阈值

| 分组 | 分数范围 |
|------|----------|
| 高优先级 | > 50 |
| 中优先级 | 30-50 |
| 低优先级 | < 30 |

## 并发控制

`MafTaskScheduler` 通过 `SemaphoreSlim` 限制同时执行的任务数量：

```csharp
var scheduler = new MafTaskScheduler(
    priorityCalculator,
    maxConcurrentTasks: 10  // 最大并发数，默认 10
);
```

`ExecuteTaskAsync` 调用前会等待信号量，确保不超过并发上限。

## 调度结果模型

```csharp
public class ScheduleResult
{
    public List<string> ScheduledTasks { get; set; }        // 已排序的任务 ID 列表
    public ScheduleExecutionPlan ExecutionPlan { get; set; } // 分组后的执行计划
}

public class ScheduleExecutionPlan
{
    public string PlanId { get; set; }
    public List<DecomposedTask> HighPriorityTasks { get; set; }
    public List<DecomposedTask> MediumPriorityTasks { get; set; }
    public List<DecomposedTask> LowPriorityTasks { get; set; }
}
```

## 与 Orchestrator 的协作

典型流程：Scheduler 计算优先级 → Orchestrator 按依赖分组执行

```csharp
// 1. 调度（计算优先级 + 排序）
var scheduleResult = await scheduler.ScheduleAsync(tasks);

// 2. 编排（按依赖分组 + 执行）
var executionPlan = await orchestrator.CreatePlanAsync(tasks);
var results = await orchestrator.ExecutePlanAsync(executionPlan, agentCallback);
```

## 相关文件

| 文件 | 说明 |
|------|------|
| `src/Services/Scheduling/ITaskScheduler.cs` | 调度器接口 |
| `src/Services/Scheduling/MafTaskScheduler.cs` | 调度器实现 + 模型类 |
| `src/Services/Scheduling/MafPriorityCalculator.cs` | 优先级计算器 |
| `src/Core/Abstractions/IPriorityCalculator.cs` | 优先级计算器接口 |
| `tests/UnitTests/Scheduling/MafTaskSchedulerTests.cs` | 调度器测试 |
